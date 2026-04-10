<#
.SYNOPSIS
    Test Environment Script for Hmm Backend (Scenario 2)

.DESCRIPTION
    Starts the full Hmm backend in Docker (Scenario 2: everything in Docker),
    waits for all services to be healthy, then runs smoke tests to verify
    the IDP and API are functional. Designed for use before testing with
    the Hmm_Console Flutter client.

    Services started:
      - Seq (logging UI)              localhost:8081
      - Hmm IDP (IdentityServer)      localhost:5001  (PostgreSQL inside container)
      - Hmm API (REST service)        localhost:5010  (SQLite on host)

    Seeded test users:
      - admin@hmm.local      / Admin@12345678#    (Administrator)
      - testuser@hmm.local   / TestPassword123#   (User)
      - alice                / Alice@12345678#     (User)
      - bob                  / Bob@123456789#      (User)

.PARAMETER Up
    Start the test environment (default action)

.PARAMETER Down
    Stop and remove all containers

.PARAMETER Rebuild
    Force rebuild Docker images from scratch (no cache)

.PARAMETER SkipTests
    Skip the smoke tests after starting services

.PARAMETER ResetDb
    Delete databases and restart fresh (API SQLite files + IDP PostgreSQL volume)

.EXAMPLE
    .\test-env.ps1
    Start test environment, wait for health, run smoke tests

.EXAMPLE
    .\test-env.ps1 -Rebuild
    Rebuild images and start test environment

.EXAMPLE
    .\test-env.ps1 -Down
    Stop and tear down the test environment

.EXAMPLE
    .\test-env.ps1 -ResetDb
    Delete databases and restart fresh
#>

param(
    [switch]$Up,
    [switch]$Down,
    [switch]$Rebuild,
    [switch]$SkipTests,
    [switch]$ResetDb
)

# ── Configuration ──────────────────────────────────────────────────────
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

$ComposeFiles = @("-f", "compose.base-sqlite.yml", "-f", "compose.idp.yml", "-f", "compose.api-sqlite.yml")

$IdpBaseUrl   = "http://localhost:5001"
$ApiBaseUrl   = "http://localhost:5010"
$SeqUrl       = "http://localhost:8081"

# IDP client credentials (must match HostingExtensions.cs seed data)
$ClientId     = "hmm.functest"
$ClientSecret = "FuncTestSecret123#"

# Test user credentials (from SeedDataService)
$TestUser     = "testuser@hmm.local"
$TestPassword = "TestPassword123#"

$MaxWaitSecs  = 180   # Max seconds to wait for services

# Host-side data directory for API SQLite database (easy to inspect with DBeaver)
$DataDir = Join-Path $ScriptDir "data"

# ── Helpers ────────────────────────────────────────────────────────────
function Write-Banner($text) {
    Write-Host ""
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host "  $text" -ForegroundColor Cyan
    Write-Host ("=" * 60) -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step($text) {
    Write-Host ">> $text" -ForegroundColor Yellow
}

function Write-Ok($text) {
    Write-Host "   [OK] $text" -ForegroundColor Green
}

function Write-Fail($text) {
    Write-Host "   [FAIL] $text" -ForegroundColor Red
}

function Write-Info($text) {
    Write-Host "   $text" -ForegroundColor DarkGray
}

function Test-Url($url, $description) {
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Ok "$description ($url)"
            return $true
        }
        Write-Fail "$description - Status: $($response.StatusCode)"
        return $false
    }
    catch {
        Write-Fail "$description - $($_.Exception.Message)"
        return $false
    }
}

# ── Check Docker ───────────────────────────────────────────────────────
Write-Banner "Hmm Test Environment (Scenario 2)"

$dockerCheck = & docker version --format "{{.Server.Version}}" 2>$null
if (-not $dockerCheck) {
    Write-Fail "Docker is not running. Please start Docker Desktop and try again."
    exit 1
}
Write-Ok "Docker is running (Engine $dockerCheck)"

