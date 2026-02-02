<#
.SYNOPSIS
    Starts the infrastructure containers (SQL Server + Seq) for functional testing.

.DESCRIPTION
    This script starts Docker containers for SQL Server and Seq logging.
    Use this when running the API locally (IIS Express or Kestrel) for functional testing.

.EXAMPLE
    .\scripts\Start-FuncTestInfra.ps1

.NOTES
    After starting, access:
    - SQL Server: localhost,1433 (sa / Shcdlhgm1!)
    - Seq UI: http://localhost:8081
    - Seq Ingestion: http://localhost:5341
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

Write-Host "Starting Infrastructure Containers (SQL Server + Seq)..." -ForegroundColor Cyan
Write-Host "Project Root: $ProjectRoot" -ForegroundColor Gray

$composeFile = Join-Path $ProjectRoot "docker\docker-compose.infra.yml"

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
    Write-Host "Infrastructure started successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Services available:" -ForegroundColor Yellow
    Write-Host "  SQL Server: localhost,1433" -ForegroundColor White
    Write-Host "    User: sa" -ForegroundColor Gray
    Write-Host "    Password: Shcdlhgm1!" -ForegroundColor Gray
    Write-Host "  Seq UI: http://localhost:8081" -ForegroundColor White
    Write-Host "  Seq Ingestion: http://localhost:5341" -ForegroundColor White
    Write-Host ""
    Write-Host "To run API with SQL Server:" -ForegroundColor Yellow
    Write-Host "  Visual Studio: Select 'IIS Express SqlServer' or 'Hmm.ServiceApi SqlServer' profile" -ForegroundColor Gray
    Write-Host "  CLI: dotnet run --project src/Hmm.ServiceApi --launch-profile 'Hmm.ServiceApi SqlServer'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To stop: .\scripts\Stop-FuncTest.ps1" -ForegroundColor Yellow
} else {
    Write-Error "Failed to start infrastructure containers"
    exit 1
}
