namespace OSRSGeMonitor.Api.Models;

public class MonitoredItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
