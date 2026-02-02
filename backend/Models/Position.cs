namespace OSRSGeMonitor.Api.Models;

public class Position
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double BuyPrice { get; set; }
    public DateTimeOffset BoughtAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcknowledgedAt { get; set; }
}
