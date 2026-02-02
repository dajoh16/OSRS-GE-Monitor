namespace OSRSGeMonitor.Api.Models;

public class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public double TriggerPrice { get; set; }
    public double Mean { get; set; }
    public double StandardDeviation { get; set; }
    public DateTimeOffset TriggeredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RecoveredAt { get; set; }
    public double? RecoveredPrice { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
}
