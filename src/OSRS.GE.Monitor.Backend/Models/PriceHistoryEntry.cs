namespace OSRS.GE.Monitor.Backend.Models;

public sealed record PriceHistoryEntry(
    long ItemId,
    DateTimeOffset Timestamp,
    long BuyPrice,
    long SellPrice
);
