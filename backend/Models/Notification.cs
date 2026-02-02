namespace OSRSGeMonitor.Api.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public static Notification ForDrop(Alert alert)
    {
        return new Notification
        {
            Type = "drop",
            Title = $"{alert.ItemName} dropped",
            Message = $"Triggered at {alert.TriggerPrice:N0} gp (mean {alert.Mean:N0}, sigma {alert.StandardDeviation:N2}).",
            CreatedAt = alert.TriggeredAt
        };
    }

    public static Notification ForRecovery(Alert alert)
    {
        var price = alert.RecoveredPrice ?? 0;
        return new Notification
        {
            Type = "recovery",
            Title = $"{alert.ItemName} recovered",
            Message = $"Recovered at {price:N0} gp after the drop alert.",
            CreatedAt = alert.RecoveredAt ?? DateTimeOffset.UtcNow
        };
    }
}
