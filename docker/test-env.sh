#!/usr/bin/env bash
#
# Test Environment Script for Hmm Backend (Scenario 2 - macOS)
#
# Starts the full Hmm backend in Docker:
#   - IDP uses PostgreSQL (embedded in the hmm-idp container)
#   - API uses SQLite mounted on the HOST filesystem
#
# Services started:
#   - Seq (logging UI)              localhost:8081
#   - Hmm IDP (IdentityServer)      localhost:5001  (PostgreSQL inside container)
#   - Hmm API (REST service)        localhost:5010  (SQLite on host)
#
# Seeded test users:
#   - admin@hmm.local      / Admin@12345678#    (Administrator)
#   - testuser@hmm.local   / TestPassword123#   (User)
#   - alice                / Alice@12345678#     (User)
#   - bob                  / Bob@123456789#      (User)
#
# Usage:
#   ./test-env.sh              Start environment + smoke tests
#   ./test-env.sh --down       Stop and remove containers
#   ./test-env.sh --rebuild    Force rebuild images (no cache)
#   ./test-env.sh --skip-tests Start without running smoke tests
#   ./test-env.sh --reset-db   Delete databases and restart fresh

set -euo pipefail

# ── Configuration ──────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

COMPOSE_FILES=(-f compose.base-sqlite.yml -f compose.idp.yml -f compose.api-sqlite.yml)

IDP_BASE_URL="http://localhost:5001"
API_BASE_URL="http://localhost:5010"
SEQ_URL="http://localhost:8081"

# IDP client credentials (must match HostingExtensions.cs seed data)
CLIENT_ID="hmm.functest"
CLIENT_SECRET="FuncTestSecret123#"

# Test user credentials (from SeedDataService)
TEST_USER="testuser@hmm.local"
TEST_PASSWORD="TestPassword123#"

MAX_WAIT_SECS=180

# Host-side data directory for API SQLite database (easy to inspect with DBeaver)
DATA_DIR="$SCRIPT_DIR/data"

# ── Parse arguments ──────────────────────────────────────────────────
ACTION="up"
REBUILD=false
SKIP_TESTS=false
RESET_DB=false

for arg in "$@"; do
    case "$arg" in
        --down|-d)       ACTION="down" ;;
        --rebuild|-r)    REBUILD=true ;;
        --skip-tests|-s) SKIP_TESTS=true ;;
        --reset-db)      RESET_DB=true ;;
        --help|-h)
            echo "Usage: $0 [--down] [--rebuild] [--skip-tests] [--reset-db]"
            exit 0
            ;;
        *)
            echo "Unknown option: $arg"
            echo "Usage: $0 [--down] [--rebuild] [--skip-tests] [--reset-db]"
            exit 1
            ;;
    esac
done

# ── Colors ────────────────────────────────────────────────────────────
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
RED='\033[0;31m'
GRAY='\033[0;90m'
WHITE='\033[1;37m'
NC='\033[0m'

banner()  { echo -e "\n${CYAN}$(printf '=%.0s' {1..60})\n  $1\n$(printf '=%.0s' {1..60})${NC}\n"; }
step()    { echo -e "${YELLOW}>> $1${NC}"; }
ok()      { echo -e "   ${GREEN}[OK]${NC} $1"; }
fail()    { echo -e "   ${RED}[FAIL]${NC} $1"; }
info()    { echo -e "   ${GRAY}$1${NC}"; }

test_url() {
    local url="$1" desc="$2"
    if curl -sf --max-time 5 -o /dev/null "$url"; then
        ok "$desc ($url)"
        return 0
    else
        fail "$desc - could not reach $url"
        return 1
    fi
}

# ── Check Docker ──────────────────────────────────────────────────────
banner "Hmm Test Environment (Scenario 2 - macOS)"

DOCKER_VERSION=$(docker version --format "{{.Server.Version}}" 2>/dev/null || true)
if [ -z "$DOCKER_VERSION" ]; then
    fail "Docker is not running. Please start Docker Desktop and try again."
    exit 1
fi
ok "Docker is running (Engine $DOCKER_VERSION)"

