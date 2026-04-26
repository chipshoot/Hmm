#!/usr/bin/env bash
# ============================================================
# setup-api-vps.sh — environment bootstrap for Hmm.ServiceApi
# on the same Oracle VPS that already runs Hmm.Idp.
#
# Companion to scripts/setup-idp-vps.sh. Reuses everything that's
# already installed (Postgres, ASP.NET Core 10, Caddy) and only adds
# what's specific to the API:
#   - hmm-api system user
#   - HmmNotes Postgres database with dedicated hmm_api role
#   - /opt/hmm-api, /var/log/hmm-api, /var/lib/hmm-api-data, /etc/hmm-api
#   - /etc/hmm-api/api.env
#   - /etc/systemd/system/hmm-api.service  (port 8081)
#   - api.homemademessage.com block appended to /etc/caddy/Caddyfile
#
# Run AFTER setup-idp-vps.sh (or on a system where Postgres + dotnet
# 10 + Caddy are already installed). Re-runnable.
# ============================================================
set -euo pipefail

# ----- Tunables --------------------------------------------------
DOMAIN="${DOMAIN:-api.homemademessage.com}"
APP_USER="${APP_USER:-hmm-api}"
APP_DIR="${APP_DIR:-/opt/hmm-api}"
LOG_DIR="${LOG_DIR:-/var/log/hmm-api}"
DATA_DIR="${DATA_DIR:-/var/lib/hmm-api-data}"  # SQLite fallback / asset uploads
ENV_DIR="${ENV_DIR:-/etc/hmm-api}"
ENV_FILE="${ENV_DIR}/api.env"
APP_PORT="${APP_PORT:-8081}"                   # API listens on 127.0.0.1:${APP_PORT}
PG_DB="${PG_DB:-HmmNotes}"
PG_USER="${PG_USER:-hmm_api}"
IDP_BASE_URL="${IDP_BASE_URL:-https://idp.homemademessage.com}"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-10.0}"
CADDY_FILE="${CADDY_FILE:-/etc/caddy/Caddyfile}"

# ----- Helpers ---------------------------------------------------
log()  { printf '\n\033[1;34m[setup]\033[0m %s\n' "$*"; }
warn() { printf '\n\033[1;33m[warn ]\033[0m %s\n' "$*"; }
die()  { printf '\n\033[1;31m[fail ]\033[0m %s\n' "$*" >&2; exit 1; }

require_root() { [ "$(id -u)" -eq 0 ] || die "Run with sudo or as root."; }

# ----- Preconditions --------------------------------------------
require_root
export DEBIAN_FRONTEND=noninteractive

# ----- 1. Sanity-check: Postgres + dotnet must already exist ----
if ! systemctl is-active --quiet postgresql; then
  die "PostgreSQL is not running. Run setup-idp-vps.sh first."
fi
if ! command -v dotnet >/dev/null 2>&1 || \
   ! dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.AspNetCore.App ${DOTNET_CHANNEL}"; then
  die "ASP.NET Core ${DOTNET_CHANNEL} runtime missing. Run setup-idp-vps.sh first."
fi
if ! command -v caddy >/dev/null 2>&1; then
  die "Caddy not installed. Run setup-idp-vps.sh first."
fi

log "Postgres, dotnet ${DOTNET_CHANNEL} runtime, and Caddy are all present — good."

# ----- 2. Service user ------------------------------------------
if id -u "$APP_USER" >/dev/null 2>&1; then
  log "User '$APP_USER' already exists"
else
  log "Creating system user '$APP_USER' (no login)"
  useradd --system --no-create-home --shell /usr/sbin/nologin "$APP_USER"
fi

# ----- 3. Postgres role + DB -----------------------------------
if sudo -u postgres psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='$PG_USER'" | grep -q 1; then
  log "Postgres role '$PG_USER' already exists"
  PG_PASSWORD_GENERATED=""
else
  log "Creating Postgres role '$PG_USER' with random password"
  PG_PASSWORD_GENERATED="$(openssl rand -base64 48 | tr -d '/+=\n' | head -c 32)"
  sudo -u postgres psql <<SQL
CREATE ROLE "$PG_USER" WITH LOGIN PASSWORD '$PG_PASSWORD_GENERATED';
SQL
fi

if sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='$PG_DB'" | grep -q 1; then
  log "Postgres database '$PG_DB' already exists"
else
  log "Creating database '$PG_DB' owned by '$PG_USER'"
  sudo -u postgres psql <<SQL
CREATE DATABASE "$PG_DB" OWNER "$PG_USER" ENCODING 'UTF8' TEMPLATE template0;
SQL
fi

