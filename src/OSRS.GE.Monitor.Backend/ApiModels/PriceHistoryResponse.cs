namespace OSRS.GE.Monitor.Backend.ApiModels;

public sealed record PriceHistoryResponse(
    long ItemId,
    DateTimeOffset Timestamp,
    long BuyPrice,
    long SellPrice
);
