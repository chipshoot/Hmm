<#
.SYNOPSIS
    Docker Setup Script for Hmm Test Environment

.DESCRIPTION
    This script sets up a self-contained MSSQL Docker environment
    with pre-seeded data for functional testing.

.PARAMETER WithApi
    Start the API service along with the database

.PARAMETER Rebuild
    Rebuild Docker images from scratch (no cache)

.PARAMETER Down
    Stop and remove all containers

.EXAMPLE
    .\docker-setup.ps1
    Starts only the database and Seq services

.EXAMPLE
    .\docker-setup.ps1 -WithApi
    Starts the database, Seq, and API services

.EXAMPLE
    .\docker-setup.ps1 -Rebuild
    Rebuilds images and starts services

.EXAMPLE
    .\docker-setup.ps1 -Down
    Stops and removes all containers
#>

param(
    [switch]$WithApi,
    [switch]$Rebuild,
    [switch]$Down
)

# Configuration
$SA_PASSWORD = "Password123!"
$DB_PORT = 14330
$SEQ_PORT = 8083
$API_PORT = 8080

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Hmm Docker Test Environment Setup" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is running
try {
    docker info 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker not running"
    }
}
catch {
    Write-Host "ERROR: Docker is not running. Please start Docker Desktop and try again." -ForegroundColor Red
    exit 1
}

Write-Host "Docker is running." -ForegroundColor Green
Write-Host ""

# Navigate to the repository root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Get-Item "$ScriptDir\..").FullName
Set-Location $RepoRoot

Write-Host "Working directory: $RepoRoot"
Write-Host ""

# Check for required files
if (-not (Test-Path "docker\sqlserver\init-db.sql")) {
    Write-Host "ERROR: init-db.sql not found at docker\sqlserver\init-db.sql" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path "docker\test-db\seed-data.sql")) {
    Write-Host "ERROR: seed-data.sql not found at docker\test-db\seed-data.sql" -ForegroundColor Red
    exit 1
}

Write-Host "Required files found." -ForegroundColor Green
Write-Host ""

# Handle Down parameter
if ($Down) {
    Write-Host "Stopping and removing containers..." -ForegroundColor Yellow
    docker-compose -f docker/docker-compose.test.yml down -v
    Write-Host "Environment stopped." -ForegroundColor Green
    exit 0
}

# Build and start the environment
Write-Host "Starting Docker environment..." -ForegroundColor Yellow
Write-Host ""

if ($Rebuild) {
    Write-Host "Rebuilding images from scratch..." -ForegroundColor Yellow
    docker-compose -f docker/docker-compose.test.yml down -v
    docker-compose -f docker/docker-compose.test.yml build --no-cache
}

if ($WithApi) {
    Write-Host "Starting database, Seq, and API services..." -ForegroundColor Yellow
    docker-compose -f docker/docker-compose.test.yml up --build -d
}
else {
    Write-Host "Starting database and Seq services only..." -ForegroundColor Yellow
    docker-compose -f docker/docker-compose.test.yml up --build -d db-test hmm-seq
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Environment Started Successfully!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Database Connection:" -ForegroundColor Cyan
Write-Host "  Server:   localhost,$DB_PORT"
Write-Host "  Database: hmm"
Write-Host "  User:     sa"
Write-Host "  Password: $SA_PASSWORD"
Write-Host ""
Write-Host "Connection String:" -ForegroundColor Cyan
Write-Host "  Server=localhost,$DB_PORT;Database=hmm;User Id=sa;Password=$SA_PASSWORD;TrustServerCertificate=True"
Write-Host ""
Write-Host "Seq Logging (UI): " -ForegroundColor Cyan -NoNewline
Write-Host "http://localhost:$SEQ_PORT"
Write-Host ""

if ($WithApi) {
    Write-Host "API Endpoint: " -ForegroundColor Cyan -NoNewline
    Write-Host "http://localhost:$API_PORT"
    Write-Host "Swagger UI:   " -ForegroundColor Cyan -NoNewline
    Write-Host "http://localhost:$API_PORT/swagger"
    Write-Host ""
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Commands:" -ForegroundColor Cyan
Write-Host "  Stop:    " -NoNewline
Write-Host "docker-compose -f docker/docker-compose.test.yml down" -ForegroundColor DarkGray
Write-Host "  Logs:    " -NoNewline
Write-Host "docker-compose -f docker/docker-compose.test.yml logs -f" -ForegroundColor DarkGray
Write-Host "  Restart: " -NoNewline
Write-Host "docker-compose -f docker/docker-compose.test.yml restart" -ForegroundColor DarkGray
Write-Host "============================================================" -ForegroundColor Cyan
