namespace OSRSGeMonitor.Api.Models.Responses;

public sealed record WatchlistMarketDto(
    int ItemId,
    double? High,
    double? Low,
    DateTimeOffset? HighTime,
    DateTimeOffset? LowTime,
    int? BuyLimit);
