namespace OSRS.GE.Monitor.Backend.Models;

public sealed record ItemCatalogEntry(
    long Id,
    string Name,
    string? Metadata
);
