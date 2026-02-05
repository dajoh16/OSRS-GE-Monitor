using System.Threading.Channels;

namespace OSRSGeMonitor.Api.Services;

public sealed record DiscordNotification(
    DiscordNotificationType Type,
    string ItemName,
    double TriggerPrice,
    double Mean,
    double StandardDeviation,
    double? RecoveryPrice,
    DateTimeOffset Timestamp,
    string? Message,
    double? ReportHigh = null,
    double? ReportLow = null,
    double? ReportSpread = null,
    double? ReportAfterTax = null);

public enum DiscordNotificationType
{
    Drop,
    Recovery,
    Test,
    Report
}

public sealed class DiscordNotificationQueue
{
    private readonly Channel<DiscordNotification> _channel = Channel.CreateUnbounded<DiscordNotification>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(DiscordNotification notification, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(notification, cancellationToken);
    }

    public IAsyncEnumerable<DiscordNotification> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
