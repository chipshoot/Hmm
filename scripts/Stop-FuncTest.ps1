<#
.SYNOPSIS
    Stops the functional testing Docker containers.

.DESCRIPTION
    This script stops and optionally removes Docker containers used for functional testing.

.PARAMETER Full
    Stop the full stack (API + SQL Server + Seq). If not specified, stops infrastructure only.

.PARAMETER RemoveVolumes
    Also remove the data volumes (SQL Server data, Seq data).

.EXAMPLE
    .\scripts\Stop-FuncTest.ps1
    # Stops infrastructure containers only

.EXAMPLE
    .\scripts\Stop-FuncTest.ps1 -Full
    # Stops full stack containers

.EXAMPLE
    .\scripts\Stop-FuncTest.ps1 -Full -RemoveVolumes
    # Stops full stack and removes all data
#>

param(
    [switch]$Full = $false,
    [switch]$RemoveVolumes = $false
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

if ($Full) {
    Write-Host "Stopping Full Stack Containers..." -ForegroundColor Cyan
    $composeFile = Join-Path $ProjectRoot "docker\docker-compose.sqlserver.yml"
} else {
    Write-Host "Stopping Infrastructure Containers..." -ForegroundColor Cyan
    $composeFile = Join-Path $ProjectRoot "docker\docker-compose.infra.yml"
}

Write-Host "Project Root: $ProjectRoot" -ForegroundColor Gray

if (-not (Test-Path $composeFile)) {
    Write-Error "Docker compose file not found: $composeFile"
    exit 1
}

$args = @("-f", $composeFile, "down")

if ($RemoveVolumes) {
    $args += "-v"
    Write-Host "Warning: Data volumes will be removed!" -ForegroundColor Yellow
}

Write-Host "Running: docker-compose $($args -join ' ')" -ForegroundColor Gray
docker-compose @args

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Containers stopped successfully!" -ForegroundColor Green
    if ($RemoveVolumes) {
        Write-Host "Data volumes have been removed." -ForegroundColor Yellow
    }
} else {
    Write-Error "Failed to stop containers"
    exit 1
}
