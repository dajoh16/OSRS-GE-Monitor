namespace OSRSGeMonitor.Api.Models.Requests;

public class CreateMonitoredItemRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
