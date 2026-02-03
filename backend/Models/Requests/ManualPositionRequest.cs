namespace OSRSGeMonitor.Api.Models.Requests;

public sealed record ManualPositionRequest(
    string ItemName,
    int? ItemId,
    int Quantity,
    double BuyPrice,
    DateTimeOffset? BoughtAt);
