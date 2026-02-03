namespace OSRSGeMonitor.Api.Models.Requests;

public sealed record CreatePositionRequest(
    int ItemId,
    string ItemName,
    int Quantity,
    double BuyPrice,
    DateTimeOffset? BoughtAt);
