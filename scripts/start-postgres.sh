#!/usr/bin/env bash
set -euo pipefail

CONTAINER_NAME=${CONTAINER_NAME:-osrs-ge-monitor-db}
VOLUME_NAME=${VOLUME_NAME:-osrs-ge-monitor-data}
POSTGRES_USER=${POSTGRES_USER:-osrs}
POSTGRES_PASSWORD=${POSTGRES_PASSWORD:-osrs_password}
POSTGRES_DB=${POSTGRES_DB:-osrs_ge_monitor}
POSTGRES_PORT=${POSTGRES_PORT:-5432}

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SCHEMA_PATH="$ROOT_DIR/database/schema.sql"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required to run PostgreSQL." >&2
  exit 1
fi

if ! docker volume inspect "$VOLUME_NAME" >/dev/null 2>&1; then
  docker volume create "$VOLUME_NAME" >/dev/null
fi

if ! docker ps -a --format '{{.Names}}' | grep -qx "$CONTAINER_NAME"; then
  docker run -d \
    --name "$CONTAINER_NAME" \
    -e POSTGRES_USER="$POSTGRES_USER" \
    -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
    -e POSTGRES_DB="$POSTGRES_DB" \
    -p "$POSTGRES_PORT":5432 \
    -v "$VOLUME_NAME":/var/lib/postgresql/data \
    -v "$SCHEMA_PATH":/migrations/schema.sql:ro \
    postgres:16
else
  docker start "$CONTAINER_NAME" >/dev/null
fi

echo "Waiting for PostgreSQL to accept connections..."
for _ in {1..30}; do
  if docker exec "$CONTAINER_NAME" pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

if ! docker exec "$CONTAINER_NAME" pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" >/dev/null 2>&1; then
  echo "PostgreSQL did not become ready in time." >&2
  exit 1
fi

docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$CONTAINER_NAME" \
  psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f /migrations/schema.sql

echo "PostgreSQL is running at localhost:$POSTGRES_PORT with database $POSTGRES_DB."
