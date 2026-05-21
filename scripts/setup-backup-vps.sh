#!/usr/bin/env bash
# ============================================================
# setup-backup-vps.sh — Provision nightly backup on the VPS
# ============================================================
#
# Creates:
#   - User/group: hmm-backup
#   - /opt/hmm-backup/hmm-backup.sh   (the script)
#   - /etc/hmm-backup.env             (PGPASSWORD + tunables, mode 0640)
#   - /var/backups/hmm                (output dir, owned by hmm-backup)
#   - /etc/systemd/system/hmm-backup.{service,timer}
#
# Idempotent: re-running updates the script + units, leaves
# existing backups + env file untouched. Run as root.
#
# Reads (with defaults):
#   PG_USER          Default postgres
#   PG_PASSWORD      No default — set in the env or this fails closed
#   IDP_PG_DB        Default HmmIdp
#   API_PG_DB        Default HmmNotes
#   VAULT_DIR        Default /var/lib/hmm-api/vault
#   RETAIN_DAYS      Default 14
#
# ============================================================

set -euo pipefail

if [[ $EUID -ne 0 ]]; then
    echo "ERROR: run as root (sudo)." >&2
    exit 1
fi

PG_USER="${PG_USER:-postgres}"
IDP_PG_DB="${IDP_PG_DB:-HmmIdp}"
API_PG_DB="${API_PG_DB:-HmmNotes}"
VAULT_DIR="${VAULT_DIR:-/var/lib/hmm-api/vault}"
RETAIN_DAYS="${RETAIN_DAYS:-14}"

SCRIPT_SRC_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INSTALL_DIR="/opt/hmm-backup"
BACKUP_DIR="/var/backups/hmm"
ENV_FILE="/etc/hmm-backup.env"
UNIT_DIR="/etc/systemd/system"

log() { printf '[setup-backup] %s\n' "$*"; }

# 1. User + group
if ! id hmm-backup &>/dev/null; then
    log "Creating service user hmm-backup"
    useradd --system --shell /usr/sbin/nologin --home /nonexistent --no-create-home hmm-backup
fi

# 2. Directories
log "Provisioning ${INSTALL_DIR} + ${BACKUP_DIR}"
mkdir -p "${INSTALL_DIR}" "${BACKUP_DIR}"
chown root:hmm-backup "${INSTALL_DIR}"
chown -R hmm-backup:hmm-backup "${BACKUP_DIR}"
chmod 0755 "${INSTALL_DIR}"
chmod 0750 "${BACKUP_DIR}"

# 3. Script
log "Installing hmm-backup.sh"
install -m 0755 -o root -g hmm-backup \
    "${SCRIPT_SRC_DIR}/hmm-backup.sh" \
    "${INSTALL_DIR}/hmm-backup.sh"

# 4. Env file (only if absent — never overwrite existing secrets)
if [[ ! -f "${ENV_FILE}" ]]; then
    log "Writing ${ENV_FILE} (FILL IN PG_PASSWORD BEFORE FIRST RUN)"
    cat > "${ENV_FILE}" <<EOF
# /etc/hmm-backup.env — sourced by hmm-backup.service
# Mode 0640, owned by root:hmm-backup.

PGPASSWORD=REPLACE_WITH_POSTGRES_PASSWORD_BEFORE_FIRST_BOOT
PG_HOST=127.0.0.1
PG_USER=${PG_USER}
IDP_PG_DB=${IDP_PG_DB}
API_PG_DB=${API_PG_DB}
VAULT_DIR=${VAULT_DIR}
BACKUP_DIR=${BACKUP_DIR}
RETAIN_DAYS=${RETAIN_DAYS}
EOF
    chown root:hmm-backup "${ENV_FILE}"
    chmod 0640 "${ENV_FILE}"
    log "ACTION REQUIRED: edit ${ENV_FILE} and set PGPASSWORD"
else
    log "${ENV_FILE} already present — leaving it alone."
fi

# 5. systemd units
log "Installing systemd units"
install -m 0644 -o root -g root \
    "${SCRIPT_SRC_DIR}/systemd/hmm-backup.service" \
    "${UNIT_DIR}/hmm-backup.service"
install -m 0644 -o root -g root \
    "${SCRIPT_SRC_DIR}/systemd/hmm-backup.timer" \
    "${UNIT_DIR}/hmm-backup.timer"

systemctl daemon-reload
systemctl enable hmm-backup.timer >/dev/null
systemctl start hmm-backup.timer

log "Done."
log ""
log "Next steps:"
log "  1. Set PGPASSWORD in ${ENV_FILE}"
log "  2. Test a manual run: systemctl start hmm-backup.service"
log "  3. Check the result : journalctl -u hmm-backup -e"
log "  4. Confirm timer    : systemctl list-timers hmm-backup.timer"