# ── Handle --down ─────────────────────────────────────────────────────
if [ "$ACTION" = "down" ]; then
    step "Stopping test environment..."
    docker compose "${COMPOSE_FILES[@]}" down --volumes --remove-orphans
    ok "Test environment stopped and volumes removed"
    echo ""
    info "Host data directory preserved at: $DATA_DIR"
    info "To remove API database files: rm -rf $DATA_DIR"
    info "IDP PostgreSQL data was in Docker volume (removed with --volumes)"
    exit 0
fi

# ── Handle --reset-db ─────────────────────────────────────────────────
if [ "$RESET_DB" = true ]; then
    step "Resetting databases..."
    # Stop containers first so db files are not locked
    docker compose "${COMPOSE_FILES[@]}" down --volumes --remove-orphans 2>/dev/null || true

    # Remove API SQLite files from host
    if [ -d "$DATA_DIR" ]; then
        rm -f "$DATA_DIR"/hmm.db "$DATA_DIR"/hmm.db-wal "$DATA_DIR"/hmm.db-shm
        ok "Deleted API SQLite database files from $DATA_DIR"
    else
        info "No data directory found at $DATA_DIR — nothing to delete"
    fi

    ok "IDP PostgreSQL volume removed (via --volumes flag above)"
fi

# ── Prepare host data directory ───────────────────────────────────────
step "Preparing host data directories..."
mkdir -p "$DATA_DIR"
ok "Data directory: $DATA_DIR"

# ── Export bind mount paths for compose override ──────────────────────
export HMM_DATA_DIR="$DATA_DIR"

# Write a temporary compose override to use host bind mounts for API SQLite database
# IDP uses PostgreSQL inside its container (data persisted via idp-postgres-data volume)
OVERRIDE_FILE="$SCRIPT_DIR/.compose.host-data.yml"
cat > "$OVERRIDE_FILE" <<'YAML'
# Auto-generated by test-env.sh - mounts API SQLite database on the host
services:
  hmm-api:
    volumes:
      - ${HMM_DATA_DIR}:/app/data
YAML

COMPOSE_FILES+=(-f "$OVERRIDE_FILE")
info "Using host bind mounts for API (override: .compose.host-data.yml)"

# ── Start Services ────────────────────────────────────────────────────
if [ "$REBUILD" = true ]; then
    step "Tearing down existing containers..."
    docker compose "${COMPOSE_FILES[@]}" down --volumes --remove-orphans
    step "Rebuilding images from scratch..."
    docker compose "${COMPOSE_FILES[@]}" build --no-cache
fi

step "Starting Scenario 2 (all services in Docker)..."
info "Compose: compose.base-sqlite.yml + compose.idp.yml + compose.api-sqlite.yml + host-data override"
info "IDP: PostgreSQL (embedded in container)  |  API: SQLite (host: $DATA_DIR)"
echo ""

docker compose "${COMPOSE_FILES[@]}" up -d --build

if [ $? -ne 0 ]; then
    fail "Failed to start containers"
    exit 1
fi
ok "Docker containers started"

# ── Wait for IDP ──────────────────────────────────────────────────────
step "Waiting for IDP to be ready (up to ${MAX_WAIT_SECS}s)..."

elapsed=0
idp_ready=false
while [ $elapsed -lt $MAX_WAIT_SECS ]; do
    if curl -sf --max-time 3 -o /dev/null "$IDP_BASE_URL/.well-known/openid-configuration"; then
        idp_ready=true
        break
    fi
    sleep 3
    elapsed=$((elapsed + 3))
    echo -n "."
done
echo ""

if [ "$idp_ready" = false ]; then
    fail "IDP did not become ready within ${MAX_WAIT_SECS}s"
    info "Check logs: docker compose ${COMPOSE_FILES[*]} logs hmm-idp"
    exit 1
fi
ok "IDP is ready (${elapsed}s)"

# ── Wait for API ──────────────────────────────────────────────────────
step "Waiting for API to be ready (up to ${MAX_WAIT_SECS}s)..."

elapsed=0
api_ready=false
while [ $elapsed -lt $MAX_WAIT_SECS ]; do
    if curl -sf --max-time 3 -o /dev/null "$API_BASE_URL/swagger/index.html"; then
        api_ready=true
        break
    fi
    sleep 3
    elapsed=$((elapsed + 3))
    echo -n "."
