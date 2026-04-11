#!/bin/bash
# ============================================================
# Production Deployment Script for Hmm (Cloudflare)
# ============================================================
#
# Deploys API + IDP in Docker:
#   - API uses SQLite database on the host
#   - IDP uses PostgreSQL (embedded in hmm-idp container)
# Designed for macOS with Cloudflare Tunnel for public access.
#
# Usage:
#   ./hmm-deploy.sh --start [--build|--rebuild]
#   ./hmm-deploy.sh --stop
#   ./hmm-deploy.sh --status
#   ./hmm-deploy.sh --logs [service]
#   ./hmm-deploy.sh --backup
# ============================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Compose file combination for production (API: SQLite, IDP: PostgreSQL)
COMPOSE_FILES="-f compose.base-sqlite.yml -f compose.idp.yml -f compose.api-sqlite.yml"

# Default OneDrive path on macOS
SQLITE_DATA_DIR="${HMM_DATA_DIR:-$HOME/Library/CloudStorage/OneDrive-Personal/hmm-data}"
BACKUP_DIR="${HMM_BACKUP_DIR:-$HOME/hmm-backups}"

# Parse arguments
ACTION=""
BUILD_FLAG=""
LOG_SERVICE=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --start)
            ACTION="start"
            shift
            ;;
        --stop)
            ACTION="stop"
            shift
            ;;
        --status)
            ACTION="status"
            shift
            ;;
        --logs)
            ACTION="logs"
            if [[ -n "$2" && ! "$2" =~ ^-- ]]; then
                LOG_SERVICE="$2"
                shift
            fi
            shift
            ;;
        --backup)
            ACTION="backup"
            shift
            ;;
        --build|-b)
            BUILD_FLAG="--build"
            shift
            ;;
        --rebuild|-r)
            BUILD_FLAG="rebuild"
            shift
            ;;
        --help|-h)
            echo "Usage: ./hmm-deploy.sh <action> [options]"
            echo ""
            echo "Actions:"
            echo "  --start             Start production containers"
            echo "  --stop              Stop and remove containers"
            echo "  --status            Show container status and health"
            echo "  --logs [service]    Follow logs (optional: hmm-api, hmm-idp, hmm-seq)"
            echo "  --backup            Backup API SQLite and IDP PostgreSQL databases"
            echo ""
            echo "Options:"
            echo "  --build, -b         Build images before starting"
            echo "  --rebuild, -r       Rebuild images from scratch (no cache)"
            echo ""
            echo "Environment variables:"
            echo "  HMM_DATA_DIR        API SQLite data directory (default: ~/Library/CloudStorage/OneDrive-Personal/hmm-data)"
            echo "  HMM_BACKUP_DIR      Backup directory (default: ~/hmm-backups)"
            exit 0
            ;;
        *)
            echo "Unknown option: $1 (use --help for usage)"
            exit 1
            ;;
    esac
done

if [ -z "$ACTION" ]; then
    echo "ERROR: No action specified. Use --help for usage."
    exit 1
fi

# Check Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        echo "ERROR: Docker is not running. Please start Docker Desktop."
        exit 1
    fi
}

# ============================================================
# Actions
# ============================================================

