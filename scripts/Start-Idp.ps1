<#
.SYNOPSIS
    Starts the Hmm Identity Provider for functional testing.

.DESCRIPTION
    This script starts the Hmm.Idp project which provides authentication
    for the Hmm.ServiceApi.

.PARAMETER Build
    Build the project before running.

.EXAMPLE
    .\scripts\Start-Idp.ps1

.EXAMPLE
    .\scripts\Start-Idp.ps1 -Build

.NOTES
    The IDP requires its own SQL Server database. By default, it uses port 14333
    to avoid conflict with the API database on port 1433.
#>

param(
    [switch]$Build = $false
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not $ProjectRoot) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

$idpProject = Join-Path $ProjectRoot "src\Hmm.Idp\Hmm.Idp.csproj"

if (-not (Test-Path $idpProject)) {
    Write-Error "IDP project not found: $idpProject"
    exit 1
}

Write-Host "Starting Hmm Identity Provider..." -ForegroundColor Cyan
Write-Host "Project: $idpProject" -ForegroundColor Gray
Write-Host ""

if ($Build) {
    Write-Host "Building project..." -ForegroundColor Yellow
    dotnet build $idpProject
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed"
        exit 1
    }
}

Write-Host "Starting IDP on https://localhost:5001..." -ForegroundColor Green
Write-Host ""
Write-Host "Available endpoints:" -ForegroundColor Yellow
Write-Host "  Discovery: https://localhost:5001/.well-known/openid-configuration" -ForegroundColor Gray
Write-Host "  Token:     https://localhost:5001/connect/token" -ForegroundColor Gray
Write-Host "  Authorize: https://localhost:5001/connect/authorize" -ForegroundColor Gray
Write-Host "  UserInfo:  https://localhost:5001/connect/userinfo" -ForegroundColor Gray
Write-Host "  Register:  https://localhost:5001/Account/Register" -ForegroundColor Gray
Write-Host "  Login:     https://localhost:5001/Account/Login" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

dotnet run --project $idpProject