# ----- 4. Directories -------------------------------------------
log "Preparing $APP_DIR, $LOG_DIR, $DATA_DIR, $ENV_DIR"
mkdir -p "$APP_DIR" "$LOG_DIR" "$DATA_DIR" "$ENV_DIR"
chown -R "$APP_USER:$APP_USER" "$APP_DIR" "$LOG_DIR" "$DATA_DIR"
chown root:"$APP_USER"          "$ENV_DIR"
chmod 0750                      "$ENV_DIR"

# ----- 5. Env file (preserve existing password) ----------------
if [ -f "$ENV_FILE" ]; then
  log "Env file already exists at $ENV_FILE — leaving it alone"
else
  log "Writing $ENV_FILE"
  : "${PG_PASSWORD_GENERATED:?Password was not generated — role pre-existed but env file is missing. Reset role password manually or recreate it.}"
  cat > "$ENV_FILE" <<EOF
# Hmm.ServiceApi runtime environment (produced by setup-api-vps.sh)
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:${APP_PORT}
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Postgres (set by this script; rotate via psql + edit if needed)
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=${PG_DB};Username=${PG_USER};Password=${PG_PASSWORD_GENERATED}
AppSettings__DatabaseProvider=PostgreSQL
AppSettings__ConnectionString=Host=localhost;Port=5432;Database=${PG_DB};Username=${PG_USER};Password=${PG_PASSWORD_GENERATED}

# IDP integration
AppSettings__IdpBaseUrl=${IDP_BASE_URL}
EOF
  chown root:"$APP_USER" "$ENV_FILE"
  chmod 0640            "$ENV_FILE"
fi

# ----- 6. systemd unit ------------------------------------------
UNIT_FILE="/etc/systemd/system/hmm-api.service"
if [ -f "$UNIT_FILE" ]; then
  log "Existing systemd unit at $UNIT_FILE — leaving it alone"
else
  log "Writing systemd unit $UNIT_FILE"
  cat > "$UNIT_FILE" <<EOF
[Unit]
Description=Hmm Service API
After=network.target postgresql.service
Wants=postgresql.service

[Service]
Type=notify
WorkingDirectory=${APP_DIR}
ExecStart=/usr/bin/dotnet ${APP_DIR}/Hmm.ServiceApi.dll
Restart=always
RestartSec=10
SyslogIdentifier=hmm-api
User=${APP_USER}
Group=${APP_USER}
EnvironmentFile=${ENV_FILE}

# Hardening
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
ReadWritePaths=${LOG_DIR} ${DATA_DIR}
ProtectKernelTunables=true
ProtectKernelModules=true
ProtectControlGroups=true
LockPersonality=true
RestrictRealtime=true
RestrictNamespaces=true

[Install]
WantedBy=multi-user.target
EOF
fi

systemctl daemon-reload
# Deliberately NOT enabling/starting yet — binaries don't exist on first run.

# ----- 7. Caddy site block (append if missing) -----------------
if grep -q "$DOMAIN" "$CADDY_FILE" 2>/dev/null; then
  log "Caddyfile already references $DOMAIN — leaving it alone"
else
  log "Appending $DOMAIN site block to $CADDY_FILE"
  cat >> "$CADDY_FILE" <<EOF

$DOMAIN {
    encode gzip zstd
    reverse_proxy 127.0.0.1:${APP_PORT} {
        header_up X-Forwarded-Proto {scheme}
        header_up X-Forwarded-Host {host}
    }

    log {
        output file /var/log/caddy/api-access.log {
            roll_size 10mb
            roll_keep 5
        }
    }
}
EOF
  systemctl reload caddy
fi

# ----- 8. Summary ----------------------------------------------
cat <<EOF

============================================================
  VPS environment is ready for Hmm.ServiceApi
============================================================
  Domain       : https://${DOMAIN}
  App user     : ${APP_USER}
  App dir      : ${APP_DIR}
  Env file     : ${ENV_FILE}   (chmod 640, owned by root:${APP_USER})
  Log dir      : ${LOG_DIR}
  Data dir     : ${DATA_DIR}   (writable by ${APP_USER}; for SQLite-backed test
                                runs or asset uploads — Postgres is the prod store)
  Postgres     : localhost:5432  db=${PG_DB}  user=${PG_USER}
  App port     : 127.0.0.1:${APP_PORT}   (Caddy terminates TLS)
  Issuer (IDP) : ${IDP_BASE_URL}

  Services:
    hmm-api.service  inactive  (starts after first deploy)

  Next steps (from your workstation):
    1. Verify DNS:    dig +short ${DOMAIN}
    2. Deploy:        ./scripts/deploy-api.sh --deploy
                       (or scripts/deploy-api.ps1 -Deploy on Windows)
    3. EF Core will create the schema in '${PG_DB}' on first run
       (or run 'dotnet ef database update' against the connection string
        if migrations aren't configured to apply automatically).
============================================================
EOF
