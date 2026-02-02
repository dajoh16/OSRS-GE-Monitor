using System.Collections.Concurrent;
using OSRSGeMonitor.Api.Models;
using OSRSGeMonitor.Api.Models.Requests;

namespace OSRSGeMonitor.Api.Services;

public class InMemoryDataStore
{
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<int, MonitoredItem> _items = new();
    private readonly List<Position> _positions = new();
    private readonly List<Alert> _alerts = new();
    private readonly List<Notification> _notifications = new();
    private readonly SqliteWatchlistStore _watchlistStore;
    private readonly SqliteConfigStore _configStore;
    private readonly DiscordNotificationService _discordNotificationService;

    public InMemoryDataStore(
        SqliteWatchlistStore watchlistStore,
        SqliteConfigStore configStore,
        DiscordNotificationService discordNotificationService)
    {
        _watchlistStore = watchlistStore;
        _configStore = configStore;
        _discordNotificationService = discordNotificationService;
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

    public bool RemovePosition(Guid id)
    {
        lock (_lock)
        {
            var index = _positions.FindIndex(entry => entry.Id == id);
            if (index < 0)
            {
                return false;
            }

            _positions.RemoveAt(index);
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

    public bool AcknowledgeAlert(Guid id, int quantity, out Position? position)
    {
        position = null;
        lock (_lock)
        {
            var alert = _alerts.FirstOrDefault(entry => entry.Id == id);
            if (alert is null)
            {
                return false;
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
            return true;
        }
    }

    public bool TryRecoverAlert(int itemId, double recoveredPrice)
    {
        Alert? alert;
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

            foreach (var position in _positions.Where(entry => entry.ItemId == itemId && entry.RecoveredAt is null))
            {
                position.RecoveredAt = alert.RecoveredAt;
                position.RecoveryPrice = recoveredPrice;
            }
        }

        if (alert is not null && Config.DiscordNotificationsEnabled && !string.IsNullOrWhiteSpace(Config.DiscordWebhookUrl))
        {
            _ = _discordNotificationService.EnqueueRecoveryAsync(alert);
        }

        return true;
    }

    public bool HasActiveAlert(int itemId)
    {
        lock (_lock)
        {
            return _alerts.Any(alert => alert.ItemId == itemId && alert.RecoveredAt is null);
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

            _notifications.RemoveAt(index);
            return true;
        }
    }

    public void ClearNotifications()
    {
        lock (_lock)
        {
            _notifications.Clear();
        }
    }
}
