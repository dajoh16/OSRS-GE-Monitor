using System.Globalization;
using Microsoft.Data.Sqlite;
using OSRSGeMonitor.Api.Models;

namespace OSRSGeMonitor.Api.Services;

public class SqlitePriceHistoryStore
{
    private const string DateFormat = "O";
    private readonly string _connectionString;

    public SqlitePriceHistoryStore(IHostEnvironment env)
    {
        var dbPath = Path.Combine(env.ContentRootPath, "price-history.db");
        _connectionString = $"Data Source={dbPath}";
        EnsureCreated();
    }

    public void AddPricePoint(int itemId, PricePoint point)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO PricePoints (ItemId, Timestamp, Price)
            VALUES ($itemId, $timestamp, $price);
            """;
        command.Parameters.AddWithValue("$itemId", itemId);
        command.Parameters.AddWithValue("$timestamp", point.Timestamp.ToString(DateFormat, CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$price", point.Price);
        command.ExecuteNonQuery();
    }

    public List<PricePoint> GetRecentPricePoints(int itemId, int limit)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Timestamp, Price
            FROM PricePoints
            WHERE ItemId = $itemId
            ORDER BY Timestamp DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$itemId", itemId);
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var results = new List<PricePoint>();
        while (reader.Read())
        {
            var timestampRaw = reader.GetString(0);
            var price = reader.GetDouble(1);
            if (DateTimeOffset.TryParse(timestampRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp))
            {
                results.Add(new PricePoint(timestamp, price));
            }
        }

        return results;
    }

    private void EnsureCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS PricePoints (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ItemId INTEGER NOT NULL,
                Timestamp TEXT NOT NULL,
                Price REAL NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_PricePoints_ItemId ON PricePoints (ItemId);
            """;
        command.ExecuteNonQuery();
    }
}
