# OSRS GE Monitor Database

This repository includes a PostgreSQL/TimescaleDB schema and helper scripts to run PostgreSQL in Docker with persisted storage.

## Start PostgreSQL with Docker

The scripts below create a Docker volume for persistence, start (or reuse) a PostgreSQL container, and run the schema migration in `database/schema.sql`.

### Bash (macOS/Linux)

```bash
./scripts/start-postgres.sh
```

You can override defaults with environment variables:

```bash
POSTGRES_USER=osrs \
POSTGRES_PASSWORD=osrs_password \
POSTGRES_DB=osrs_ge_monitor \
POSTGRES_PORT=5432 \
CONTAINER_NAME=osrs-ge-monitor-db \
VOLUME_NAME=osrs-ge-monitor-data \
./scripts/start-postgres.sh
```

### PowerShell (Windows)

```powershell
./scripts/start-postgres.ps1
```

You can override defaults with parameters:

```powershell
./scripts/start-postgres.ps1 `
  -PostgresUser osrs `
  -PostgresPassword osrs_password `
  -PostgresDb osrs_ge_monitor `
  -PostgresPort 5432 `
  -ContainerName osrs-ge-monitor-db `
  -VolumeName osrs-ge-monitor-data
```

## Migration

Both scripts run `database/schema.sql` inside the container after PostgreSQL is ready. Re-running the scripts is safe because the schema uses `if not exists` guards.

## Backend configuration

The backend can swap between the in-memory repository (default) and PostgreSQL by setting configuration values.

```json
{
  "TimeSeriesRepository": {
    "Provider": "Postgres",
    "ConnectionString": "Host=localhost;Port=5432;Database=osrs_ge_monitor;Username=osrs;Password=osrs_password"
  }
}
```

Set `"Provider"` to `"InMemory"` to use the in-memory implementation.
