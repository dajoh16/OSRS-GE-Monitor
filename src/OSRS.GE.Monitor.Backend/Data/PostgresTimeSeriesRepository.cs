using System.Globalization;
using Npgsql;
using OSRS.GE.Monitor.Backend.Models;

namespace OSRS.GE.Monitor.Backend.Data;

public sealed class PostgresTimeSeriesRepository : ITimeSeriesRepository
{
    private readonly string _connectionString;

    public PostgresTimeSeriesRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public async Task UpsertItemCatalogAsync(ItemCatalogEntry entry, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into items_catalog (id, name, metadata)
            values (@id, @name, @metadata)
            on conflict (id) do update
                set name = excluded.name,
                    metadata = excluded.metadata;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", entry.Id);
        command.Parameters.AddWithValue("name", entry.Name);
        command.Parameters.AddWithValue("metadata", (object?)entry.Metadata ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertPriceHistoryAsync(PriceHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into price_history (item_id, timestamp, buy_price, sell_price)
            values (@item_id, @timestamp, @buy_price, @sell_price);
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("item_id", entry.ItemId);
        command.Parameters.AddWithValue("timestamp", entry.Timestamp);
        command.Parameters.AddWithValue("buy_price", entry.BuyPrice);
        command.Parameters.AddWithValue("sell_price", entry.SellPrice);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<PriceHistoryEntry?> GetLatestPriceAsync(long itemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select item_id, timestamp, buy_price, sell_price
            from price_history
            where item_id = @item_id
            order by timestamp desc
            limit 1;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("item_id", itemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new PriceHistoryEntry(
            reader.GetInt64(0),
            reader.GetFieldValue<DateTimeOffset>(1),
            reader.GetInt64(2),
            reader.GetInt64(3));
    }

    public async Task<IReadOnlyList<PriceHistoryEntry>> GetPriceHistoryAsync(long itemId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select item_id, timestamp, buy_price, sell_price
            from price_history
            where item_id = @item_id
              and timestamp >= @since
            order by timestamp asc;
            """;

        var results = new List<PriceHistoryEntry>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("item_id", itemId);
        command.Parameters.AddWithValue("since", since);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new PriceHistoryEntry(
                reader.GetInt64(0),
                reader.GetFieldValue<DateTimeOffset>(1),
                reader.GetInt64(2),
                reader.GetInt64(3)));
        }

        return results;
    }

    public async Task<long> CreateAlertAsync(long itemId, DateTimeOffset timestamp, decimal deviation, string status, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into alerts (item_id, timestamp, deviation, status)
            values (@item_id, @timestamp, @deviation, @status)
            returning id;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("item_id", itemId);
        command.Parameters.AddWithValue("timestamp", timestamp);
        command.Parameters.AddWithValue("deviation", deviation);
        command.Parameters.AddWithValue("status", status);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<AlertEntry>> GetOpenAlertsAsync(long itemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, item_id, timestamp, deviation, status
            from alerts
            where item_id = @item_id
              and status = 'open'
            order by timestamp desc;
            """;

        var results = new List<AlertEntry>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("item_id", itemId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new AlertEntry(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetDecimal(3),
                reader.GetString(4)));
        }

        return results;
    }

    public async Task<IReadOnlyList<AlertEntry>> GetAlertsForRecoveryAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        const string sql = """
            select id, item_id, timestamp, deviation, status
            from alerts
            where status = 'open'
              and timestamp >= @since
            order by timestamp asc;
            """;

        var results = new List<AlertEntry>();
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("since", since);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new AlertEntry(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetDecimal(3),
                reader.GetString(4)));
        }

        return results;
    }

    public async Task UpdateAlertStatusAsync(long alertId, string status, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update alerts
            set status = @status
            where id = @id;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", alertId);
        command.Parameters.AddWithValue("status", status);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertUserPositionAsync(UserPositionEntry entry, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into user_positions (id, item_id, quantity, buy_price, buy_time)
            values (@id, @item_id, @quantity, @buy_price, @buy_time)
            on conflict (id) do update
                set item_id = excluded.item_id,
                    quantity = excluded.quantity,
                    buy_price = excluded.buy_price,
                    buy_time = excluded.buy_time;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", entry.Id);
        command.Parameters.AddWithValue("item_id", entry.ItemId);
        command.Parameters.AddWithValue("quantity", entry.Quantity);
        command.Parameters.AddWithValue("buy_price", entry.BuyPrice);
        command.Parameters.AddWithValue("buy_time", entry.BuyTime);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<GlobalConfiguration?> GetGlobalConfigurationAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            select standard_deviation_threshold
            from global_configuration
            limit 1;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new GlobalConfiguration(reader.GetDecimal(0));
    }

    public async Task UpsertGlobalConfigurationAsync(GlobalConfiguration config, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into global_configuration (id, standard_deviation_threshold)
            values (1, @threshold)
            on conflict (id) do update
                set standard_deviation_threshold = excluded.standard_deviation_threshold;
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("threshold", config.StandardDeviationThreshold);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
