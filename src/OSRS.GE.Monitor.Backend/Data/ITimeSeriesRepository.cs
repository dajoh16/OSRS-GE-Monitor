using OSRS.GE.Monitor.Backend.Models;

namespace OSRS.GE.Monitor.Backend.Data;

public interface ITimeSeriesRepository
{
    Task UpsertItemCatalogAsync(ItemCatalogEntry entry, CancellationToken cancellationToken = default);
    Task InsertPriceHistoryAsync(PriceHistoryEntry entry, CancellationToken cancellationToken = default);
    Task<PriceHistoryEntry?> GetLatestPriceAsync(long itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceHistoryEntry>> GetPriceHistoryAsync(long itemId, DateTimeOffset since, CancellationToken cancellationToken = default);

    Task<long> CreateAlertAsync(long itemId, DateTimeOffset timestamp, decimal deviation, string status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertEntry>> GetOpenAlertsAsync(long itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertEntry>> GetAlertsForRecoveryAsync(DateTimeOffset since, CancellationToken cancellationToken = default);
    Task UpdateAlertStatusAsync(long alertId, string status, CancellationToken cancellationToken = default);

    Task UpsertUserPositionAsync(UserPositionEntry entry, CancellationToken cancellationToken = default);

    Task<GlobalConfiguration?> GetGlobalConfigurationAsync(CancellationToken cancellationToken = default);
    Task UpsertGlobalConfigurationAsync(GlobalConfiguration config, CancellationToken cancellationToken = default);
}
