using Microsoft.AspNetCore.Mvc;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Requests;
using OSRSGeMonitor.Api.Services;

namespace OSRSGeMonitor.Api.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    private readonly InMemoryDataStore _dataStore;
    private readonly DiscordNotificationService _discordNotificationService;

    public ConfigController(InMemoryDataStore dataStore, DiscordNotificationService discordNotificationService)
    {
        _dataStore = dataStore;
        _discordNotificationService = discordNotificationService;
    }

    [HttpGet]
    public ActionResult<GlobalConfig> GetConfig()
    {
        return Ok(_dataStore.Config);
    }

    [HttpPut]
    public async Task<ActionResult<GlobalConfig>> UpdateConfig(UpdateConfigRequest request, CancellationToken cancellationToken)
    {
        if (request.UserAgent is not null && string.IsNullOrWhiteSpace(request.UserAgent))
        {
            return BadRequest("User-Agent is required.");
        }

        var current = _dataStore.Config;
        var enabled = request.DiscordNotificationsEnabled ?? current.DiscordNotificationsEnabled;
        var webhookUrl = request.DiscordWebhookUrl ?? current.DiscordWebhookUrl;
        if (enabled && string.IsNullOrWhiteSpace(webhookUrl))
        {
            return BadRequest("Discord webhook URL is required when notifications are enabled.");
        }

        if (!string.IsNullOrWhiteSpace(webhookUrl) && !IsValidDiscordWebhookUrl(webhookUrl))
        {
            return BadRequest("Discord webhook URL must be a valid https://discord.com/api/webhooks/... URL.");
        }

        var updated = await _dataStore.UpdateConfigAsync(request, cancellationToken);
        return Ok(updated);
    }

    private static bool IsValidDiscordWebhookUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        if (host is not "discord.com" and not "discordapp.com")
        {
            return false;
        }

        return uri.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.OrdinalIgnoreCase);
    }

    [HttpPost("discord-test")]
    public async Task<IActionResult> SendDiscordTest([FromBody] DiscordTestRequest? request, CancellationToken cancellationToken)
    {
        var config = _dataStore.Config;
        if (!config.DiscordNotificationsEnabled || string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
        {
            return BadRequest("Discord notifications are disabled or webhook URL is missing.");
        }

        var message = string.IsNullOrWhiteSpace(request?.Message)
            ? "OMG bestie ALERTS are LIVE!! 💅✨📈💸🔥 This is so slay, no cap, fr fr 😎🎉🪙💎 #GEGodTier"
            : request!.Message.Trim();

        await _discordNotificationService.EnqueueTestAsync(message, cancellationToken);
        return Accepted();
    }

    public sealed record DiscordTestRequest(string? Message);
}
