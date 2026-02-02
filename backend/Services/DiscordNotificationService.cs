using OSRSGeMonitor.Api.Models;

namespace OSRSGeMonitor.Api.Services;

public sealed class DiscordNotificationService
{
    private readonly DiscordNotificationQueue _queue;

    public DiscordNotificationService(DiscordNotificationQueue queue)
    {
        _queue = queue;
    }

    public ValueTask EnqueueDropAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        var notification = new DiscordNotification(
            DiscordNotificationType.Drop,
            alert.ItemName,
            alert.TriggerPrice,
            alert.Mean,
            alert.StandardDeviation,
            null,
            alert.TriggeredAt,
            null);
        return _queue.EnqueueAsync(notification, cancellationToken);
    }

    public ValueTask EnqueueRecoveryAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        var notification = new DiscordNotification(
            DiscordNotificationType.Recovery,
            alert.ItemName,
            alert.TriggerPrice,
            alert.Mean,
            alert.StandardDeviation,
            alert.RecoveredPrice,
            alert.RecoveredAt ?? DateTimeOffset.UtcNow,
            null);
        return _queue.EnqueueAsync(notification, cancellationToken);
    }

    public ValueTask EnqueueTestAsync(string message, CancellationToken cancellationToken = default)
    {
        var notification = new DiscordNotification(
            DiscordNotificationType.Test,
            "Test",
            0,
            0,
            0,
            null,
            DateTimeOffset.UtcNow,
            message);
        return _queue.EnqueueAsync(notification, cancellationToken);
    }
}
