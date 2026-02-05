using System.Collections.Concurrent;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Requests;
using OSRSGeMonitor.Api.Models.Responses;

namespace OSRSGeMonitor.Api.Services;

public class InMemoryDataStore
{
    public sealed record LatestPriceSnapshot(
        int ItemId,
        double? High,
        double? Low,
        DateTimeOffset? HighTime,
        DateTimeOffset? LowTime);
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<int, MonitoredItem> _items = new();
    private readonly List<Position> _positions = new();
    private readonly List<Alert> _alerts = new();
    private readonly List<Notification> _notifications = new();
    private readonly HashSet<int> _suppressedDropItems = new();
    private readonly Dictionary<int, LatestPriceSnapshot> _latestPrices = new();
    private readonly SqliteWatchlistStore _watchlistStore;
    private readonly SqliteConfigStore _configStore;
    private readonly DiscordNotificationService _discordNotificationService;
    private readonly SqlitePositionStore _positionStore;

    public InMemoryDataStore(
        SqliteWatchlistStore watchlistStore,
        SqliteConfigStore configStore,
        DiscordNotificationService discordNotificationService,
        SqlitePositionStore positionStore)
    {
        _watchlistStore = watchlistStore;
        _configStore = configStore;
        _discordNotificationService = discordNotificationService;
        _positionStore = positionStore;
        foreach (var item in _watchlistStore.GetItems())
        {
            _items[item.Id] = item;
        }
    }

    public GlobalConfig Config { get; } = new();

    public async Task LoadConfigAsync(CancellationToken cancellationToken = default)
    {
        var persisted = await _configStore.LoadAsync(cancellationToken);
        if (persisted is null)
        {
            return;
        }

        Config.StandardDeviationThreshold = persisted.StandardDeviationThreshold;
        Config.ProfitTargetPercent = persisted.ProfitTargetPercent;
        Config.RecoveryStandardDeviationThreshold = persisted.RecoveryStandardDeviationThreshold;
        Config.RollingWindowSize = persisted.RollingWindowSize;
        Config.FetchIntervalSeconds = persisted.FetchIntervalSeconds;
        Config.UserAgent = persisted.UserAgent;
        Config.DiscordNotificationsEnabled = persisted.DiscordNotificationsEnabled;
        Config.DiscordWebhookUrl = persisted.DiscordWebhookUrl;
        Config.AlertGraceMinutes = persisted.AlertGraceMinutes;
    }

    public async Task LoadPositionsAsync(CancellationToken cancellationToken = default)
    {
        var persisted = await _positionStore.GetAllAsync(cancellationToken);
        lock (_lock)
        {
            _positions.Clear();
            _positions.AddRange(persisted);
        }
    }

    public async Task<GlobalConfig> UpdateConfigAsync(UpdateConfigRequest request, CancellationToken cancellationToken = default)
    {
        if (request.StandardDeviationThreshold.HasValue)
        {
            Config.StandardDeviationThreshold = request.StandardDeviationThreshold.Value;
        }

        if (request.ProfitTargetPercent.HasValue)
        {
            Config.ProfitTargetPercent = Math.Max(0, request.ProfitTargetPercent.Value);
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

        if (!string.IsNullOrWhiteSpace(request.UserAgent))
        {
            Config.UserAgent = request.UserAgent.Trim();
        }

        if (request.DiscordNotificationsEnabled.HasValue)
        {
            Config.DiscordNotificationsEnabled = request.DiscordNotificationsEnabled.Value;
        }

        if (request.DiscordWebhookUrl is not null)
        {
            Config.DiscordWebhookUrl = request.DiscordWebhookUrl.Trim();
        }

        if (request.AlertGraceMinutes.HasValue)
        {
            Config.AlertGraceMinutes = Math.Max(0, request.AlertGraceMinutes.Value);
        }

        await _configStore.SaveAsync(Config, cancellationToken);
        return Config;
    }

    public IReadOnlyCollection<MonitoredItem> GetItems() => _items.Values.OrderBy(item => item.Id).ToArray();

    public MonitoredItem? GetItem(int id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }

    public MonitoredItem AddItem(CreateMonitoredItemRequest request)
    {
        if (_items.TryGetValue(request.Id, out var existing))
        {
            return existing;
        }

        var item = new MonitoredItem
        {
            Id = request.Id,
            Name = request.Name,
            AddedAt = DateTimeOffset.UtcNow
        };

        _items[item.Id] = item;
        _watchlistStore.UpsertItem(item);
        return item;
    }

    public bool RemoveItem(int id)
    {
        var removed = _items.TryRemove(id, out _);
        if (removed)
        {
            _watchlistStore.RemoveItem(id);
        }
        return removed;
    }

    public IReadOnlyCollection<Position> GetPositions()
    {
        lock (_lock)
        {
            return _positions.OrderByDescending(position => position.BoughtAt).ToArray();
        }
    }

    public Position? GetPosition(Guid id)
    {
        lock (_lock)
        {
            return _positions.FirstOrDefault(entry => entry.Id == id);
        }
    }

    public async Task<Position> AddPositionAsync(CreatePositionRequest request, CancellationToken cancellationToken = default)
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

        await _positionStore.UpsertAsync(position, cancellationToken);
        return position;
    }

