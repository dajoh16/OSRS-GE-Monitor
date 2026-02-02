using OSRS.GE.Monitor.Backend.Models;

namespace OSRS.GE.Monitor.Backend.Data;

public sealed class InMemoryTimeSeriesRepository : ITimeSeriesRepository
{
    private readonly object _lock = new();
    private readonly Dictionary<long, ItemCatalogEntry> _items = new();
    private readonly List<PriceHistoryEntry> _priceHistory = new();
    private readonly List<AlertEntry> _alerts = new();
    private readonly Dictionary<long, UserPositionEntry> _positions = new();
    private GlobalConfiguration? _configuration;
    private long _alertIdSeed = 1;

    public Task UpsertItemCatalogAsync(ItemCatalogEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _items[entry.Id] = entry;
        }

        return Task.CompletedTask;
    }

    public Task InsertPriceHistoryAsync(PriceHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _priceHistory.Add(entry);
        }

        return Task.CompletedTask;
    }

    public Task<PriceHistoryEntry?> GetLatestPriceAsync(long itemId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var latest = _priceHistory
                .Where(entry => entry.ItemId == itemId)
                .OrderByDescending(entry => entry.Timestamp)
                .FirstOrDefault();

            return Task.FromResult<PriceHistoryEntry?>(latest);
        }
    }

    public Task<IReadOnlyList<PriceHistoryEntry>> GetPriceHistoryAsync(long itemId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var results = _priceHistory
                .Where(entry => entry.ItemId == itemId && entry.Timestamp >= since)
                .OrderBy(entry => entry.Timestamp)
                .ToList();

            return Task.FromResult<IReadOnlyList<PriceHistoryEntry>>(results);
        }
    }

    public Task<long> CreateAlertAsync(long itemId, DateTimeOffset timestamp, decimal deviation, string status, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var alert = new AlertEntry(_alertIdSeed++, itemId, timestamp, deviation, status);
            _alerts.Add(alert);
            return Task.FromResult(alert.Id);
        }
    }

    public Task<IReadOnlyList<AlertEntry>> GetOpenAlertsAsync(long itemId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var results = _alerts
                .Where(alert => alert.ItemId == itemId && alert.Status == "open")
                .OrderByDescending(alert => alert.Timestamp)
                .ToList();

            return Task.FromResult<IReadOnlyList<AlertEntry>>(results);
        }
    }

    public Task<IReadOnlyList<AlertEntry>> GetAlertsForRecoveryAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var results = _alerts
                .Where(alert => alert.Status == "open" && alert.Timestamp >= since)
                .OrderBy(alert => alert.Timestamp)
                .ToList();

            return Task.FromResult<IReadOnlyList<AlertEntry>>(results);
        }
    }

    public Task UpdateAlertStatusAsync(long alertId, string status, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var index = _alerts.FindIndex(alert => alert.Id == alertId);
            if (index >= 0)
            {
                var existing = _alerts[index];
                _alerts[index] = existing with { Status = status };
            }
        }

        return Task.CompletedTask;
    }

    public Task UpsertUserPositionAsync(UserPositionEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _positions[entry.Id] = entry;
        }

        return Task.CompletedTask;
    }

    public Task<GlobalConfiguration?> GetGlobalConfigurationAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_configuration);
        }
    }

    public Task UpsertGlobalConfigurationAsync(GlobalConfiguration config, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _configuration = config;
        }

        return Task.CompletedTask;
    }
}
