#!/usr/bin/env bash
# ============================================================
# setup-idp-vps.sh — one-shot environment bootstrap for Hmm.Idp
# on an Oracle Cloud Ubuntu 22.04 VPS (ARM Ampere or x86).
#
# Prepares:
#   - System packages (curl, gnupg, iptables-persistent)
#   - Host firewall rules for ports 80 and 443
#   - hmm-idp system user (no login shell)
#   - PostgreSQL 16 with dedicated hmm_idp role + HmmIdp DB
#   - ASP.NET Core 10 runtime (apt first, dotnet-install.sh fallback
#     for preview channels)
#   - Caddy with a reverse-proxy Caddyfile for idp.homemademessage.com
#   - /opt/hmm-idp, /var/log/hmm-idp, /etc/hmm-idp/idp.env
#   - /etc/systemd/system/hmm-idp.service (registered, NOT started —
#     start only after first code deploy)
#
# Run:
#   curl -fsSL https://example/path/setup-idp-vps.sh | sudo bash
#   # or
#   scp setup-idp-vps.sh ubuntu@<vps>:~
#   ssh ubuntu@<vps>
#   sudo bash ~/setup-idp-vps.sh
#
# Idempotent: safe to re-run. Existing users, DBs and env files are
# preserved; only missing pieces are created.
# ============================================================
set -euo pipefail

# ----- Tunables --------------------------------------------------
DOMAIN="${DOMAIN:-idp.homemademessage.com}"
APP_USER="${APP_USER:-hmm-idp}"
APP_DIR="${APP_DIR:-/opt/hmm-idp}"
LOG_DIR="${LOG_DIR:-/var/log/hmm-idp}"
ENV_DIR="${ENV_DIR:-/etc/hmm-idp}"
ENV_FILE="${ENV_DIR}/idp.env"
APP_PORT="${APP_PORT:-8080}"              # IDP listens on 127.0.0.1:${APP_PORT}
PG_DB="${PG_DB:-HmmIdp}"
PG_USER="${PG_USER:-hmm_idp}"
PG_VERSION_WANTED="${PG_VERSION_WANTED:-16}"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-10.0}"  # override to 9.0 etc. if needed

# ----- Helpers ---------------------------------------------------
log()  { printf '\n\033[1;34m[setup]\033[0m %s\n' "$*"; }
warn() { printf '\n\033[1;33m[warn ]\033[0m %s\n' "$*"; }
die()  { printf '\n\033[1;31m[fail ]\033[0m %s\n' "$*" >&2; exit 1; }

require_root() {
  if [ "$(id -u)" -ne 0 ]; then
    die "Run with sudo or as root."
  fi
}

ubuntu_codename() {
  . /etc/os-release
  echo "${VERSION_CODENAME:-jammy}"
}

# ----- Preconditions --------------------------------------------
require_root

if ! grep -q 'Ubuntu' /etc/os-release 2>/dev/null; then
  warn "This script targets Ubuntu. Continuing anyway."
fi

export DEBIAN_FRONTEND=noninteractive

# ----- 1. System update + basics --------------------------------
log "Updating apt and installing base packages"
apt-get update -y
apt-get upgrade -y
apt-get install -y \
  ca-certificates curl gnupg lsb-release \
  iptables-persistent netfilter-persistent \
  openssl acl

# ----- 2. Host firewall (Oracle-style iptables) -----------------
log "Opening host firewall for 80 and 443"
# Oracle's Ubuntu image uses a pre-populated INPUT chain; insert at a
# position before the default REJECT so new connections are accepted.
ensure_iptables_rule() {
  local port="$1"
  if ! iptables -C INPUT -m state --state NEW -p tcp --dport "$port" -j ACCEPT 2>/dev/null; then
    iptables -I INPUT 6 -m state --state NEW -p tcp --dport "$port" -j ACCEPT
    log "  added iptables ACCEPT for tcp/$port"
  else
    log "  iptables ACCEPT for tcp/$port already present"
  fi
}
ensure_iptables_rule 80
ensure_iptables_rule 443
netfilter-persistent save

warn "Reminder: also open tcp/80 and tcp/443 in the OCI VCN security list."

# ----- 3. Service user ------------------------------------------
if id -u "$APP_USER" >/dev/null 2>&1; then
  log "User '$APP_USER' already exists"
else
  log "Creating system user '$APP_USER' (no login)"
  useradd --system --no-create-home --shell /usr/sbin/nologin "$APP_USER"
fi

# ----- 4. PostgreSQL --------------------------------------------
log "Installing PostgreSQL $PG_VERSION_WANTED"
apt-get install -y "postgresql-$PG_VERSION_WANTED" "postgresql-client-$PG_VERSION_WANTED" || \
  apt-get install -y postgresql postgresql-client   # fallback to distro default

systemctl enable --now postgresql

# Confirm it's responding
for i in $(seq 1 30); do
  if sudo -u postgres psql -tAc 'SELECT 1' >/dev/null 2>&1; then break; fi
  sleep 1
