using System.Globalization;
using Microsoft.Data.Sqlite;
using OSRSGeMonitor.Api.Models;

namespace OSRSGeMonitor.Api.Services;

public class SqliteWatchlistStore
{
    private const string DateFormat = "O";
    private readonly string _connectionString;

    public SqliteWatchlistStore(IHostEnvironment env)
    {
        var dbPath = Path.Combine(env.ContentRootPath, "price-history.db");
        _connectionString = $"Data Source={dbPath}";
        EnsureCreated();
    }

    public IReadOnlyCollection<MonitoredItem> GetItems()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, AddedAt
            FROM Watchlist
            ORDER BY Id ASC;
            """;

        using var reader = command.ExecuteReader();
        var results = new List<MonitoredItem>();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var addedAtRaw = reader.GetString(2);
            if (!DateTimeOffset.TryParse(addedAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var addedAt))
            {
                addedAt = DateTimeOffset.UtcNow;
            }

            results.Add(new MonitoredItem
            {
                Id = id,
                Name = name,
                AddedAt = addedAt
            });
        }

        return results;
    }

    public void UpsertItem(MonitoredItem item)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Watchlist (Id, Name, AddedAt)
            VALUES ($id, $name, $addedAt)
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name,
                AddedAt = excluded.AddedAt;
            """;
        command.Parameters.AddWithValue("$id", item.Id);
        command.Parameters.AddWithValue("$name", item.Name);
        command.Parameters.AddWithValue("$addedAt", item.AddedAt.ToString(DateFormat, CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    public bool RemoveItem(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Watchlist WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() > 0;
    }

    private void EnsureCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Watchlist (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                AddedAt TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}
