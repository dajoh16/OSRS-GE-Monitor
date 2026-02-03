using System.Globalization;
using Microsoft.Data.Sqlite;
using OSRSGeMonitor.Api.Models;

namespace OSRSGeMonitor.Api.Services;

public sealed class SqlitePositionStore
{
    private const string DateFormat = "O";
    private readonly string _connectionString;

    public SqlitePositionStore(IHostEnvironment env)
    {
        var dbPath = Path.Combine(env.ContentRootPath, "price-history.db");
        _connectionString = $"Data Source={dbPath}";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Positions (
                Id TEXT PRIMARY KEY,
                ItemId INTEGER NOT NULL,
                ItemName TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                BuyPrice REAL NOT NULL,
                BoughtAt TEXT NOT NULL,
                AcknowledgedAt TEXT,
                RecoveredAt TEXT,
                RecoveryPrice REAL,
                SellPrice REAL,
                SoldAt TEXT,
                TaxRateApplied REAL,
                TaxPaid REAL,
                Profit REAL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Position>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                Id,
                ItemId,
                ItemName,
                Quantity,
                BuyPrice,
                BoughtAt,
                AcknowledgedAt,
                RecoveredAt,
                RecoveryPrice,
                SellPrice,
                SoldAt,
                TaxRateApplied,
                TaxPaid,
                Profit
            FROM Positions
            ORDER BY BoughtAt DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<Position>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new Position
            {
                Id = Guid.Parse(reader.GetString(0)),
                ItemId = reader.GetInt32(1),
                ItemName = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                BuyPrice = reader.GetDouble(4),
                BoughtAt = ParseDate(reader.GetString(5)),
                AcknowledgedAt = reader.IsDBNull(6) ? null : ParseDate(reader.GetString(6)),
                RecoveredAt = reader.IsDBNull(7) ? null : ParseDate(reader.GetString(7)),
                RecoveryPrice = reader.IsDBNull(8) ? null : reader.GetDouble(8),
                SellPrice = reader.IsDBNull(9) ? null : reader.GetDouble(9),
                SoldAt = reader.IsDBNull(10) ? null : ParseDate(reader.GetString(10)),
                TaxRateApplied = reader.IsDBNull(11) ? null : reader.GetDouble(11),
                TaxPaid = reader.IsDBNull(12) ? null : reader.GetDouble(12),
                Profit = reader.IsDBNull(13) ? null : reader.GetDouble(13)
            });
        }

        return results;
    }

    public async Task UpsertAsync(Position position, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Positions (
                Id,
                ItemId,
                ItemName,
                Quantity,
                BuyPrice,
                BoughtAt,
                AcknowledgedAt,
                RecoveredAt,
                RecoveryPrice,
                SellPrice,
                SoldAt,
                TaxRateApplied,
                TaxPaid,
                Profit
            )
            VALUES (
                $id,
                $itemId,
                $itemName,
                $quantity,
                $buyPrice,
                $boughtAt,
                $acknowledgedAt,
                $recoveredAt,
                $recoveryPrice,
                $sellPrice,
                $soldAt,
                $taxRateApplied,
                $taxPaid,
                $profit
            )
            ON CONFLICT(Id) DO UPDATE SET
                ItemId = excluded.ItemId,
                ItemName = excluded.ItemName,
                Quantity = excluded.Quantity,
                BuyPrice = excluded.BuyPrice,
                BoughtAt = excluded.BoughtAt,
                AcknowledgedAt = excluded.AcknowledgedAt,
                RecoveredAt = excluded.RecoveredAt,
                RecoveryPrice = excluded.RecoveryPrice,
                SellPrice = excluded.SellPrice,
                SoldAt = excluded.SoldAt,
                TaxRateApplied = excluded.TaxRateApplied,
                TaxPaid = excluded.TaxPaid,
                Profit = excluded.Profit;
            """;

        command.Parameters.AddWithValue("$id", position.Id.ToString());
        command.Parameters.AddWithValue("$itemId", position.ItemId);
        command.Parameters.AddWithValue("$itemName", position.ItemName);
        command.Parameters.AddWithValue("$quantity", position.Quantity);
        command.Parameters.AddWithValue("$buyPrice", position.BuyPrice);
        command.Parameters.AddWithValue("$boughtAt", position.BoughtAt.ToString(DateFormat, CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$acknowledgedAt", ToDbString(position.AcknowledgedAt));
        command.Parameters.AddWithValue("$recoveredAt", ToDbString(position.RecoveredAt));
        command.Parameters.AddWithValue("$recoveryPrice", ToDbValue(position.RecoveryPrice));
        command.Parameters.AddWithValue("$sellPrice", ToDbValue(position.SellPrice));
        command.Parameters.AddWithValue("$soldAt", ToDbString(position.SoldAt));
        command.Parameters.AddWithValue("$taxRateApplied", ToDbValue(position.TaxRateApplied));
        command.Parameters.AddWithValue("$taxPaid", ToDbValue(position.TaxPaid));
        command.Parameters.AddWithValue("$profit", ToDbValue(position.Profit));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Positions WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static DateTimeOffset ParseDate(string value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed;
        }

        return DateTimeOffset.UtcNow;
    }

    private static object ToDbValue(double? value) => value.HasValue ? value.Value : DBNull.Value;

    private static object ToDbString(DateTimeOffset? value) =>
        value.HasValue
            ? value.Value.ToString(DateFormat, CultureInfo.InvariantCulture)
            : DBNull.Value;
}
