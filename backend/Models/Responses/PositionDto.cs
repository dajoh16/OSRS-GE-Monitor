namespace OSRSGeMonitor.Api.Models.Responses;

public sealed record PositionDto(
    Guid Id,
    int ItemId,
    string ItemName,
    int Quantity,
    double BuyPrice,
    DateTimeOffset BoughtAt,
    DateTimeOffset? AcknowledgedAt,
    DateTimeOffset? RecoveredAt,
    double? RecoveryPrice,
    double? SellPrice,
    DateTimeOffset? SoldAt,
    double? TaxRateApplied,
    double? TaxPaid,
    double? Profit,
    bool IsSold);
