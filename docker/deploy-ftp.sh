#!/bin/bash
# Deploy Hmm apps to cPanel via FTP
# Usage: ./deploy-ftp.sh --deploy api|idp|both [--build-only|--upload-only]
#        ./deploy-ftp.sh --help
#
# Environment variables:
#   FTP_HOST     FTP server (default: cp.homemademessage.com)
#   FTP_USER     FTP username (required)
#   FTP_PASS     FTP password (required, or set via .env.ftp file)
#
# Requires: lftp (apt install lftp / brew install lftp)

set -e

# Configuration
FTP_HOST="${FTP_HOST:-cp.homemademessage.com}"
FTP_USER="${FTP_USER:-}"
FTP_PASS="${FTP_PASS:-}"

API_REMOTE_DIR="/api.homemademessage.com"
IDP_REMOTE_DIR="/idp.homemademessage.com"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PUBLISH_DIR="$REPO_ROOT/publish"
ENV_FILE="$SCRIPT_DIR/.env.ftp"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info()  { echo -e "${GREEN}[INFO]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Load credentials from .env.ftp if it exists
load_env() {
    if [ -f "$ENV_FILE" ]; then
        log_info "Loading credentials from $ENV_FILE"
        # shellcheck source=/dev/null
        source "$ENV_FILE"
    fi
}

check_prerequisites() {
    if ! command -v lftp &> /dev/null; then
        log_error "lftp is not installed."
        echo "  Install with: apt install lftp (Linux) or brew install lftp (macOS)"
        exit 1
    fi

    if [ -z "$FTP_USER" ] || [ -z "$FTP_PASS" ]; then
        log_error "FTP_USER and FTP_PASS are required."
        echo ""
        echo "  Option 1: Set environment variables"
        echo "    FTP_USER=youruser FTP_PASS=yourpass $0 --deploy api"
        echo ""
        echo "  Option 2: Create $ENV_FILE with:"
        echo "    FTP_USER=youruser"
        echo "    FTP_PASS=yourpass"
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

ftp_upload() {
    local local_dir="$1"
    local remote_dir="$2"
    local label="$3"

    log_info "Uploading $label to ftp://$FTP_HOST$remote_dir ..."

    lftp -u "$FTP_USER","$FTP_PASS" "ftp://$FTP_HOST" <<EOF
set ssl:verify-certificate no
set ftp:ssl-allow yes
set net:timeout 30
set net:max-retries 3
set net:reconnect-interval-base 5
mirror --reverse --delete --verbose --parallel=4 \
    "$local_dir" "$remote_dir"
bye
EOF

    log_info "$label uploaded successfully to $remote_dir"
}

upload_api() {
    ftp_upload "$PUBLISH_DIR/api" "$API_REMOTE_DIR" "API"
}

upload_idp() {
    ftp_upload "$PUBLISH_DIR/idp" "$IDP_REMOTE_DIR" "IDP"
}

show_help() {
    echo "Hmm cPanel FTP Deployment Script"
    echo ""
    echo "Usage:"
    echo "  $0 --deploy api|idp|both                 Build and upload"
    echo "  $0 --deploy api|idp|both --build-only    Build only, skip FTP upload"
    echo "  $0 --deploy api|idp|both --upload-only   Upload only, skip build"
    echo "  $0 --help                                 Show this help"
    echo ""
    echo "Environment:"
    echo "  FTP_HOST  FTP server (default: cp.homemademessage.com)"
    echo "  FTP_USER  FTP username (required)"
    echo "  FTP_PASS  FTP password (required)"
    echo ""
    echo "Or create docker/.env.ftp with FTP_USER and FTP_PASS."
    echo ""
    echo "First-time setup on cPanel:"
    echo "  1. Create subdomains in cPanel: api.homemademessage.com, idp.homemademessage.com"
    echo "  2. Enable AutoSSL for both subdomains"
    echo "  3. Configure .NET Selector per app:"
    echo "     - API: App root=$API_REMOTE_DIR, Startup=Hmm.ServiceApi.dll"
    echo "     - IDP: App root=$IDP_REMOTE_DIR, Startup=Hmm.Idp.dll"
    echo "     - Env var: ASPNETCORE_ENVIRONMENT=Production"
    echo "  4. For IDP first run: set SEED_DATA=true, then remove after seeding"
}

# Main
load_env

case "${1:-}" in
    --deploy)
        TARGET="${2:-}"
        MODE="${3:-}"

        if [ -z "$TARGET" ]; then
            log_error "Missing target. Usage: $0 --deploy api|idp|both"
            exit 1
        fi

        case "$MODE" in
            --build-only)
                case "$TARGET" in
                    api)  publish_api ;;
                    idp)  publish_idp ;;
                    both) publish_api; publish_idp ;;
                    *)    log_error "Invalid target: $TARGET. Use api, idp, or both."; exit 1 ;;
                esac
                ;;
            --upload-only)
                check_prerequisites
                case "$TARGET" in
                    api)
                        [ ! -d "$PUBLISH_DIR/api" ] && { log_error "No publish output at $PUBLISH_DIR/api. Build first."; exit 1; }
                        upload_api
                        ;;
                    idp)
                        [ ! -d "$PUBLISH_DIR/idp" ] && { log_error "No publish output at $PUBLISH_DIR/idp. Build first."; exit 1; }
                        upload_idp
                        ;;
                    both)
                        [ ! -d "$PUBLISH_DIR/api" ] && { log_error "No publish output at $PUBLISH_DIR/api. Build first."; exit 1; }
                        [ ! -d "$PUBLISH_DIR/idp" ] && { log_error "No publish output at $PUBLISH_DIR/idp. Build first."; exit 1; }
                        upload_api
                        upload_idp
                        ;;
                    *)  log_error "Invalid target: $TARGET. Use api, idp, or both."; exit 1 ;;
                esac
                ;;
            "")
                check_prerequisites
                case "$TARGET" in
                    api)  publish_api; upload_api ;;
                    idp)  publish_idp; upload_idp ;;
                    both) publish_api; publish_idp; upload_api; upload_idp ;;
                    *)    log_error "Invalid target: $TARGET. Use api, idp, or both."; exit 1 ;;
                esac
                ;;
            *)
                log_error "Unknown mode: $MODE. Use --build-only or --upload-only."
                exit 1
                ;;
        esac
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
