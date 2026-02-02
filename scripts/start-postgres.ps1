Param(
    [string]$ContainerName = "osrs-ge-monitor-db",
    [string]$VolumeName = "osrs-ge-monitor-data",
    [string]$PostgresUser = "osrs",
    [string]$PostgresPassword = "osrs_password",
    [string]$PostgresDb = "osrs_ge_monitor",
    [int]$PostgresPort = 5432
)

$rootDir = Resolve-Path (Join-Path $PSScriptRoot "..")
$schemaPath = Join-Path $rootDir "database" "schema.sql"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker is required to run PostgreSQL."
    exit 1
}

if (-not (docker volume inspect $VolumeName 2>$null)) {
    docker volume create $VolumeName | Out-Null
}

$existing = docker ps -a --format "{{.Names}}" | Where-Object { $_ -eq $ContainerName }
if (-not $existing) {
    docker run -d `
        --name $ContainerName `
        -e POSTGRES_USER=$PostgresUser `
        -e POSTGRES_PASSWORD=$PostgresPassword `
        -e POSTGRES_DB=$PostgresDb `
        -p ${PostgresPort}:5432 `
        -v ${VolumeName}:/var/lib/postgresql/data `
        -v ${schemaPath}:/migrations/schema.sql:ro `
        postgres:16 | Out-Null
} else {
    docker start $ContainerName | Out-Null
}

Write-Host "Waiting for PostgreSQL to accept connections..."
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    docker exec $ContainerName pg_isready -U $PostgresUser -d $PostgresDb | Out-Null
    if ($LASTEXITCODE -eq 0) {
        $ready = $true
        break
    }
    Start-Sleep -Seconds 1
}

if (-not $ready) {
    Write-Error "PostgreSQL did not become ready in time."
    exit 1
}

docker exec -e PGPASSWORD=$PostgresPassword $ContainerName \
    psql -U $PostgresUser -d $PostgresDb -f /migrations/schema.sql

Write-Host "PostgreSQL is running at localhost:$PostgresPort with database $PostgresDb."
