#!/bin/bash
# ============================================================
# Local Dev Deployment Script for Hmm (Docker)
# ============================================================
#
# DEV ONLY. Not the production deployment path — that's bare-metal
# systemd on an Oracle VPS, provisioned by
# scripts/setup-{idp,api,backup}-vps.sh and pushed via
# scripts/deploy-{idp,api}.sh. See docs/PRODUCTION_VPS_DEPLOY.md.
#
# This script runs the full stack inside Docker on a local
# workstation (typically macOS) so you can iterate on the IDP +
# API end-to-end without touching the VPS:
#   - API uses PostgreSQL embedded in the hmm-api container
#     (volume: api-postgres-data — see compose.api.yml).
#   - IDP uses PostgreSQL embedded in the hmm-idp container
#     (volume: idp-postgres-data — see compose.idp.yml).
# Optional: Cloudflare Tunnel (cloudflared) for sharing the local
# stack publicly during dev/demo. Production does not use this.
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

# Compose file combination for the local dev stack.
# compose.base-sqlite.yml hosts the shared infra (Seq + Mailpit)
# without the legacy SQL Server container — the API and IDP each
# embed their own Postgres instance.
COMPOSE_FILES="-f compose.base-sqlite.yml -f compose.idp.yml -f compose.api.yml"

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
            echo "  --start             Start the local dev stack"
            echo "  --stop              Stop and remove containers"
            echo "  --status            Show container status and health"
            echo "  --logs [service]    Follow logs (optional: hmm-api, hmm-idp, hmm-seq)"
            echo "  --backup            Backup API + IDP PostgreSQL + attachment vault"
            echo ""
            echo "Options:"
            echo "  --build, -b         Build images before starting"
            echo "  --rebuild, -r       Rebuild images from scratch (no cache)"
            echo ""
            echo "Environment variables:"
            echo "  HMM_BACKUP_DIR      Backup directory (default: ~/hmm-backups)"
            echo ""
            echo "Restore order (when manually restoring from a backup set):"
            echo "  1. Restore HmmIdp + HmmNotes PostgreSQL dumps FIRST"
            echo "     (e.g. psql -h 127.0.0.1 -U postgres HmmNotes < HmmNotes-<ts>.sql)"
            echo "  2. THEN extract the vault tarball into the api-vault-data"
            echo "     volume — Postgres holds the Notes.attachments JSON that"
            echo "     references vault paths, so DB must be in place before the"
            echo "     bytes show up. Recovering bytes without DB rows leaves"
            echo "     them as orphans; recovering DB without bytes leaves the"
            echo "     UI rendering placeholders until the vault arrives."
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
        echo "Hmm Local Dev Stack (Docker — embedded PostgreSQL per service)"
        echo "============================================================"
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
        echo "Local Dev Stack Running"
        echo "============================================================"
        echo ""
        echo "Local endpoints:"
        echo "  API:         http://localhost:5010"
        echo "  Swagger:     http://localhost:5010/swagger"
        echo "  IDP:         http://localhost:5001"
        echo "  Seq:         http://localhost:8081"
        echo "  Mailpit UI:  http://localhost:8025"
        echo ""
        echo "API PostgreSQL: embedded in hmm-api container (volume: api-postgres-data)"
        echo "IDP PostgreSQL: embedded in hmm-idp container (volume: idp-postgres-data)"
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
        echo "Stopping local dev containers..."
        docker compose $COMPOSE_FILES down
        echo ""
        echo "Local dev stack stopped."
        ;;

    status)
        check_docker
        echo "============================================================"
        echo "Container Status"
        echo "============================================================"
        echo ""
        docker ps --filter "name=hmm-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
        echo ""

        # Check API PostgreSQL
        if docker ps --format '{{.Names}}' | grep -q hmm-api; then
            echo "API PostgreSQL: embedded in hmm-api container (volume: api-postgres-data)"
        else
            echo "API PostgreSQL: hmm-api container not running"
        fi

        # Check IDP PostgreSQL
        if docker ps --format '{{.Names}}' | grep -q hmm-idp; then
            echo "IDP PostgreSQL: embedded in hmm-idp container (volume: idp-postgres-data)"
        else
            echo "IDP PostgreSQL: hmm-idp container not running"
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

        # Backup API PostgreSQL (embedded in hmm-api container)
        echo "Backing up API PostgreSQL database..."
        if docker ps --format '{{.Names}}' | grep -q hmm-api; then
            docker exec hmm-api su postgres -c "pg_dump -h 127.0.0.1 HmmNotes" \
                > "$BACKUP_DIR/HmmNotes-$TIMESTAMP.sql" 2>/dev/null && \
            echo "  Saved: $BACKUP_DIR/HmmNotes-$TIMESTAMP.sql" || \
            echo "  API PostgreSQL backup failed (database may not exist yet)"
        else
            echo "  hmm-api container not running, skipping."
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

        # Backup attachment vault (api-vault-data volume mounted at
        # /var/lib/hmm-vault inside hmm-api). Tar from the container
        # side so the host platform doesn't matter — works whether
        # the volume sits on macOS or Linux, and skips any host bind
        # gymnastics. Empty vault is still archived (an empty tar
        # decompresses cleanly), so restores stay symmetric.
        echo "Backing up attachment vault..."
        if docker ps --format '{{.Names}}' | grep -q hmm-api; then
            VAULT_TAR="$BACKUP_DIR/hmm-vault-$TIMESTAMP.tar.gz"
            # -C into the vault root so paths inside the tarball
            # are relative ("authors/N/..." rather than
            # "/var/lib/hmm-vault/authors/N/..."), making restores
            # portable to a different mount point if needed.
            if docker exec hmm-api tar -C /var/lib/hmm-vault -czf - . > "$VAULT_TAR" 2>/dev/null; then
                VAULT_SIZE=$(du -h "$VAULT_TAR" | cut -f1)
                echo "  Saved: $VAULT_TAR ($VAULT_SIZE)"
            else
                echo "  Vault backup failed (vault dir may not exist yet)"
                # Don't leave a half-written file behind.
                rm -f "$VAULT_TAR"
            fi
        else
            echo "  hmm-api container not running, skipping."
        fi

        echo ""
        echo "Backups saved to: $BACKUP_DIR"
        ls -lh "$BACKUP_DIR"/*$TIMESTAMP* 2>/dev/null || true
        echo ""
        echo "Restore order: Postgres dumps FIRST, then the vault tarball."
        echo "(See ./hmm-deploy.sh --help for the full restore note.)"
        echo "============================================================"
        ;;
esac
