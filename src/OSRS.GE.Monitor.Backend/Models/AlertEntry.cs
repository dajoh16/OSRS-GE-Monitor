namespace OSRS.GE.Monitor.Backend.Models;

public sealed record AlertEntry(
    long Id,
    long ItemId,
    DateTimeOffset Timestamp,
    decimal Deviation,
    string Status
);