# ── Handle -Down ───────────────────────────────────────────────────────
if ($Down) {
    Write-Step "Stopping test environment..."
    $overrideFile = Join-Path $ScriptDir ".compose.host-data.yml"
    if (Test-Path $overrideFile) {
        docker compose @ComposeFiles -f $overrideFile down --volumes --remove-orphans
    } else {
        docker compose @ComposeFiles down --volumes --remove-orphans
    }
    Write-Ok "Test environment stopped and volumes removed"
    Write-Host ""
    Write-Info "Host data directory preserved at: $DataDir"
    Write-Info "To remove API database files: Remove-Item -Recurse $DataDir"
    Write-Info "IDP PostgreSQL data was in Docker volume (removed with --volumes)"
    exit 0
}

# ── Handle -ResetDb ───────────────────────────────────────────────────
if ($ResetDb) {
    Write-Step "Resetting databases..."
    # Stop containers and remove volumes (includes IDP PostgreSQL volume)
    docker compose @ComposeFiles down --volumes --remove-orphans 2>$null

    # Remove API SQLite files from host
    if (Test-Path $DataDir) {
        $dbFiles = @("hmm.db", "hmm.db-wal", "hmm.db-shm")
        foreach ($f in $dbFiles) {
            $path = Join-Path $DataDir $f
            if (Test-Path $path) { Remove-Item $path -Force }
        }
        Write-Ok "Deleted API SQLite database files from $DataDir"
    }
    else {
        Write-Info "No data directory found at $DataDir - nothing to delete"
    }

    Write-Ok "IDP PostgreSQL volume removed (via --volumes flag above)"
}

# ── Prepare host data directory ───────────────────────────────────────
Write-Step "Preparing host data directories..."
New-Item -ItemType Directory -Force -Path $DataDir | Out-Null
Write-Ok "Data directory: $DataDir"

# ── Export bind mount paths for compose override ──────────────────────
$env:HMM_DATA_DIR = $DataDir