    public async Task<bool> AcknowledgePositionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Position? position;
        lock (_lock)
        {
            position = _positions.FirstOrDefault(entry => entry.Id == id);
            if (position is null)
            {
                return false;
            }

            position.AcknowledgedAt = DateTimeOffset.UtcNow;
        }

        await _positionStore.UpsertAsync(position, cancellationToken);
        return true;
    }

    public async Task<bool> RemovePositionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var index = _positions.FindIndex(entry => entry.Id == id);
            if (index < 0)
            {
                return false;
            }

            _positions.RemoveAt(index);
        }

        return await _positionStore.RemoveAsync(id, cancellationToken);
    }

    public IReadOnlyCollection<Alert> GetAlerts(string? status)
    {
        lock (_lock)
        {
            IEnumerable<Alert> alerts = _alerts;
            alerts = status?.ToLowerInvariant() switch
            {
                "active" => FilterActiveWithGrace(alerts),
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
            _notifications.Add(Notification.ForDrop(alert));
        }

        if (Config.DiscordNotificationsEnabled && !string.IsNullOrWhiteSpace(Config.DiscordWebhookUrl))
        {
            _ = _discordNotificationService.EnqueueDropAsync(alert);
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

    private IEnumerable<Alert> FilterActiveWithGrace(IEnumerable<Alert> alerts)
    {
        var graceMinutes = Math.Max(0, Config.AlertGraceMinutes);
        if (graceMinutes <= 0)
        {
            return alerts.Where(alert => alert.RecoveredAt is null);
        }

        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-graceMinutes);
        return alerts.Where(alert => alert.RecoveredAt is null || alert.RecoveredAt >= cutoff);
    }

    public bool RemoveAlert(Guid id)
    {
        lock (_lock)
        {
            var index = _alerts.FindIndex(entry => entry.Id == id);
            if (index < 0)
            {
                return false;
            }

            _alerts.RemoveAt(index);
            return true;
        }
    }

    public async Task<Position?> AcknowledgeAlertAsync(Guid id, int quantity, CancellationToken cancellationToken = default)
    {
        Position? position = null;
        lock (_lock)
        {
            var alert = _alerts.FirstOrDefault(entry => entry.Id == id);
            if (alert is null)
            {
                return null;
            }

            if (alert.AcknowledgedAt is null)
            {
                alert.AcknowledgedAt = DateTimeOffset.UtcNow;
            }

            position = new Position
            {
                ItemId = alert.ItemId,
                ItemName = alert.ItemName,
                Quantity = quantity,
                BuyPrice = alert.TriggerPrice,
                BoughtAt = DateTimeOffset.UtcNow
            };
            _positions.Add(position);
        }

        if (position is not null)
        {
            await _positionStore.UpsertAsync(position, cancellationToken);
        }

        return position;
    }

    public async Task<bool> TryRecoverAlertAsync(int itemId, double recoveredPrice, CancellationToken cancellationToken = default)
    {
        Alert? alert;
        List<Position> updatedPositions = new();
        lock (_lock)
        {
            alert = _alerts.LastOrDefault(entry => entry.ItemId == itemId && entry.RecoveredAt is null);
            if (alert is null)
            {
                return false;
            }

            alert.RecoveredAt = DateTimeOffset.UtcNow;
            alert.RecoveredPrice = recoveredPrice;
            _notifications.Add(Notification.ForRecovery(alert));
            _suppressedDropItems.Remove(itemId);

            foreach (var position in _positions.Where(entry => entry.ItemId == itemId && entry.RecoveredAt is null))
            {
                position.RecoveredAt = alert.RecoveredAt;
                position.RecoveryPrice = recoveredPrice;
                updatedPositions.Add(position);
            }
        }

        if (alert is not null && Config.DiscordNotificationsEnabled && !string.IsNullOrWhiteSpace(Config.DiscordWebhookUrl))
        {
            _ = _discordNotificationService.EnqueueRecoveryAsync(alert);
        }

        foreach (var position in updatedPositions)
        {
            await _positionStore.UpsertAsync(position, cancellationToken);
        }

        return true;
    }

    public async Task<Position?> SellPositionAsync(
        Guid id,
        double sellPrice,
        int sellQuantity,
        CancellationToken cancellationToken = default)
    {
        Position? position;
        Position? soldPosition = null;
        lock (_lock)
        {
            position = _positions.FirstOrDefault(entry => entry.Id == id);
            if (position is null || position.SoldAt.HasValue)
            {
                return null;
            }

            if (sellQuantity <= 0 || sellQuantity > position.Quantity)
            {
                return null;
            }

            if (sellQuantity == position.Quantity)
            {
                var taxPerItem = CalculateTaxPerItem(sellPrice);
                var taxPaid = taxPerItem * position.Quantity;
                var gross = sellPrice * position.Quantity;
                var cost = position.BuyPrice * position.Quantity;
                var profit = gross - cost - taxPaid;

                position.SellPrice = sellPrice;
                position.SoldAt = DateTimeOffset.UtcNow;
                position.TaxRateApplied = 0.02;
                position.TaxPaid = taxPaid;
                position.Profit = profit;
                soldPosition = position;
            }
            else
            {
                var taxPerItem = CalculateTaxPerItem(sellPrice);
                var taxPaid = taxPerItem * sellQuantity;
                var gross = sellPrice * sellQuantity;
                var cost = position.BuyPrice * sellQuantity;
                var profit = gross - cost - taxPaid;

                soldPosition = new Position
                {
                    ItemId = position.ItemId,
                    ItemName = position.ItemName,
                    Quantity = sellQuantity,
                    BuyPrice = position.BuyPrice,
                    BoughtAt = position.BoughtAt,
                    AcknowledgedAt = position.AcknowledgedAt,
                    RecoveredAt = position.RecoveredAt,
                    RecoveryPrice = position.RecoveryPrice,
                    SellPrice = sellPrice,
                    SoldAt = DateTimeOffset.UtcNow,
                    TaxRateApplied = 0.02,
                    TaxPaid = taxPaid,
                    Profit = profit
                };

                position.Quantity -= sellQuantity;
                _positions.Add(soldPosition);
            }
        }

        if (soldPosition is null)
        {
            return null;
        }

        await _positionStore.UpsertAsync(soldPosition, cancellationToken);
        if (!ReferenceEquals(soldPosition, position))
        {
            await _positionStore.UpsertAsync(position!, cancellationToken);
        }

        return soldPosition;
    }

    public async Task<Position?> UpdateBuyPriceAsync(Guid id, double buyPrice, CancellationToken cancellationToken = default)
    {
        Position? position;
        lock (_lock)
        {
            position = _positions.FirstOrDefault(entry => entry.Id == id);
            if (position is null)
            {
                return null;
            }

            position.BuyPrice = buyPrice;

            if (position.SoldAt.HasValue && position.SellPrice.HasValue)
            {
                var taxPaid = position.TaxPaid ?? CalculateTaxPerItem(position.SellPrice.Value) * position.Quantity;
                position.TaxPaid = taxPaid;
                position.TaxRateApplied = 0.02;
                var gross = position.SellPrice.Value * position.Quantity;
                var cost = position.BuyPrice * position.Quantity;
                position.Profit = gross - cost - taxPaid;
            }
        }

        await _positionStore.UpsertAsync(position, cancellationToken);
        return position;
    }

    public async Task<Position?> IncreasePositionQuantityAsync(
        Guid id,
        int quantity,
        double buyPrice,
        CancellationToken cancellationToken = default)
    {
        Position? position;
        lock (_lock)
        {
            position = _positions.FirstOrDefault(entry => entry.Id == id);
            if (position is null || position.SoldAt.HasValue)
            {
                return null;
            }

            if (quantity <= 0)
            {
                return null;
            }

            if (position.BuyPrice != buyPrice)
            {
                return null;
            }

            position.Quantity += quantity;
        }

        await _positionStore.UpsertAsync(position, cancellationToken);
        return position;
    }

    public PositionSummaryDto GetPositionSummary()
    {
        List<Position> soldPositions;
        lock (_lock)
        {
            soldPositions = _positions.Where(entry => entry.SoldAt.HasValue).ToList();
        }

        var totalProfit = soldPositions.Sum(entry => entry.Profit ?? 0);
        var totalTax = soldPositions.Sum(entry => entry.TaxPaid ?? 0);
        var perItem = soldPositions
            .GroupBy(entry => new { entry.ItemId, entry.ItemName })
            .Select(group =>
            {
                var count = group.Count();
                var total = group.Sum(entry => entry.Profit ?? 0);
                var avg = count == 0 ? 0 : total / count;
                var wins = group.Count(entry => (entry.Profit ?? 0) > 0);
                var winRate = count == 0 ? 0 : (double)wins / count;
                return new ItemProfitDto(group.Key.ItemId, group.Key.ItemName, count, total, avg, winRate);
            })
            .OrderByDescending(entry => entry.TotalProfit)
            .ToArray();

        return new PositionSummaryDto(totalProfit, totalTax, perItem);
    }

    public IReadOnlyCollection<ProfitPointDto> GetProfitHistory(int? itemId)
    {
        List<Position> soldPositions;
        lock (_lock)
        {
            soldPositions = _positions
                .Where(entry => entry.SoldAt.HasValue)
                .Where(entry => itemId is null || entry.ItemId == itemId.Value)
                .ToList();
        }

        var daily = soldPositions
            .GroupBy(entry => entry.SoldAt!.Value.Date)
            .Select(group => new
            {
                Date = group.Key,
                Profit = group.Sum(entry => entry.Profit ?? 0)
            })
            .OrderBy(entry => entry.Date)
            .ToList();

        var running = 0.0;
        var points = new List<ProfitPointDto>();
        foreach (var day in daily)
        {
            running += day.Profit;
            points.Add(new ProfitPointDto(day.Date.ToString("yyyy-MM-dd"), running));
        }

        return points;
    }

    private static double CalculateTaxPerItem(double sellPrice)
    {
        if (sellPrice < 100)
        {
            return 0;
        }

        var tax = Math.Floor(sellPrice * 0.02);
        return Math.Min(tax, 5_000_000);
    }

    public bool HasActiveAlert(int itemId)
    {
        lock (_lock)
        {
            return _alerts.Any(alert => alert.ItemId == itemId && alert.RecoveredAt is null);
        }
    }

    public void UpdateLatestPrice(int itemId, LatestPriceSnapshot snapshot)
    {
        lock (_lock)
        {
            _latestPrices[itemId] = snapshot;
        }
    }

    public LatestPriceSnapshot? GetLatestPrice(int itemId)
    {
        lock (_lock)
        {
            return _latestPrices.TryGetValue(itemId, out var snapshot) ? snapshot : null;
        }
    }

    public IReadOnlyCollection<LatestPriceSnapshot> GetLatestPrices(IEnumerable<int> itemIds)
    {
        lock (_lock)
        {
            return itemIds
                .Where(id => _latestPrices.ContainsKey(id))
                .Select(id => _latestPrices[id] with { ItemId = id })
                .ToArray();
        }
    }

    public IReadOnlyCollection<Notification> GetNotifications()
    {
        lock (_lock)
        {
            return _notifications.OrderByDescending(notification => notification.CreatedAt).ToArray();
        }
    }

    public bool RemoveNotification(Guid id)
    {
        lock (_lock)
        {
            var index = _notifications.FindIndex(notification => notification.Id == id);
            if (index < 0)
            {
                return false;
            }

            var notification = _notifications[index];
            if (IsDropNotification(notification))
            {
                _suppressedDropItems.Add(notification.ItemId);
            }
            else if (IsRecoveryNotification(notification))
            {
                _suppressedDropItems.Remove(notification.ItemId);
            }

            _notifications.RemoveAt(index);
            return true;
        }
    }

    public void ClearNotifications()
    {
        lock (_lock)
        {
            foreach (var notification in _notifications)
            {
                if (IsDropNotification(notification))
                {
                    _suppressedDropItems.Add(notification.ItemId);
                }
            }
            _notifications.Clear();
        }
    }

    public IReadOnlyCollection<int> GetSuppressedDropItems()
    {
        lock (_lock)
        {
            return _suppressedDropItems.OrderBy(id => id).ToArray();
        }
    }

    public bool IsDropSuppressed(int itemId)
    {
        lock (_lock)
        {
            return _suppressedDropItems.Contains(itemId);
        }
    }

    public void ClearDropSuppression(int itemId)
    {
        lock (_lock)
        {
            _suppressedDropItems.Remove(itemId);
        }
    }

    private static bool IsDropNotification(Notification notification)
    {
        return notification.ItemId > 0 && string.Equals(notification.Type, "drop", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRecoveryNotification(Notification notification)
    {
        return notification.ItemId > 0 && string.Equals(notification.Type, "recovery", StringComparison.OrdinalIgnoreCase);
    }
}
