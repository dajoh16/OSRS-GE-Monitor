namespace OSRSGeMonitor.Api.Models.Responses;

public sealed record ItemDetailsDto(
    int Id,
    string Name,
    string Examine,
    bool Members,
    int? BuyLimit,
    int? LowAlch,
    int? HighAlch,
    int? Value,
    string Icon,
    ItemTrendDto? Trend,
    LatestPriceDto? Latest);

public sealed record ItemTrendDto(
    string Window,
    string Direction,
    double? PercentChange,
    double? LatestPrice,
    double? PreviousPrice);
