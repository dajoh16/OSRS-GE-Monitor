namespace OSRS.GE.Monitor.Backend.ApiModels;

public sealed record AlertResponse(
    long Id,
    long ItemId,
    DateTimeOffset Timestamp,
    decimal Deviation,
    string Status
);
