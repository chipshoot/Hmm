#!/bin/bash
# ============================================================
# Docker Setup Script for Hmm Development Environment
# ============================================================
#
# Scenarios:
#   1  API local, IDP + SQL Server (API) + SQL Server (IDP) + Seq in Docker
#   2  Everything in Docker (API + IDP + SQL Server x2 + Seq)
#   3  API + IDP local, SQL Server (API) + SQL Server (IDP) + Seq in Docker
#
# Usage:
#   ./hmm-setup.sh --scenario 1|2|3 [--rebuild]
#   ./hmm-setup.sh --down
# ============================================================

set -e

# Navigate to the docker directory (where compose files live)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Parse arguments
SCENARIO=""
DOWN=0
REBUILD=0

while [[ $# -gt 0 ]]; do
    case $1 in
        --scenario|-s)
            SCENARIO="$2"
            shift 2
            ;;
        --down|-d)
            DOWN=1
            shift
            ;;
        --rebuild|-r)
            REBUILD=1
            shift
            ;;
        --help|-h)
            echo "Usage: ./hmm-setup.sh --scenario <1|2|3> [--rebuild]"
            echo "       ./hmm-setup.sh --down"
            echo ""
            echo "Scenarios:"
            echo "  1  API local, IDP + SQL Servers + Seq in Docker"
            echo "  2  Everything in Docker"
            echo "  3  API + IDP local, SQL Servers + Seq in Docker"
            echo ""
            echo "Options:"
            echo "  --scenario, -s  Scenario number (1, 2, or 3)"
            echo "  --down, -d      Stop and remove all containers"
            echo "  --rebuild, -r   Rebuild images from scratch"
            echo "  --help, -h      Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information."
            exit 1
            ;;
    esac
done

echo "============================================================"
echo "Hmm Docker Development Environment"
echo "============================================================"
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "ERROR: Docker is not running. Please start Docker and try again."
    exit 1
fi

echo "Docker is running."
echo ""

# Define compose file combinations
BASE_FILES="-f compose.base.yml"
IDP_FILES="$BASE_FILES -f compose.idp.yml"
ALL_FILES="$IDP_FILES -f compose.api.yml"

# Handle --down: stop all possible services
if [ $DOWN -eq 1 ]; then
    echo "Stopping and removing containers..."
    docker compose $ALL_FILES down
    echo ""
    echo "Environment stopped."
    exit 0
fi

# Validate scenario
if [ -z "$SCENARIO" ]; then
    echo "ERROR: Please specify a scenario with --scenario 1, 2, or 3"
    echo ""
    echo "Scenarios:"
    echo "  1  API local, IDP + SQL Servers + Seq in Docker"
    echo "  2  Everything in Docker"
    echo "  3  API + IDP local, SQL Servers + Seq in Docker"
    echo ""
    echo "Usage: ./hmm-setup.sh --scenario <1|2|3> [--rebuild]"
    echo "       ./hmm-setup.sh --down"
    exit 1
fi

if [[ "$SCENARIO" != "1" && "$SCENARIO" != "2" && "$SCENARIO" != "3" ]]; then
    echo "ERROR: Invalid scenario '$SCENARIO'. Must be 1, 2, or 3."
    exit 1
fi

# Select compose files based on scenario
case $SCENARIO in
    1) COMPOSE_FILES="$IDP_FILES" ;;
    2) COMPOSE_FILES="$ALL_FILES" ;;
    3) COMPOSE_FILES="$BASE_FILES" ;;
esac

# Rebuild if requested
if [ $REBUILD -eq 1 ]; then
    echo "Rebuilding: stopping existing containers..."
    docker compose $COMPOSE_FILES down
    echo "Building images from scratch (no cache)..."
    docker compose $COMPOSE_FILES build --no-cache
    echo ""
fi

# Start the environment
echo "Starting Scenario $SCENARIO..."
echo ""
case $SCENARIO in
    1) echo "  Docker: IDP + SQL Server (API) + SQL Server (IDP) + Seq" ;;
    2) echo "  Docker: API + IDP + SQL Server (API) + SQL Server (IDP) + Seq" ;;
    3) echo "  Docker: SQL Server (API) + SQL Server (IDP) + Seq" ;;
esac
echo ""

docker compose $COMPOSE_FILES up -d --build

echo ""
echo "============================================================"
echo "Scenario $SCENARIO Started Successfully!"
echo "============================================================"
echo ""

# Connection info
echo "Services:"
echo "  SQL Server (API): localhost,1433  (user: sa)"
echo "  SQL Server (IDP): localhost,14333 (user: sa)"
echo "  Seq UI:      http://localhost:8081"
echo "  Seq Ingest:  http://localhost:5341"

if [ "$SCENARIO" = "1" ] || [ "$SCENARIO" = "2" ]; then
    echo "  IDP:         http://localhost:5001"
fi
if [ "$SCENARIO" = "2" ]; then
    echo "  API:         http://localhost:5010"
    echo "  Swagger:     http://localhost:5010/swagger"
fi
echo ""

# Next steps for local services
if [ "$SCENARIO" = "1" ]; then
    echo "Next steps:"
    echo "  Run the API locally:"
    echo '  AppSettings__IdpBaseUrl="http://localhost:5001" dotnet run --project src/Hmm.ServiceApi'
    echo ""
elif [ "$SCENARIO" = "3" ]; then
    echo "Next steps:"
    echo "  1. Run the IDP locally:"
    echo "  dotnet run --project src/Hmm.Idp"
    echo ""
    echo "  2. Run the API locally:"
    echo "  dotnet run --project src/Hmm.ServiceApi"
    echo ""
fi

echo "============================================================"
echo "Commands:"
echo "  Stop:     ./hmm-setup.sh --down"
echo "  Rebuild:  ./hmm-setup.sh --scenario $SCENARIO --rebuild"
echo "  Logs:     docker compose $COMPOSE_FILES logs -f"
echo "============================================================"
