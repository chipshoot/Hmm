<#
.SYNOPSIS
    Docker Setup Script for Hmm Development Environment

.DESCRIPTION
    Sets up Docker containers for 3 development scenarios using compose file layering.

    Scenario 1: API runs locally, IDP (PostgreSQL embedded) + SQL Server (API) + Seq in Docker
    Scenario 2: Everything in Docker (API + IDP (PostgreSQL embedded) + SQL Server (API) + Seq)
    Scenario 3: API + IDP run locally, SQL Server (API) + Seq in Docker

.PARAMETER Scenario
    Which development scenario to start (1, 2, or 3)

.PARAMETER Down
    Stop and remove all containers

.PARAMETER Rebuild
    Rebuild Docker images from scratch (no cache)

.EXAMPLE
    .\hmm-setup.ps1 -Scenario 1
    Start Scenario 1: debug API locally with full backend in Docker

.EXAMPLE
    .\hmm-setup.ps1 -Scenario 2
    Start Scenario 2: everything runs in Docker

.EXAMPLE
    .\hmm-setup.ps1 -Scenario 3
    Start Scenario 3: debug both API and IDP locally, infra in Docker

.EXAMPLE
    .\hmm-setup.ps1 -Down
    Stop and remove all containers

.EXAMPLE
    .\hmm-setup.ps1 -Scenario 2 -Rebuild
    Rebuild images and start Scenario 2
#>

param(
    [ValidateSet(1, 2, 3)]
    [int]$Scenario,

    [switch]$Down,
    [switch]$Rebuild
)

# Navigate to the docker directory (where compose files live)
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Hmm Docker Development Environment" -ForegroundColor Cyan
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

# Define compose file combinations for each scenario
$BaseFiles = @("-f", "compose.base.yml")
$IdpFiles  = $BaseFiles + @("-f", "compose.idp.yml")
$AllFiles  = $IdpFiles + @("-f", "compose.api.yml")

# Handle Down parameter - stop all possible services
if ($Down) {
    Write-Host "Stopping and removing containers..." -ForegroundColor Yellow
    docker compose @AllFiles down
    Write-Host ""
    Write-Host "Environment stopped." -ForegroundColor Green
    exit 0
}

# Validate Scenario is provided when not using -Down
if (-not $Scenario) {
    Write-Host "ERROR: Please specify a scenario with -Scenario 1, 2, or 3" -ForegroundColor Red
    Write-Host ""
    Write-Host "Scenarios:" -ForegroundColor Cyan
    Write-Host "  1  API local, IDP (PostgreSQL) + SQL Server (API) + Seq in Docker"
    Write-Host "  2  Everything in Docker"
    Write-Host "  3  API + IDP local, SQL Server (API) + Seq in Docker"
    Write-Host ""
    Write-Host "Usage: .\hmm-setup.ps1 -Scenario <1|2|3> [-Rebuild]" -ForegroundColor Yellow
    Write-Host "       .\hmm-setup.ps1 -Down" -ForegroundColor Yellow
    exit 1
}

# Select compose files based on scenario
switch ($Scenario) {
    1 { $ComposeFiles = $IdpFiles }
    2 { $ComposeFiles = $AllFiles }
    3 { $ComposeFiles = $BaseFiles }
}

# Rebuild if requested
if ($Rebuild) {
    Write-Host "Rebuilding: stopping existing containers..." -ForegroundColor Yellow
    docker compose @ComposeFiles down
    Write-Host "Building images from scratch (no cache)..." -ForegroundColor Yellow
    docker compose @ComposeFiles build --no-cache
    Write-Host ""
}

# Start the environment
Write-Host "Starting Scenario $Scenario..." -ForegroundColor Yellow
Write-Host ""

switch ($Scenario) {
    1 { Write-Host "  Docker: IDP (PostgreSQL embedded) + SQL Server (API) + Seq" -ForegroundColor DarkGray }
    2 { Write-Host "  Docker: API + IDP (PostgreSQL embedded) + SQL Server (API) + Seq" -ForegroundColor DarkGray }
    3 { Write-Host "  Docker: SQL Server (API) + Seq" -ForegroundColor DarkGray }
}
Write-Host ""

docker compose @ComposeFiles up -d --build

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Failed to start containers." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Green
Write-Host "Scenario $Scenario Started Successfully!" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green
Write-Host ""

# Connection info
Write-Host "Services:" -ForegroundColor Cyan
Write-Host "  SQL Server (API): localhost,1433  (user: sa)" -ForegroundColor White
Write-Host "  Seq UI:      http://localhost:8081" -ForegroundColor White
Write-Host "  Seq Ingest:  http://localhost:5341" -ForegroundColor White

if ($Scenario -eq 1 -or $Scenario -eq 2) {
    Write-Host "  IDP:         http://localhost:5001" -ForegroundColor White
    Write-Host "  IDP DB:      PostgreSQL (embedded in hmm-idp container)" -ForegroundColor White
    Write-Host "  IDP DB CLI:  docker exec -it hmm-idp su postgres -c `"psql -h 127.0.0.1 -d HmmIdp`"" -ForegroundColor White
}
if ($Scenario -eq 2) {
    Write-Host "  API:         http://localhost:5010" -ForegroundColor White
    Write-Host "  Swagger:     http://localhost:5010/swagger" -ForegroundColor White
}
Write-Host ""

# Next steps for local services
if ($Scenario -eq 1) {
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  Run the API locally:" -ForegroundColor White
    Write-Host '  $env:AppSettings__IdpBaseUrl="http://localhost:5001"; dotnet run --project src/Hmm.ServiceApi' -ForegroundColor DarkGray
    Write-Host ""
}
elseif ($Scenario -eq 3) {
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Run the IDP locally:" -ForegroundColor White
    Write-Host '  dotnet run --project src/Hmm.Idp' -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  2. Run the API locally:" -ForegroundColor White
    Write-Host '  dotnet run --project src/Hmm.ServiceApi' -ForegroundColor DarkGray
    Write-Host ""
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Commands:" -ForegroundColor Cyan
Write-Host "  Stop:     .\hmm-setup.ps1 -Down" -ForegroundColor DarkGray
Write-Host "  Rebuild:  .\hmm-setup.ps1 -Scenario $Scenario -Rebuild" -ForegroundColor DarkGray
Write-Host "  Logs:     docker compose $($ComposeFiles -join ' ') logs -f" -ForegroundColor DarkGray
Write-Host "============================================================" -ForegroundColor Cyan
