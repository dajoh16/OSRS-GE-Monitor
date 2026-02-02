namespace OSRSGeMonitor.Api.Models.Requests;

public class CreatePositionRequest
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double BuyPrice { get; set; }
    public DateTimeOffset? BoughtAt { get; set; }
}