done
echo ""

if [ "$api_ready" = false ]; then
    fail "API did not become ready within ${MAX_WAIT_SECS}s"
    info "Check logs: docker compose ${COMPOSE_FILES[*]} logs hmm-api"
    exit 1
fi
ok "API is ready (${elapsed}s)"

# ── Smoke Tests ───────────────────────────────────────────────────────
if [ "$SKIP_TESTS" = true ]; then
    step "Skipping smoke tests (--skip-tests)"
else
    banner "Smoke Tests"

    passed=0
    failed=0

    # Test 1: IDP Discovery endpoint
    step "Test 1: IDP Discovery endpoint"
    if test_url "$IDP_BASE_URL/.well-known/openid-configuration" "OpenID Configuration"; then
        passed=$((passed + 1))
    else
        failed=$((failed + 1))
    fi

    # Test 2: IDP Login page
    step "Test 2: IDP Login page"
    if test_url "$IDP_BASE_URL/Account/Login" "Login page"; then
        passed=$((passed + 1))
    else
        failed=$((failed + 1))
    fi

    # Test 3: Swagger UI
    step "Test 3: API Swagger UI"
    if test_url "$API_BASE_URL/swagger/index.html" "Swagger UI"; then
        passed=$((passed + 1))
    else
        failed=$((failed + 1))
    fi

    # Test 4: Seq logging UI
    step "Test 4: Seq Logging UI"
    if test_url "$SEQ_URL" "Seq UI"; then
        passed=$((passed + 1))
    else
        failed=$((failed + 1))
    fi

    # Test 5: Token exchange (ROPC grant with test user)
    step "Test 5: IDP Token exchange (ROPC grant)"
    token_response=$(curl -sf --max-time 10 \
        -X POST "$IDP_BASE_URL/connect/token" \
        -H "Content-Type: application/x-www-form-urlencoded" \
        -d "grant_type=password&client_id=$CLIENT_ID&client_secret=$CLIENT_SECRET&username=$TEST_USER&password=$TEST_PASSWORD&scope=openid+profile+hmmapi+offline_access" \
        2>/dev/null || true)

    access_token=""
    if [ -n "$token_response" ]; then
        access_token=$(echo "$token_response" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('access_token',''))" 2>/dev/null || true)
    fi

    if [ -n "$access_token" ]; then
        token_type=$(echo "$token_response" | python3 -c "import sys,json; print(json.load(sys.stdin).get('token_type',''))" 2>/dev/null || true)
        expires_in=$(echo "$token_response" | python3 -c "import sys,json; print(json.load(sys.stdin).get('expires_in',''))" 2>/dev/null || true)
        ok "Token acquired for $TEST_USER"
        info "Token type: $token_type, expires in: ${expires_in}s"
        passed=$((passed + 1))
    else
        fail "Token exchange failed"
        failed=$((failed + 1))
    fi

    # Test 6: Authenticated API call (GET /api/v1/authors)
    step "Test 6: Authenticated API call (GET /api/v1/authors)"
    if [ -n "$access_token" ]; then
        api_response=$(curl -sf --max-time 10 \
            -H "Authorization: Bearer $access_token" \
            "$API_BASE_URL/api/v1/authors" 2>/dev/null || true)
        if [ -n "$api_response" ]; then
            ok "API returned authors successfully"
            passed=$((passed + 1))
        else
            fail "API call failed"
            failed=$((failed + 1))
        fi
    else
        fail "Skipped (no token from Test 5)"
        failed=$((failed + 1))
    fi

    # Test 7: Verify external login providers are registered
    step "Test 7: External login providers visible on login page"
    login_html=$(curl -sf --max-time 5 "$IDP_BASE_URL/Account/Login" 2>/dev/null || true)
    if [ -n "$login_html" ]; then
        google_found=false
        github_found=false
        echo "$login_html" | grep -qi "Google" && google_found=true
        echo "$login_html" | grep -qi "GitHub" && github_found=true

        if [ "$google_found" = true ] && [ "$github_found" = true ]; then
            ok "Both Google and GitHub login buttons found"
            passed=$((passed + 1))
        elif [ "$google_found" = true ] || [ "$github_found" = true ]; then
            found="Google"; missing="GitHub"
            [ "$github_found" = true ] && found="GitHub" && missing="Google"
            ok "$found button found, $missing button missing (may need valid client credentials)"
            passed=$((passed + 1))
        else
            fail "Neither Google nor GitHub buttons found on login page"
            info "External providers may not register without valid ClientId/ClientSecret"
            failed=$((failed + 1))
        fi
    else
        fail "Could not load login page"
        failed=$((failed + 1))
    fi

    # Results
    echo ""
    echo -e "${CYAN}$(printf -- '-%.0s' {1..60})${NC}"
    if [ $failed -eq 0 ]; then
        echo -e "  ${GREEN}All $passed tests passed!${NC}"
    else
        echo -e "  ${RED}Results: $passed passed, $failed failed${NC}"
    fi
    echo -e "${CYAN}$(printf -- '-%.0s' {1..60})${NC}"
