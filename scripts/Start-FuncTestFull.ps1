<#
.SYNOPSIS
    Starts the full stack containers (API + SQL Server + Seq) for functional testing.

.DESCRIPTION
    This script starts Docker containers for the API, SQL Server, and Seq logging.
    Use this for testing the API running in a Docker container.

.EXAMPLE
    .\scripts\Start-FuncTestFull.ps1

.EXAMPLE
    .\scripts\Start-FuncTestFull.ps1 -Build

.NOTES
    After starting, access:
    - API: https://localhost:5001 (or http://localhost:5000)
    - Swagger: https://localhost:5001/swagger
    - SQL Server: localhost,1433 (sa / Shcdlhgm1!)
    - Seq UI: http://localhost:8081
#>

param(
    [switch]$Detached = $true,
    [switch]$Build = $false
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

Write-Host "Starting Full Stack Containers (API + SQL Server + Seq)..." -ForegroundColor Cyan
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Gray

$composeFile = Join-Path $ProjectRoot "docker\docker-compose.sqlserver.yml"

if (-not (Test-Path $composeFile)) {
    Write-Error "Docker compose file not found: $composeFile"
    exit 1
}

$args = @("-f", $composeFile, "up")

if ($Detached) {
    $args += "-d"
}

if ($Build) {
    $args += "--build"
}

Write-Host "Running: docker-compose $($args -join ' ')" -ForegroundColor Gray
docker-compose @args

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Full stack started successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Services available:" -ForegroundColor Yellow
    Write-Host "  API (HTTPS): https://localhost:5001" -ForegroundColor White
    Write-Host "  API (HTTP): http://localhost:5000" -ForegroundColor White
    Write-Host "  Swagger: https://localhost:5001/swagger" -ForegroundColor White
    Write-Host "  SQL Server: localhost,1433" -ForegroundColor White
    Write-Host "    User: sa" -ForegroundColor Gray
    Write-Host "    Password: Shcdlhgm1!" -ForegroundColor Gray
    Write-Host "  Seq UI: http://localhost:8081" -ForegroundColor White
    Write-Host "  Seq Ingestion: http://localhost:5341" -ForegroundColor White
    Write-Host ""
    Write-Host "To view logs: docker-compose -f docker/docker-compose.sqlserver.yml logs -f hmm-api" -ForegroundColor Yellow
    Write-Host "To stop: .\scripts\Stop-FuncTest.ps1 -Full" -ForegroundColor Yellow
} else {
    Write-Error "Failed to start full stack containers"
    exit 1
}
