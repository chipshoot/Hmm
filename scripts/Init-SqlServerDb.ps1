<#
.SYNOPSIS
    Initializes the SQL Server database with the schema.

.DESCRIPTION
    This script runs the init-db.sql script against the Docker SQL Server instance
    to create the database schema.

.EXAMPLE
    .\scripts\Init-SqlServerDb.ps1

.NOTES
    Requires SQL Server container to be running.
    Uses the init-db.sql script from docker/sqlserver/
#>

param(
    [string]$Server = "localhost,1433",
    [string]$User = "sa",
    [string]$Password = "Shcdlhgm1!"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$initScript = Join-Path $ProjectRoot "docker\sqlserver\init-db.sql"

if (-not (Test-Path $initScript)) {
    Write-Error "Init script not found: $initScript"
    exit 1
}

Write-Host "Initializing SQL Server Database..." -ForegroundColor Cyan
Write-Host "Server: $Server" -ForegroundColor Gray
Write-Host "Script: $initScript" -ForegroundColor Gray

# Check if sqlcmd is available
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue

if ($sqlcmd) {
    Write-Host "Using local sqlcmd..." -ForegroundColor Gray
    sqlcmd -S $Server -U $User -P $Password -i $initScript -C
} else {
    Write-Host "Local sqlcmd not found, using Docker container..." -ForegroundColor Gray

    # Copy script to container and execute
    $containerName = "hmm-sqlserver"

    # Check if container is running
    $container = docker ps --filter "name=$containerName" --format "{{.Names}}" 2>$null
    if (-not $container) {
        Write-Error "SQL Server container '$containerName' is not running. Start it first with: .\scripts\Start-FuncTestInfra.ps1"
        exit 1
    }

    # Copy and execute
    docker cp $initScript "${containerName}:/tmp/init-db.sql"
    docker exec $containerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U $User -P $Password -i /tmp/init-db.sql -C
}

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Database initialized successfully!" -ForegroundColor Green
} else {
    Write-Error "Failed to initialize database"
    exit 1
}
