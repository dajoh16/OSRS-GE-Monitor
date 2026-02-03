using Microsoft.Data.Sqlite;
using OSRSGeMonitor.Api.Models;

namespace OSRSGeMonitor.Api.Services;

public sealed class SqliteConfigStore
{
    private const int SingleRowId = 1;
    private readonly string _connectionString;

    public SqliteConfigStore(IHostEnvironment env)
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
            CREATE TABLE IF NOT EXISTS AppConfig (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                StandardDeviationThreshold REAL NOT NULL,
                ProfitTargetPercent REAL NOT NULL,
                RecoveryStandardDeviationThreshold REAL NOT NULL,
                RollingWindowSize INTEGER NOT NULL,
                FetchIntervalSeconds INTEGER NOT NULL,
                UserAgent TEXT NOT NULL,
                DiscordNotificationsEnabled INTEGER NOT NULL,
                DiscordWebhookUrl TEXT,
                AlertGraceMinutes INTEGER NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);

        await EnsureColumnAsync(connection, "AlertGraceMinutes", "INTEGER NOT NULL DEFAULT 10", cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        SqliteConnection connection,
        string columnName,
        string columnDefinition,
        CancellationToken cancellationToken)
    {
        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA table_info(AppConfig);";
        await using var reader = await pragma.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(1);
            if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE AppConfig ADD COLUMN {columnName} {columnDefinition};";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<GlobalConfig?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                StandardDeviationThreshold,
                ProfitTargetPercent,
                RecoveryStandardDeviationThreshold,
                RollingWindowSize,
                FetchIntervalSeconds,
                UserAgent,
                DiscordNotificationsEnabled,
                DiscordWebhookUrl,
                AlertGraceMinutes
            FROM AppConfig
            WHERE Id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", SingleRowId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var config = new GlobalConfig
        {
            StandardDeviationThreshold = reader.GetDouble(0),
            ProfitTargetPercent = reader.GetDouble(1),
            RecoveryStandardDeviationThreshold = reader.GetDouble(2),
            RollingWindowSize = reader.GetInt32(3),
            FetchIntervalSeconds = reader.GetInt32(4),
            UserAgent = reader.GetString(5),
            DiscordNotificationsEnabled = reader.GetInt64(6) == 1,
            DiscordWebhookUrl = reader.IsDBNull(7) ? string.Empty : reader.GetString(7)
        };

        if (!reader.IsDBNull(8))
        {
            config.AlertGraceMinutes = reader.GetInt32(8);
        }

        return config;
    }

    public async Task SaveAsync(GlobalConfig config, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AppConfig (
                Id,
                StandardDeviationThreshold,
                ProfitTargetPercent,
                RecoveryStandardDeviationThreshold,
                RollingWindowSize,
                FetchIntervalSeconds,
                UserAgent,
                DiscordNotificationsEnabled,
                DiscordWebhookUrl,
                AlertGraceMinutes
            )
            VALUES (
                $id,
                $standardDeviationThreshold,
                $profitTargetPercent,
                $recoveryStandardDeviationThreshold,
                $rollingWindowSize,
                $fetchIntervalSeconds,
                $userAgent,
                $discordNotificationsEnabled,
                $discordWebhookUrl,
                $alertGraceMinutes
            )
            ON CONFLICT(Id) DO UPDATE SET
                StandardDeviationThreshold = excluded.StandardDeviationThreshold,
                ProfitTargetPercent = excluded.ProfitTargetPercent,
                RecoveryStandardDeviationThreshold = excluded.RecoveryStandardDeviationThreshold,
                RollingWindowSize = excluded.RollingWindowSize,
                FetchIntervalSeconds = excluded.FetchIntervalSeconds,
                UserAgent = excluded.UserAgent,
                DiscordNotificationsEnabled = excluded.DiscordNotificationsEnabled,
                DiscordWebhookUrl = excluded.DiscordWebhookUrl,
                AlertGraceMinutes = excluded.AlertGraceMinutes;
            """;

        command.Parameters.AddWithValue("$id", SingleRowId);
        command.Parameters.AddWithValue("$standardDeviationThreshold", config.StandardDeviationThreshold);
        command.Parameters.AddWithValue("$profitTargetPercent", config.ProfitTargetPercent);
        command.Parameters.AddWithValue("$recoveryStandardDeviationThreshold", config.RecoveryStandardDeviationThreshold);
        command.Parameters.AddWithValue("$rollingWindowSize", config.RollingWindowSize);
        command.Parameters.AddWithValue("$fetchIntervalSeconds", config.FetchIntervalSeconds);
        command.Parameters.AddWithValue("$userAgent", config.UserAgent);
        command.Parameters.AddWithValue("$discordNotificationsEnabled", config.DiscordNotificationsEnabled ? 1 : 0);
        command.Parameters.AddWithValue(
            "$discordWebhookUrl",
            string.IsNullOrWhiteSpace(config.DiscordWebhookUrl) ? DBNull.Value : config.DiscordWebhookUrl);
        command.Parameters.AddWithValue("$alertGraceMinutes", config.AlertGraceMinutes);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

}
