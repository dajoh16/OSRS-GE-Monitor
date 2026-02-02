using System.Collections.Concurrent;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Requests;

namespace OSRSGeMonitor.Api.Services;

public class InMemoryDataStore
{
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<int, MonitoredItem> _items = new();
    private readonly ConcurrentDictionary<int, List<PricePoint>> _priceHistory = new();
    private readonly List<Position> _positions = new();
    private readonly List<Alert> _alerts = new();

    public GlobalConfig Config { get; } = new();

    public GlobalConfig UpdateConfig(UpdateConfigRequest request)
    {
        if (request.StandardDeviationThreshold.HasValue)
        {
            Config.StandardDeviationThreshold = request.StandardDeviationThreshold.Value;
        }

        if (request.RecoveryStandardDeviationThreshold.HasValue)
        {
            Config.RecoveryStandardDeviationThreshold = request.RecoveryStandardDeviationThreshold.Value;
        }

        if (request.RollingWindowSize.HasValue)
        {
            Config.RollingWindowSize = Math.Max(1, request.RollingWindowSize.Value);
        }

        if (request.FetchIntervalSeconds.HasValue)
        {
            Config.FetchIntervalSeconds = Math.Max(5, request.FetchIntervalSeconds.Value);
        }

        return Config;
    }

    public IReadOnlyCollection<MonitoredItem> GetItems() => _items.Values.OrderBy(item => item.Id).ToArray();

    public MonitoredItem AddItem(CreateMonitoredItemRequest request)
    {
        var item = new MonitoredItem
        {
            Id = request.Id,
            Name = request.Name
        };

        _items[item.Id] = item;
        _priceHistory.TryAdd(item.Id, new List<PricePoint>());
        return item;
    }

    public bool RemoveItem(int id)
    {
        var removed = _items.TryRemove(id, out _);
        _priceHistory.TryRemove(id, out _);
        return removed;
    }

    public IReadOnlyCollection<Position> GetPositions()
    {
        lock (_lock)
        {
            return _positions.OrderByDescending(position => position.BoughtAt).ToArray();
        }
    }

    public Position AddPosition(CreatePositionRequest request)
    {
        var position = new Position
        {
            ItemId = request.ItemId,
            ItemName = request.ItemName,
            Quantity = request.Quantity,
            BuyPrice = request.BuyPrice,
            BoughtAt = request.BoughtAt ?? DateTimeOffset.UtcNow
        };

        lock (_lock)
        {
            _positions.Add(position);
        }

        return position;
    }

    public bool AcknowledgePosition(Guid id)
    {
        lock (_lock)
        {
            var position = _positions.FirstOrDefault(entry => entry.Id == id);
            if (position is null)
            {
                return false;
            }

            position.AcknowledgedAt = DateTimeOffset.UtcNow;
            return true;
        }
    }

    public IReadOnlyCollection<Alert> GetAlerts(string? status)
    {
        lock (_lock)
        {
            IEnumerable<Alert> alerts = _alerts;
            alerts = status?.ToLowerInvariant() switch
            {
                "active" => alerts.Where(alert => alert.RecoveredAt is null),
                "recovered" => alerts.Where(alert => alert.RecoveredAt is not null),
                _ => alerts
            };

            return alerts.OrderByDescending(alert => alert.TriggeredAt).ToArray();
        }
    }

    public Alert? GetAlert(Guid id)
    {
        lock (_lock)
        {
            return _alerts.FirstOrDefault(alert => alert.Id == id);
        }
    }

    public Alert AddAlert(Alert alert)
    {
        lock (_lock)
        {
            _alerts.Add(alert);
        }

        return alert;
    }

    public bool AcknowledgeAlert(Guid id)
    {
        lock (_lock)
        {
            var alert = _alerts.FirstOrDefault(entry => entry.Id == id);
            if (alert is null)
            {
                return false;
            }

            alert.AcknowledgedAt = DateTimeOffset.UtcNow;
            return true;
        }
    }

    public bool TryRecoverAlert(int itemId, double recoveredPrice)
    {
        lock (_lock)
        {
            var alert = _alerts.LastOrDefault(entry => entry.ItemId == itemId && entry.RecoveredAt is null);
            if (alert is null)
            {
                return false;
            }

            alert.RecoveredAt = DateTimeOffset.UtcNow;
            alert.RecoveredPrice = recoveredPrice;
            return true;
        }
    }

    public bool HasActiveAlert(int itemId)
    {
        lock (_lock)
        {
            return _alerts.Any(alert => alert.ItemId == itemId && alert.RecoveredAt is null);
        }
    }

    public void AddPricePoint(int itemId, PricePoint point)
    {
        var history = _priceHistory.GetOrAdd(itemId, _ => new List<PricePoint>());
        lock (_lock)
        {
            history.Add(point);
            var max = Math.Max(Config.RollingWindowSize, 1);
            if (history.Count > max)
            {
                history.RemoveRange(0, history.Count - max);
            }
        }
    }

    public (double Mean, double StandardDeviation, int SampleSize) GetRollingStats(int itemId)
    {
        if (!_priceHistory.TryGetValue(itemId, out var history))
        {
            return (0, 0, 0);
        }

        List<PricePoint> snapshot;
        lock (_lock)
        {
            snapshot = history.ToList();
        }

        if (snapshot.Count == 0)
        {
            return (0, 0, 0);
        }

        var prices = snapshot.Select(point => point.Price).ToArray();
        var mean = prices.Average();
        var variance = prices.Sum(price => Math.Pow(price - mean, 2)) / prices.Length;
        var stdDev = Math.Sqrt(variance);
        return (mean, stdDev, prices.Length);
    }
}