# Write a temporary compose override to use host bind mounts for API SQLite database
# IDP uses PostgreSQL inside its container (data persisted via idp-postgres-data volume)
$overrideFile = Join-Path $ScriptDir ".compose.host-data.yml"
@"
# Auto-generated by test-env.ps1 - mounts API SQLite database on the host
services:
  hmm-api:
    volumes:
      - `${HMM_DATA_DIR}:/app/data
"@ | Set-Content -Path $overrideFile -Encoding UTF8

$ComposeFiles += @("-f", $overrideFile)
Write-Info "Using host bind mounts for API (override: .compose.host-data.yml)"

# ── Start Services ─────────────────────────────────────────────────────
if ($Rebuild) {
    Write-Step "Tearing down existing containers..."
    docker compose @ComposeFiles down --volumes --remove-orphans
    Write-Step "Rebuilding images from scratch..."
    docker compose @ComposeFiles build --no-cache
}

Write-Step "Starting Scenario 2 (all services in Docker)..."
Write-Info "Compose: compose.base-sqlite.yml + compose.idp.yml + compose.api-sqlite.yml"
Write-Info "IDP: PostgreSQL (embedded in container)  |  API: SQLite (host: $DataDir)"
Write-Host ""

docker compose @ComposeFiles up -d --build

if ($LASTEXITCODE -ne 0) {
    Write-Fail "Failed to start containers"
    exit 1
}
Write-Ok "Docker containers started"

# ── Wait for IDP ───────────────────────────────────────────────────────
Write-Step "Waiting for IDP to be ready (up to ${MaxWaitSecs}s)..."

$elapsed = 0
$idpReady = $false
while ($elapsed -lt $MaxWaitSecs) {
    try {
        $r = Invoke-WebRequest -Uri "$IdpBaseUrl/.well-known/openid-configuration" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
        if ($r.StatusCode -eq 200) {
            $idpReady = $true
            break
        }
    }
    catch { }
    Start-Sleep -Seconds 3
    $elapsed += 3
    Write-Host "." -NoNewline -ForegroundColor DarkGray
}
Write-Host ""

if (-not $idpReady) {
    Write-Fail "IDP did not become ready within ${MaxWaitSecs}s"
    Write-Info "Check logs: docker compose $($ComposeFiles -join ' ') logs hmm-idp"
    exit 1
}
Write-Ok "IDP is ready (${elapsed}s)"

# ── Wait for API ───────────────────────────────────────────────────────
Write-Step "Waiting for API to be ready (up to ${MaxWaitSecs}s)..."

$elapsed = 0
$apiReady = $false
while ($elapsed -lt $MaxWaitSecs) {
    try {
        $r = Invoke-WebRequest -Uri "$ApiBaseUrl/swagger/index.html" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
        if ($r.StatusCode -eq 200) {
            $apiReady = $true
            break
        }
    }
    catch { }
    Start-Sleep -Seconds 3
    $elapsed += 3
    Write-Host "." -NoNewline -ForegroundColor DarkGray
}
Write-Host ""

if (-not $apiReady) {
    Write-Fail "API did not become ready within ${MaxWaitSecs}s"
    Write-Info "Check logs: docker compose $($ComposeFiles -join ' ') logs hmm-api"
    exit 1
}
Write-Ok "API is ready (${elapsed}s)"

# ── Smoke Tests ────────────────────────────────────────────────────────
if ($SkipTests) {
    Write-Step "Skipping smoke tests (-SkipTests)"
}
else {
    Write-Banner "Smoke Tests"

    $passed = 0
    $failed = 0

    # Test 1: IDP Discovery endpoint
    Write-Step "Test 1: IDP Discovery endpoint"
    if (Test-Url "$IdpBaseUrl/.well-known/openid-configuration" "OpenID Configuration") {
        $passed++
    } else { $failed++ }

    # Test 2: IDP Login page loads
    Write-Step "Test 2: IDP Login page"
    if (Test-Url "$IdpBaseUrl/Account/Login" "Login page") {
        $passed++
    } else { $failed++ }

    # Test 3: Swagger UI
    Write-Step "Test 3: API Swagger UI"
    if (Test-Url "$ApiBaseUrl/swagger/index.html" "Swagger UI") {
        $passed++
    } else { $failed++ }

    # Test 4: Seq logging UI
    Write-Step "Test 4: Seq Logging UI"
    if (Test-Url $SeqUrl "Seq UI") {
        $passed++
    } else { $failed++ }

    # Test 5: Token exchange (ROPC grant with test user)
    Write-Step "Test 5: IDP Token exchange (ROPC grant)"
    try {
        $tokenBody = @{
            grant_type    = "password"
            client_id     = $ClientId
            client_secret = $ClientSecret
            username      = $TestUser
            password      = $TestPassword
            scope         = "openid profile hmmapi offline_access"
        }
        $tokenResponse = Invoke-RestMethod -Uri "$IdpBaseUrl/connect/token" `
            -Method Post `
            -Body $tokenBody `
            -ContentType "application/x-www-form-urlencoded" `
            -TimeoutSec 10 `
            -ErrorAction Stop

        if ($tokenResponse.access_token) {
            Write-Ok "Token acquired for $TestUser"
            Write-Info "Token type: $($tokenResponse.token_type), expires in: $($tokenResponse.expires_in)s"
            $accessToken = $tokenResponse.access_token
            $passed++
        }
        else {
            Write-Fail "Token response missing access_token"
            $failed++
        }
    }
    catch {
        Write-Fail "Token exchange failed - $($_.Exception.Message)"
        $failed++
    }

    # Test 6: Authenticated API call (GET /api/v1/authors)
    Write-Step "Test 6: Authenticated API call (GET /api/v1/authors)"
    if ($accessToken) {
        try {
            $headers = @{ Authorization = "Bearer $accessToken" }
            $apiResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/api/v1/authors" `
                -Method Get `
                -Headers $headers `
                -TimeoutSec 10 `
                -ErrorAction Stop
            Write-Ok "API returned authors successfully"
            $passed++
        }
        catch {
            Write-Fail "API call failed - $($_.Exception.Message)"
            $failed++
        }
    }
    else {
        Write-Fail "Skipped (no token from Test 5)"
        $failed++
    }

    # Test 7: Verify external login providers are registered
    Write-Step "Test 7: External login providers visible on login page"
    try {
        $loginPage = Invoke-WebRequest -Uri "$IdpBaseUrl/Account/Login" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        $html = $loginPage.Content
        $googleFound = $html -match "Google"
        $githubFound = $html -match "GitHub"

        if ($googleFound -and $githubFound) {
            Write-Ok "Both Google and GitHub login buttons found"
            $passed++
        }
        elseif ($googleFound -or $githubFound) {
            $found = if ($googleFound) { "Google" } else { "GitHub" }
            $missing = if ($googleFound) { "GitHub" } else { "Google" }
            Write-Ok "$found button found, $missing button missing (may need valid client credentials)"
            $passed++
        }
        else {
            Write-Fail "Neither Google nor GitHub buttons found on login page"
            Write-Info "External providers may not register without valid ClientId/ClientSecret"
            $failed++
        }
    }
    catch {
        Write-Fail "Could not load login page - $($_.Exception.Message)"
        $failed++
    }

    # Results
    Write-Host ""
    Write-Host ("-" * 60) -ForegroundColor Cyan
    if ($failed -eq 0) {
        Write-Host "  All $passed tests passed!" -ForegroundColor Green
    }
    else {
        Write-Host "  Results: $passed passed, $failed failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
    }
    Write-Host ("-" * 60) -ForegroundColor Cyan
}

# ── Summary ────────────────────────────────────────────────────────────
Write-Banner "Test Environment Ready"

Write-Host "  Services:" -ForegroundColor Cyan
Write-Host "    IDP:           $IdpBaseUrl" -ForegroundColor White
Write-Host "    IDP Login:     $IdpBaseUrl/Account/Login" -ForegroundColor White
Write-Host "    API:           $ApiBaseUrl" -ForegroundColor White
Write-Host "    Swagger:       $ApiBaseUrl/swagger" -ForegroundColor White
Write-Host "    Seq Logs:      $SeqUrl" -ForegroundColor White
Write-Host ""
Write-Host "  Databases:" -ForegroundColor Cyan
Write-Host "    IDP:           PostgreSQL (embedded in hmm-idp container, volume: idp-postgres-data)" -ForegroundColor White
Write-Host "    API:           SQLite (host: $DataDir\hmm.db)" -ForegroundColor White
Write-Host ""
Write-Host "  API SQLite file on host (open with DBeaver):" -ForegroundColor Cyan
Write-Host "    API DB:        $DataDir\hmm.db" -ForegroundColor White
Write-Host ""
Write-Host "  IDP PostgreSQL (connect with DBeaver/pgAdmin via docker exec):" -ForegroundColor Cyan
Write-Host "    Container:     hmm-idp" -ForegroundColor White
Write-Host "    Database:      HmmIdp" -ForegroundColor White
Write-Host "    User:          postgres" -ForegroundColor White
Write-Host "    Connect:       docker exec -it hmm-idp su postgres -c `"psql -h 127.0.0.1 -d HmmIdp`"" -ForegroundColor White
Write-Host ""
Write-Host "  Test Users:" -ForegroundColor Cyan
Write-Host "    admin@hmm.local      / Admin@12345678#   (Administrator)" -ForegroundColor White
Write-Host "    testuser@hmm.local   / TestPassword123#  (User)" -ForegroundColor White
Write-Host "    alice                / Alice@12345678#    (User)" -ForegroundColor White
Write-Host "    bob                  / Bob@123456789#     (User)" -ForegroundColor White
Write-Host ""
Write-Host "  Hmm_Console (Flutter) connection:" -ForegroundColor Cyan
Write-Host "    IDP Authority:   http://localhost:5001" -ForegroundColor White
Write-Host "    API Base URL:    http://localhost:5010/api/v1" -ForegroundColor White
Write-Host "    Client ID:       hmm.functest" -ForegroundColor White
Write-Host "    Client Secret:   FuncTestSecret123#" -ForegroundColor White
Write-Host ""
Write-Host "  Commands:" -ForegroundColor Cyan
Write-Host "    Stop:      .\test-env.ps1 -Down" -ForegroundColor DarkGray
Write-Host "    Rebuild:   .\test-env.ps1 -Rebuild" -ForegroundColor DarkGray
Write-Host "    Reset DB:  .\test-env.ps1 -ResetDb" -ForegroundColor DarkGray
Write-Host "    Logs:      docker compose $($ComposeFiles -join ' ') logs -f" -ForegroundColor DarkGray
Write-Host "    IDP logs:  docker compose $($ComposeFiles -join ' ') logs -f hmm-idp" -ForegroundColor DarkGray
Write-Host "    API logs:  docker compose $($ComposeFiles -join ' ') logs -f hmm-api" -ForegroundColor DarkGray
Write-Host "    IDP DB:    docker exec -it hmm-idp su postgres -c `"psql -h 127.0.0.1 -d HmmIdp`"" -ForegroundColor DarkGray
Write-Host ""