case $ACTION in
    start)
        check_docker
        echo "============================================================"
        echo "Hmm Production Deployment (API: SQLite, IDP: PostgreSQL)"
        echo "============================================================"
        echo ""

        # Ensure API SQLite data directory exists
        if [ ! -d "$SQLITE_DATA_DIR" ]; then
            echo "Creating data directory: $SQLITE_DATA_DIR"
            mkdir -p "$SQLITE_DATA_DIR"
        fi
        echo "API SQLite data: $SQLITE_DATA_DIR"
        echo ""

        # Handle rebuild
        if [ "$BUILD_FLAG" = "rebuild" ]; then
            echo "Stopping existing containers..."
            docker compose $COMPOSE_FILES down 2>/dev/null || true
            echo "Rebuilding images (no cache)..."
            docker compose $COMPOSE_FILES build --no-cache
            BUILD_FLAG=""
            echo ""
        fi

        # Start containers
        echo "Starting containers..."
        docker compose $COMPOSE_FILES up -d $BUILD_FLAG

        # Wait for health checks
        echo ""
        echo "Waiting for services to be ready..."
        sleep 5

        # Show status
        echo ""
        docker ps --filter "name=hmm-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

        echo ""
        echo "============================================================"
        echo "Production Stack Running"
        echo "============================================================"
        echo ""
        echo "Local endpoints:"
        echo "  API:         http://localhost:5010"
        echo "  Swagger:     http://localhost:5010/swagger"
        echo "  IDP:         http://localhost:5001"
        echo "  Seq:         http://localhost:8081"
        echo ""
        echo "API SQLite database: $SQLITE_DATA_DIR/hmm.db"
        echo "IDP PostgreSQL:     embedded in hmm-idp container (volume: idp-postgres-data)"
        echo ""
        echo "Commands:"
        echo "  Stop:    ./hmm-deploy.sh --stop"
        echo "  Logs:    ./hmm-deploy.sh --logs"
        echo "  Status:  ./hmm-deploy.sh --status"
        echo "  Backup:  ./hmm-deploy.sh --backup"
        echo "============================================================"
        ;;

    stop)
        check_docker
        echo "Stopping production containers..."
        docker compose $COMPOSE_FILES down
        echo ""
        echo "Production stack stopped."
        ;;

    status)
        check_docker
        echo "============================================================"
        echo "Container Status"
        echo "============================================================"
        echo ""
        docker ps --filter "name=hmm-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
        echo ""

        # Check API SQLite database
        if [ -f "$SQLITE_DATA_DIR/hmm.db" ]; then
            DB_SIZE=$(du -h "$SQLITE_DATA_DIR/hmm.db" | cut -f1)
            echo "API SQLite database: $SQLITE_DATA_DIR/hmm.db ($DB_SIZE)"
        else
            echo "API SQLite database: not created yet"
        fi

        # Check IDP PostgreSQL
        if docker ps --format '{{.Names}}' | grep -q hmm-idp; then
            echo "IDP PostgreSQL:     embedded in hmm-idp container (volume: idp-postgres-data)"
        else
            echo "IDP PostgreSQL:     hmm-idp container not running"
        fi
        echo ""

        # Check Cloudflare Tunnel
        if command -v cloudflared &> /dev/null; then
            if pgrep -x cloudflared > /dev/null; then
                echo "Cloudflare Tunnel: running"
            else
                echo "Cloudflare Tunnel: not running"
            fi
        else
            echo "Cloudflare Tunnel: not installed (brew install cloudflared)"
        fi

        # Quick health check
        echo ""
        echo "Health checks:"
        API_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5010/swagger/v1/swagger.json 2>/dev/null || echo "down")
        IDP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5001 2>/dev/null || echo "down")
        echo "  API:  $API_STATUS"
        echo "  IDP:  $IDP_STATUS"
        ;;

    logs)
        check_docker
        if [ -n "$LOG_SERVICE" ]; then
            docker compose $COMPOSE_FILES logs -f "$LOG_SERVICE"
        else
            docker compose $COMPOSE_FILES logs -f
        fi
        ;;

    backup)
        check_docker
        echo "============================================================"
        echo "Backup"
        echo "============================================================"
        echo ""

        TIMESTAMP=$(date +%Y%m%d_%H%M%S)
        mkdir -p "$BACKUP_DIR"

        # Backup API SQLite
        if [ -f "$SQLITE_DATA_DIR/hmm.db" ]; then
            echo "Backing up API SQLite database..."
            echo "  Stopping API to checkpoint WAL..."
            docker stop hmm-api 2>/dev/null || true
            sleep 2
            cp "$SQLITE_DATA_DIR/hmm.db" "$BACKUP_DIR/hmm-$TIMESTAMP.db"
            echo "  Starting API..."
            docker start hmm-api
            echo "  Saved: $BACKUP_DIR/hmm-$TIMESTAMP.db"
        else
            echo "  API SQLite database not found, skipping."
        fi
        echo ""

        # Backup IDP PostgreSQL (embedded in hmm-idp container)
        echo "Backing up IDP PostgreSQL database..."
        if docker ps --format '{{.Names}}' | grep -q hmm-idp; then
            docker exec hmm-idp su postgres -c "pg_dump -h 127.0.0.1 HmmIdp" \
                > "$BACKUP_DIR/HmmIdp-$TIMESTAMP.sql" 2>/dev/null && \
            echo "  Saved: $BACKUP_DIR/HmmIdp-$TIMESTAMP.sql" || \
            echo "  IDP PostgreSQL backup failed (database may not exist yet)"
        else
            echo "  hmm-idp container not running, skipping."
        fi

        echo ""
        echo "Backups saved to: $BACKUP_DIR"
        ls -lh "$BACKUP_DIR"/*$TIMESTAMP* 2>/dev/null || true
        echo "============================================================"
        ;;
esac