fi

# ── Summary ───────────────────────────────────────────────────────────
banner "Test Environment Ready"

echo -e "  ${CYAN}Services:${NC}"
echo -e "    IDP:           ${WHITE}$IDP_BASE_URL${NC}"
echo -e "    IDP Login:     ${WHITE}$IDP_BASE_URL/Account/Login${NC}"
echo -e "    API:           ${WHITE}$API_BASE_URL${NC}"
echo -e "    Swagger:       ${WHITE}$API_BASE_URL/swagger${NC}"
echo -e "    Seq Logs:      ${WHITE}$SEQ_URL${NC}"
echo ""
echo -e "  ${CYAN}Databases:${NC}"
echo -e "    IDP:           ${WHITE}PostgreSQL (embedded in hmm-idp container, volume: idp-postgres-data)${NC}"
echo -e "    API:           ${WHITE}SQLite (host: $DATA_DIR/hmm.db)${NC}"
echo ""
echo -e "  ${CYAN}API SQLite file on host (open with DBeaver):${NC}"
echo -e "    API DB:        ${WHITE}$DATA_DIR/hmm.db${NC}"
echo ""
echo -e "  ${CYAN}IDP PostgreSQL (connect with DBeaver/pgAdmin via docker exec):${NC}"
echo -e "    Container:     ${WHITE}hmm-idp${NC}"
echo -e "    Database:      ${WHITE}HmmIdp${NC}"
echo -e "    User:          ${WHITE}postgres${NC}"
echo -e "    Connect:       ${WHITE}docker exec -it hmm-idp su postgres -c \"psql -h 127.0.0.1 -d HmmIdp\"${NC}"
echo ""
echo -e "  ${CYAN}Test Users:${NC}"
echo -e "    admin@hmm.local      / Admin@12345678#   (Administrator)"
echo -e "    testuser@hmm.local   / TestPassword123#  (User)"
echo -e "    alice                / Alice@12345678#    (User)"
echo -e "    bob                  / Bob@123456789#     (User)"
echo ""
echo -e "  ${CYAN}Hmm_Console (Flutter) connection:${NC}"
echo -e "    IDP Authority:   ${WHITE}http://localhost:5001${NC}"
echo -e "    API Base URL:    ${WHITE}http://localhost:5010/api/v1${NC}"
echo -e "    Client ID:       ${WHITE}hmm.functest${NC}"
echo -e "    Client Secret:   ${WHITE}FuncTestSecret123#${NC}"
echo ""
echo -e "  ${CYAN}Commands:${NC}"
echo -e "    ${GRAY}Stop:      ./test-env.sh --down${NC}"
echo -e "    ${GRAY}Rebuild:   ./test-env.sh --rebuild${NC}"
echo -e "    ${GRAY}Reset DB:  ./test-env.sh --reset-db${NC}"
echo -e "    ${GRAY}Logs:      docker compose ${COMPOSE_FILES[*]} logs -f${NC}"
echo -e "    ${GRAY}IDP logs:  docker compose ${COMPOSE_FILES[*]} logs -f hmm-idp${NC}"
echo -e "    ${GRAY}API logs:  docker compose ${COMPOSE_FILES[*]} logs -f hmm-api${NC}"
echo ""
