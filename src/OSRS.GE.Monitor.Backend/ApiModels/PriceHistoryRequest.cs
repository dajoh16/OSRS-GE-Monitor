namespace OSRS.GE.Monitor.Backend.ApiModels;

public sealed record PriceHistoryRequest(
    long ItemId,
    DateTimeOffset Timestamp,
    long BuyPrice,
    long SellPrice
);
