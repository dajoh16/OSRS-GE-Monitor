namespace OSRSGeMonitor.Api.Models.Requests;

public sealed record IncreasePositionQuantityRequest(
    double BuyPrice,
    int Quantity);
