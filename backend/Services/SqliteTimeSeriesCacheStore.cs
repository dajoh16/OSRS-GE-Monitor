using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using OSRSGeMonitor.Api.Models;

namespace OSRSGeMonitor.Api.Services;

public class SqliteTimeSeriesCacheStore
{
    private const string DateFormat = "O";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly string _connectionString;

    public SqliteTimeSeriesCacheStore(IHostEnvironment env)
    {
        var dbPath = Path.Combine(env.ContentRootPath, "price-history.db");
        _connectionString = $"Data Source={dbPath}";
        EnsureCreated();
    }

    public CacheEntry? Get(int itemId, string timestep)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT FetchedAt, PayloadJson
            FROM TimeSeriesCache
            WHERE ItemId = $itemId AND Timestep = $timestep
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$itemId", itemId);
        command.Parameters.AddWithValue("$timestep", timestep);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var fetchedAtRaw = reader.GetString(0);
        var payload = reader.GetString(1);
        if (!DateTimeOffset.TryParse(fetchedAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var fetchedAt))
        {
            return null;
        }

        var points = JsonSerializer.Deserialize<List<PricePoint>>(payload, JsonOptions);
        if (points is null)
        {
            return null;
        }

        return new CacheEntry(fetchedAt, points);
    }

    public void Upsert(int itemId, string timestep, DateTimeOffset fetchedAt, IReadOnlyList<PricePoint> points)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var payload = JsonSerializer.Serialize(points, JsonOptions);

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO TimeSeriesCache (ItemId, Timestep, FetchedAt, PayloadJson)
            VALUES ($itemId, $timestep, $fetchedAt, $payload)
            ON CONFLICT(ItemId, Timestep) DO UPDATE SET
                FetchedAt = excluded.FetchedAt,
                PayloadJson = excluded.PayloadJson;
            """;
        command.Parameters.AddWithValue("$itemId", itemId);
        command.Parameters.AddWithValue("$timestep", timestep);
        command.Parameters.AddWithValue("$fetchedAt", fetchedAt.ToString(DateFormat, CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$payload", payload);
        command.ExecuteNonQuery();
    }

    private void EnsureCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS TimeSeriesCache (
                ItemId INTEGER NOT NULL,
                Timestep TEXT NOT NULL,
                FetchedAt TEXT NOT NULL,
                PayloadJson TEXT NOT NULL,
                PRIMARY KEY (ItemId, Timestep)
            );
            """;
        command.ExecuteNonQuery();
    }

    public sealed record CacheEntry(DateTimeOffset FetchedAt, List<PricePoint> Points);
}
