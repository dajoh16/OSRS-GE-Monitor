namespace OSRSGeMonitor.Api.Models.Responses;

public sealed record PositionSummaryDto(
    double TotalProfit,
    double TotalTax,
    IReadOnlyCollection<ItemProfitDto> PerItem);

public sealed record ItemProfitDto(
    int ItemId,
    string ItemName,
    int Count,
    double TotalProfit,
    double AverageProfit,
    double WinRate);