done
sudo -u postgres psql -tAc 'SELECT 1' >/dev/null 2>&1 || die "PostgreSQL did not come up"

# Role
if sudo -u postgres psql -tAc "SELECT 1 FROM pg_roles WHERE rolname='$PG_USER'" | grep -q 1; then
  log "Postgres role '$PG_USER' already exists"
  PG_PASSWORD_EXISTING="keep"
else
  log "Creating Postgres role '$PG_USER' with a random password"
  PG_PASSWORD_GENERATED="$(openssl rand -base64 48 | tr -d '/+=\n' | head -c 32)"
  sudo -u postgres psql <<SQL
CREATE ROLE "$PG_USER" WITH LOGIN PASSWORD '$PG_PASSWORD_GENERATED';
SQL
fi

# Database
if sudo -u postgres psql -tAc "SELECT 1 FROM pg_database WHERE datname='$PG_DB'" | grep -q 1; then
  log "Postgres database '$PG_DB' already exists"
else
  log "Creating database '$PG_DB' owned by '$PG_USER'"
  sudo -u postgres psql <<SQL
CREATE DATABASE "$PG_DB" OWNER "$PG_USER" ENCODING 'UTF8' TEMPLATE template0;
SQL
fi

# ----- 5. App directories ---------------------------------------
log "Preparing $APP_DIR, $LOG_DIR, $ENV_DIR"
mkdir -p "$APP_DIR" "$LOG_DIR" "$ENV_DIR"
chown -R "$APP_USER:$APP_USER" "$APP_DIR" "$LOG_DIR"
chown root:"$APP_USER"         "$ENV_DIR"
chmod 0750                     "$ENV_DIR"

# ----- 6. Env file (preserve existing password) -----------------
if [ -f "$ENV_FILE" ]; then
  log "Env file already exists at $ENV_FILE — leaving it alone"
else
  log "Writing $ENV_FILE with freshly-generated postgres password"
  : "${PG_PASSWORD_GENERATED:?Password was not generated — role must have pre-existed without env file}"
  cat > "$ENV_FILE" <<EOF
# Hmm.Idp runtime environment (produced by setup-idp-vps.sh)
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:${APP_PORT}
# Honor X-Forwarded-Proto / X-Forwarded-Host from Caddy so OIDC
# discovery URLs come back as https://${DOMAIN}.
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=${PG_DB};Username=${PG_USER};Password=${PG_PASSWORD_GENERATED}
IssuerUri=https://${DOMAIN}

