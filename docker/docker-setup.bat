@echo off
REM ============================================================
REM Docker Setup Script for Hmm Test Environment (Windows)
REM ============================================================
REM This script sets up a self-contained MSSQL Docker environment
REM with pre-seeded data for functional testing.
REM ============================================================

setlocal enabledelayedexpansion

REM Configuration
set SA_PASSWORD=Password123!
set DB_PORT=14330
set SEQ_PORT=8083
set API_PORT=8080

echo ============================================================
echo Hmm Docker Test Environment Setup
echo ============================================================
echo.

REM Check if Docker is running
docker info > nul 2>&1
if errorlevel 1 (
    echo ERROR: Docker is not running. Please start Docker Desktop and try again.
    exit /b 1
)

echo Docker is running.
echo.

REM Navigate to the repository root (assuming script is in docker/)
cd /d "%~dp0.."

echo Working directory: %CD%
echo.

REM Check for required files
if not exist "docker\sqlserver\init-db.sql" (
    echo ERROR: init-db.sql not found at docker\sqlserver\init-db.sql
    exit /b 1
)
if not exist "docker\test-db\seed-data.sql" (
    echo ERROR: seed-data.sql not found at docker\test-db\seed-data.sql
    exit /b 1
)

echo Required files found.
echo.

REM Parse command line arguments
set START_API=0
set BUILD_FRESH=0

:parse_args
if "%~1"=="" goto :end_parse
if /i "%~1"=="--with-api" set START_API=1
if /i "%~1"=="--rebuild" set BUILD_FRESH=1
shift
goto :parse_args
:end_parse

REM Build and start the environment
echo Starting Docker environment...
echo.

if %BUILD_FRESH%==1 (
    echo Rebuilding images from scratch...
    docker-compose -f docker/docker-compose.test.yml down -v
    docker-compose -f docker/docker-compose.test.yml build --no-cache
)

if %START_API%==1 (
    echo Starting database, Seq, and API services...
    docker-compose -f docker/docker-compose.test.yml up --build -d
) else (
    echo Starting database and Seq services only...
    docker-compose -f docker/docker-compose.test.yml up --build -d db-test hmm-seq
)

echo.
echo ============================================================
echo Environment Started Successfully!
echo ============================================================
echo.
echo Database Connection:
echo   Server:   localhost,%DB_PORT%
echo   Database: hmm
echo   User:     sa
echo   Password: %SA_PASSWORD%
echo.
echo Connection String:
echo   Server=localhost,%DB_PORT%;Database=hmm;User Id=sa;Password=%SA_PASSWORD%;TrustServerCertificate=True
echo.
echo Seq Logging (UI): http://localhost:%SEQ_PORT%
echo.
if %START_API%==1 (
    echo API Endpoint: http://localhost:%API_PORT%
    echo Swagger UI:   http://localhost:%API_PORT%/swagger
    echo.
)
echo ============================================================
echo Commands:
echo   Stop:    docker-compose -f docker/docker-compose.test.yml down
echo   Logs:    docker-compose -f docker/docker-compose.test.yml logs -f
echo   Restart: docker-compose -f docker/docker-compose.test.yml restart
echo ============================================================

endlocal
