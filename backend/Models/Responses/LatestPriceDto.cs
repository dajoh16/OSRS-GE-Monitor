namespace OSRSGeMonitor.Api.Models.Responses;

public sealed record LatestPriceDto(
    int ItemId,
    double? High,
    double? Low,
    DateTimeOffset? HighTime,
    DateTimeOffset? LowTime);
