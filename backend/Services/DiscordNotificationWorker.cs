using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace OSRSGeMonitor.Api.Services;

public sealed class DiscordNotificationWorker : BackgroundService
{
    private readonly DiscordNotificationQueue _queue;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly InMemoryDataStore _dataStore;
    private readonly ILogger<DiscordNotificationWorker> _logger;
    private DateTimeOffset _rateLimitUntil = DateTimeOffset.MinValue;

    public DiscordNotificationWorker(
        DiscordNotificationQueue queue,
        IHttpClientFactory httpClientFactory,
        InMemoryDataStore dataStore,
        ILogger<DiscordNotificationWorker> logger)
    {
        _queue = queue;
        _httpClientFactory = httpClientFactory;
        _dataStore = dataStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await RespectRateLimitAsync(stoppingToken);
                var result = await TrySendAsync(notification, stoppingToken);
                if (result == SendResult.RateLimited && !stoppingToken.IsCancellationRequested)
                {
                    await _queue.EnqueueAsync(notification, stoppingToken);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Failed to send Discord notification for {ItemName}.", notification.ItemName);
            }
        }
    }

    private async Task RespectRateLimitAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (_rateLimitUntil <= now)
        {
            return;
        }

        var delay = _rateLimitUntil - now;
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }
    }

    private async Task<SendResult> TrySendAsync(DiscordNotification notification, CancellationToken cancellationToken)
    {
        var config = _dataStore.Config;
        if (!config.DiscordNotificationsEnabled)
        {
            return SendResult.Skipped;
        }

        if (string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
        {
            return SendResult.Skipped;
        }

        var payload = new DiscordWebhookPayload(BuildMessage(notification));
        var client = _httpClientFactory.CreateClient();
        using var response = await client.PostAsJsonAsync(config.DiscordWebhookUrl, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if ((int)response.StatusCode == 429)
            {
                await HandleRateLimitAsync(response, cancellationToken);
                return SendResult.RateLimited;
            }

            _logger.LogWarning(
                "Discord webhook returned status {StatusCode} for {ItemName}.",
                response.StatusCode,
                notification.ItemName);
            return SendResult.Failed;
        }

        return SendResult.Sent;
    }

    private async Task HandleRateLimitAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                _rateLimitUntil = DateTimeOffset.UtcNow.AddSeconds(5);
                return;
            }

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("retry_after", out var retryAfter))
            {
                var seconds = retryAfter.GetDouble();
                var delay = TimeSpan.FromSeconds(Math.Max(1, seconds));
                _rateLimitUntil = DateTimeOffset.UtcNow.Add(delay);
                _logger.LogWarning("Discord rate limit hit. Pausing for {DelaySeconds:N1} seconds.", delay.TotalSeconds);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Discord rate limit response.");
        }

        _rateLimitUntil = DateTimeOffset.UtcNow.AddSeconds(5);
    }

    private static string BuildMessage(DiscordNotification notification)
    {
        return notification.Type switch
        {
            DiscordNotificationType.Drop => string.Format(
                CultureInfo.InvariantCulture,
                "📉🔥 DROP ALERT: {0} @ {1:N0} gp (mean {2:N0}, σ {3:N2})",
                notification.ItemName,
                notification.TriggerPrice,
                notification.Mean,
                notification.StandardDeviation),
            DiscordNotificationType.Recovery => string.Format(
                CultureInfo.InvariantCulture,
                "📈✨ RECOVERY: {0} @ {1:N0} gp (mean {2:N0})",
                notification.ItemName,
                notification.RecoveryPrice ?? 0,
                notification.Mean),
            DiscordNotificationType.Test => string.Format(
                CultureInfo.InvariantCulture,
                "[TEST] {0}",
                notification.Message ?? "Discord alerts are configured correctly."),
            _ => notification.ItemName
        };
    }

    private sealed record DiscordWebhookPayload(string Content);

    private enum SendResult
    {
        Sent,
        Skipped,
        Failed,
        RateLimited
    }
}
