#!/bin/bash
# ============================================================
# Docker Setup Script for Hmm Test Environment (Linux/macOS)
# ============================================================
# This script sets up a self-contained MSSQL Docker environment
# with pre-seeded data for functional testing.
# ============================================================

set -e

# Configuration
SA_PASSWORD="Password123!"
DB_PORT=14330
SEQ_PORT=8083
API_PORT=8080

echo "============================================================"
echo "Hmm Docker Test Environment Setup"
echo "============================================================"
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "ERROR: Docker is not running. Please start Docker and try again."
    exit 1
fi

echo "Docker is running."
echo ""

# Navigate to the repository root (assuming script is in docker/)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/.."

echo "Working directory: $(pwd)"
echo ""

# Check for required files
if [ ! -f "docker/sqlserver/init-db.sql" ]; then
    echo "ERROR: init-db.sql not found at docker/sqlserver/init-db.sql"
    exit 1
fi
if [ ! -f "docker/test-db/seed-data.sql" ]; then
    echo "ERROR: seed-data.sql not found at docker/test-db/seed-data.sql"
    exit 1
fi

echo "Required files found."
echo ""

# Parse command line arguments
START_API=0
BUILD_FRESH=0

while [[ $# -gt 0 ]]; do
    case $1 in
        --with-api)
            START_API=1
            shift
            ;;
        --rebuild)
            BUILD_FRESH=1
            shift
            ;;
        *)
            shift
            ;;
    esac
done

# Build and start the environment
echo "Starting Docker environment..."
echo ""

if [ $BUILD_FRESH -eq 1 ]; then
    echo "Rebuilding images from scratch..."
    docker-compose -f docker/docker-compose.test.yml down -v
    docker-compose -f docker/docker-compose.test.yml build --no-cache
fi

if [ $START_API -eq 1 ]; then
    echo "Starting database, Seq, and API services..."
    docker-compose -f docker/docker-compose.test.yml up --build -d
else
    echo "Starting database and Seq services only..."
    docker-compose -f docker/docker-compose.test.yml up --build -d db-test hmm-seq
fi

echo ""
echo "============================================================"
echo "Environment Started Successfully!"
echo "============================================================"
echo ""
echo "Database Connection:"
echo "  Server:   localhost,$DB_PORT"
echo "  Database: hmm"
echo "  User:     sa"
echo "  Password: $SA_PASSWORD"
echo ""
echo "Connection String:"
echo "  Server=localhost,$DB_PORT;Database=hmm;User Id=sa;Password=$SA_PASSWORD;TrustServerCertificate=True"
echo ""
echo "Seq Logging (UI): http://localhost:$SEQ_PORT"
echo ""
if [ $START_API -eq 1 ]; then
    echo "API Endpoint: http://localhost:$API_PORT"
    echo "Swagger UI:   http://localhost:$API_PORT/swagger"
    echo ""
fi
echo "============================================================"
echo "Commands:"
echo "  Stop:    docker-compose -f docker/docker-compose.test.yml down"
echo "  Logs:    docker-compose -f docker/docker-compose.test.yml logs -f"
echo "  Restart: docker-compose -f docker/docker-compose.test.yml restart"
echo "============================================================"
