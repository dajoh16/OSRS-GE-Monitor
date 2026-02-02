namespace OSRS.GE.Monitor.Backend.Data;

public sealed class TimeSeriesRepositoryOptions
{
    public string Provider { get; init; } = "InMemory";
    public string? ConnectionString { get; init; }
}
