#!/bin/bash
# Deploy Hmm apps to cPanel shared hosting via SSH
# Usage: ./deploy-cpanel.sh --deploy api|idp|both [--build-only]
#        ./deploy-cpanel.sh --status
#        ./deploy-cpanel.sh --logs api|idp

set -e

# Configuration - update these for your environment
CPANEL_HOST="cp.homemademessage.com"
CPANEL_USER="${CPANEL_USER:-chipshoot}"
SSH_TARGET="${CPANEL_USER:+${CPANEL_USER}@}${CPANEL_HOST}"

API_REMOTE_DIR="api.homemademessage.com"
IDP_REMOTE_DIR="idp.homemademessage.com"
DATA_DIR="hmm-data"
LOG_DIR="hmm-logs"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PUBLISH_DIR="$REPO_ROOT/publish"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

check_ssh_target() {
    if [ -z "$CPANEL_USER" ]; then
        log_error "CPANEL_USER not set. Usage: CPANEL_USER=youruser ./deploy-cpanel.sh --deploy both"
        exit 1
    fi
}

publish_api() {
    log_info "Publishing Hmm.ServiceApi (self-contained, linux-x64)..."
    dotnet publish "$REPO_ROOT/src/Hmm.ServiceApi/Hmm.ServiceApi.csproj" \
        -c Release \
        -r linux-x64 \
        --self-contained true \
        -o "$PUBLISH_DIR/api"
    log_info "API published to $PUBLISH_DIR/api"
}

publish_idp() {
    log_info "Publishing Hmm.Idp (self-contained, linux-x64)..."
    dotnet publish "$REPO_ROOT/src/Hmm.Idp/Hmm.Idp.csproj" \
        -c Release \
        -r linux-x64 \
        --self-contained true \
        -o "$PUBLISH_DIR/idp"
    log_info "IDP published to $PUBLISH_DIR/idp"
}

deploy_api() {
    check_ssh_target
    log_info "Deploying API to $SSH_TARGET:~/$API_REMOTE_DIR..."
    ssh "$SSH_TARGET" "mkdir -p ~/$API_REMOTE_DIR ~/$DATA_DIR ~/$LOG_DIR"
    rsync -avz --delete "$PUBLISH_DIR/api/" "$SSH_TARGET:~/$API_REMOTE_DIR/"
    ssh "$SSH_TARGET" "chmod +x ~/$API_REMOTE_DIR/Hmm.ServiceApi"
    log_info "API deployed successfully."
}

deploy_idp() {
    check_ssh_target
    log_info "Deploying IDP to $SSH_TARGET:~/$IDP_REMOTE_DIR..."
    ssh "$SSH_TARGET" "mkdir -p ~/$IDP_REMOTE_DIR ~/$DATA_DIR ~/$LOG_DIR"
    rsync -avz --delete "$PUBLISH_DIR/idp/" "$SSH_TARGET:~/$IDP_REMOTE_DIR/"
    ssh "$SSH_TARGET" "chmod +x ~/$IDP_REMOTE_DIR/Hmm.Idp"
    log_info "IDP deployed successfully."
}

show_status() {
    check_ssh_target
    echo ""
    log_info "=== Deployment Status ==="
    echo ""

    # Check IDP
    echo -n "IDP (idp.homemademessage.com): "
    if curl -sf "https://idp.homemademessage.com/.well-known/openid-configuration" > /dev/null 2>&1; then
        echo -e "${GREEN}HEALTHY${NC}"
    else
        echo -e "${RED}UNREACHABLE${NC}"
    fi

    # Check API
    echo -n "API (api.homemademessage.com): "
    if curl -sf "https://api.homemademessage.com/swagger/v1/swagger.json" > /dev/null 2>&1; then
        echo -e "${GREEN}HEALTHY${NC}"
    else
        echo -e "${RED}UNREACHABLE${NC}"
    fi

    # Check remote DB files
    echo ""
    log_info "Database files:"
    ssh "$SSH_TARGET" "ls -lh ~/$DATA_DIR/*.db 2>/dev/null || echo '  No databases found'"

    echo ""
    log_info "Recent logs:"
    ssh "$SSH_TARGET" "ls -lt ~/$LOG_DIR/*.log 2>/dev/null | head -5 || echo '  No logs found'"
}

show_logs() {
    check_ssh_target
    local service="$1"
    case "$service" in
        api) ssh "$SSH_TARGET" "tail -100f ~/$LOG_DIR/api-*.log 2>/dev/null || echo 'No API logs found'" ;;
        idp) ssh "$SSH_TARGET" "tail -100f ~/$LOG_DIR/idp-*.log 2>/dev/null || echo 'No IDP logs found'" ;;
        *)   log_error "Usage: $0 --logs api|idp"; exit 1 ;;
    esac
}

show_help() {
    echo "Hmm cPanel Deployment Script"
    echo ""
    echo "Usage:"
    echo "  $0 --deploy api|idp|both [--build-only]  Build and deploy"
    echo "  $0 --status                               Check deployment health"
    echo "  $0 --logs api|idp                          Tail remote logs"
    echo ""
    echo "Environment:"
    echo "  CPANEL_USER  Required. Your cPanel SSH username."
    echo ""
    echo "First-time setup:"
    echo "  1. Create subdomains in cPanel: api.homemademessage.com, idp.homemademessage.com"
    echo "  2. Enable AutoSSL for both subdomains"
    echo "  3. Configure .NET Selector per app:"
    echo "     - App root: subdomain directory"
    echo "     - Startup file: Hmm.ServiceApi.dll / Hmm.Idp.dll"
    echo "     - Env var: ASPNETCORE_ENVIRONMENT=Production"
    echo "  4. For IDP first run: set SEED_DATA=true, then remove after seeding"
}

BUILD_ONLY=false

case "${1:-}" in
    --deploy)
        TARGET="${2:-both}"
        [ "${3:-}" = "--build-only" ] && BUILD_ONLY=true

        case "$TARGET" in
            api)
                publish_api
                [ "$BUILD_ONLY" = false ] && deploy_api
                ;;
            idp)
                publish_idp
                [ "$BUILD_ONLY" = false ] && deploy_idp
                ;;
            both)
                publish_api
                publish_idp
                if [ "$BUILD_ONLY" = false ]; then
                    deploy_api
                    deploy_idp
                fi
                ;;
            *)
                log_error "Invalid target: $TARGET. Use api, idp, or both."
                exit 1
                ;;
        esac
        ;;
    --status)
        show_status
        ;;
    --logs)
        show_logs "${2:-}"
        ;;
    --help|-h|"")
        show_help
        ;;
    *)
        log_error "Unknown option: $1"
        show_help
        exit 1
        ;;
esac
