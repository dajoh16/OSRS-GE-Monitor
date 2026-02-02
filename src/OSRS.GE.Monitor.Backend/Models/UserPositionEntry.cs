namespace OSRS.GE.Monitor.Backend.Models;

public sealed record UserPositionEntry(
    long Id,
    long ItemId,
    long Quantity,
    long BuyPrice,
    DateTimeOffset BuyTime
);