# ---- SMTP (verification, password-reset, lockout notifications) -----
# Fill in the AspHostPortal mailbox credentials after creating
# accounts@homemademessage.com in their control panel. Until SmtpServer is
# set to a real host, registration will succeed but the email send will fail
# (logged, not fatal — user can use /Account/ResendConfirmation later).
#
# Typical AspHostPortal values (verify in their cPanel/Plesk mail panel):
#   EmailSettings__SmtpServer  = mail.homemademessage.com
#   EmailSettings__SmtpPort    = 587  (STARTTLS) — try 465 if 587 is blocked
#   EmailSettings__UseSsl      = true
#   EmailSettings__Username    = accounts@homemademessage.com
#   EmailSettings__Password    = <mailbox password>
#
# Don't forget SPF / DKIM / DMARC at AspHostPortal DNS — without them
# Gmail and Outlook will junk verification mail.
EmailSettings__SmtpServer=SET_VIA_ENV_VAR
EmailSettings__SmtpPort=587
EmailSettings__UseSsl=true
EmailSettings__Username=accounts@${DOMAIN#idp.}
EmailSettings__Password=SET_VIA_ENV_VAR
EmailSettings__SenderEmail=accounts@${DOMAIN#idp.}
EmailSettings__SenderName=HomeMadeMessage
EmailSettings__ApplicationUrl=https://${DOMAIN}
EOF
  chown root:"$APP_USER" "$ENV_FILE"
  chmod 0640            "$ENV_FILE"
fi

# ----- 7. .NET runtime ------------------------------------------
install_dotnet_via_apt() {
  local codename
  codename="$(ubuntu_codename)"
  log "Registering Microsoft packages feed (ubuntu/$codename)"
  curl -sSL "https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb" \
    -o /tmp/packages-microsoft-prod.deb
  dpkg -i /tmp/packages-microsoft-prod.deb
  rm /tmp/packages-microsoft-prod.deb
  apt-get update -y
  if apt-cache show "aspnetcore-runtime-${DOTNET_CHANNEL}" >/dev/null 2>&1; then
    apt-get install -y "aspnetcore-runtime-${DOTNET_CHANNEL}"
    return 0
  fi
  return 1
}

install_dotnet_via_script() {
  log "Falling back to dotnet-install.sh (preview channel)"
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  /tmp/dotnet-install.sh \
    --channel "$DOTNET_CHANNEL" \
    --runtime aspnetcore \
    --quality preview \
    --install-dir /usr/share/dotnet
  ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
  rm /tmp/dotnet-install.sh
}

if command -v dotnet >/dev/null 2>&1 && dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.AspNetCore.App ${DOTNET_CHANNEL}"; then
  log "ASP.NET Core ${DOTNET_CHANNEL} runtime already installed"
else
  log "Installing ASP.NET Core ${DOTNET_CHANNEL} runtime"
  if ! install_dotnet_via_apt; then
    install_dotnet_via_script
  fi
fi

dotnet --list-runtimes || die "dotnet command not available after install"

# ----- 8. Caddy --------------------------------------------------
if command -v caddy >/dev/null 2>&1; then
  log "Caddy already installed"
else
  log "Installing Caddy"
  apt-get install -y debian-keyring debian-archive-keyring apt-transport-https
  curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | \
    gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
  curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | \
    tee /etc/apt/sources.list.d/caddy-stable.list >/dev/null
  apt-get update -y
  apt-get install -y caddy
fi

CADDY_FILE="/etc/caddy/Caddyfile"
if grep -q "$DOMAIN" "$CADDY_FILE" 2>/dev/null; then
  log "Caddyfile already contains $DOMAIN — leaving it alone"
else
  log "Writing $CADDY_FILE for $DOMAIN"
  cat > "$CADDY_FILE" <<EOF
$DOMAIN {
    encode gzip zstd
    reverse_proxy 127.0.0.1:${APP_PORT} {
        header_up X-Forwarded-Proto {scheme}
        header_up X-Forwarded-Host {host}
    }

    log {
        output file /var/log/caddy/idp-access.log {
            roll_size 10mb
            roll_keep 5
        }
    }
}
EOF
fi

systemctl enable caddy
systemctl restart caddy

# ----- 9. systemd unit ------------------------------------------
UNIT_FILE="/etc/systemd/system/hmm-idp.service"
if [ -f "$UNIT_FILE" ]; then
  log "Existing systemd unit at $UNIT_FILE — leaving it alone"
else
  log "Writing systemd unit $UNIT_FILE"
  cat > "$UNIT_FILE" <<EOF
[Unit]
Description=Hmm Identity Provider
After=network.target postgresql.service
Wants=postgresql.service

[Service]
Type=notify
WorkingDirectory=${APP_DIR}
ExecStart=/usr/bin/dotnet ${APP_DIR}/Hmm.Idp.dll
Restart=always
RestartSec=10
SyslogIdentifier=hmm-idp
User=${APP_USER}
Group=${APP_USER}
EnvironmentFile=${ENV_FILE}

# Hardening
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true
ReadWritePaths=${LOG_DIR}
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

# If /usr/bin/dotnet is a symlink to /usr/local/bin (install-script
# path), systemd can still follow it.
if [ ! -x /usr/bin/dotnet ] && [ -x /usr/local/bin/dotnet ]; then
  ln -sf /usr/local/bin/dotnet /usr/bin/dotnet
fi

systemctl daemon-reload
# Deliberately NOT enabling/starting yet — the binaries don't exist.

# ----- 10. Summary ----------------------------------------------
cat <<EOF

============================================================
  VPS environment is ready for Hmm.Idp
============================================================
  Domain       : https://${DOMAIN}
  App user     : ${APP_USER}
  App dir      : ${APP_DIR}
  Env file     : ${ENV_FILE}   (chmod 640, owned by root:${APP_USER})
  Log dir      : ${LOG_DIR}
  Postgres     : localhost:5432  db=${PG_DB}  user=${PG_USER}
  App port     : 127.0.0.1:${APP_PORT}   (Caddy terminates TLS)

  Services:
    caddy            $(systemctl is-active caddy)   (enabled)
    postgresql       $(systemctl is-active postgresql)
    hmm-idp.service  inactive  (starts after first deploy)

  Next steps (run from your workstation):
    1. Verify DNS:    dig +short ${DOMAIN}   # should return this VPS IP
    2. Publish locally:
         cd ~/projects/hmm/src/Hmm.Idp
         dotnet publish -c Release -o /tmp/hmm-idp-publish
    3. rsync to VPS:
         rsync -avz --delete /tmp/hmm-idp-publish/ \\
           $(whoami 2>/dev/null || echo ubuntu)@<vps-ip>:${APP_DIR}/
         ssh <vps> "sudo chown -R ${APP_USER}:${APP_USER} ${APP_DIR}"
    4. First start (on VPS):
         sudo systemctl enable --now hmm-idp
         sudo journalctl -u hmm-idp -f
    5. Verify:
         curl -s https://${DOMAIN}/.well-known/openid-configuration | jq .issuer

  Reminders:
    * OCI VCN security list must allow tcp/80 and tcp/443 inbound.
    * Caddy will auto-fetch a Let's Encrypt cert on first request —
      DNS must resolve to this box before that succeeds.
    * Postgres password is in ${ENV_FILE}; back it up off-box.
    * SMTP creds in ${ENV_FILE} are placeholders — edit
      EmailSettings__SmtpServer / __Password before users register, then
      \`sudo systemctl restart hmm-idp\`. SPF/DKIM/DMARC at AspHostPortal
      DNS are required for inbox delivery.
============================================================
EOF
