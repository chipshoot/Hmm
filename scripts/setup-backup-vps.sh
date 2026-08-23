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

# Default to the dedicated read-only role this script provisions (step 0),
# NOT `postgres`. See the note there for why no pre-existing credential works.
PG_USER="${PG_USER:-hmm_backup}"
IDP_PG_DB="${IDP_PG_DB:-HmmIdp}"
API_PG_DB="${API_PG_DB:-HmmNotes}"
RETAIN_DAYS="${RETAIN_DAYS:-14}"

# The vault path is read from the API's own config rather than guessed. The
# previous hard-coded default (/var/lib/hmm-api/vault) does not exist on the
# production box — the real path is /var/lib/hmm-api-data/vault — and since
# `tar` of a missing directory simply fails, the effect was a backup that
# silently contained no attachments. Ask the service where it actually writes.
if [[ -z "${VAULT_DIR:-}" ]]; then
    VAULT_DIR="$(grep -m1 '^AttachmentSettings__RootDir=' /etc/hmm-api/api.env 2>/dev/null | cut -d= -f2- || true)"
fi
VAULT_DIR="${VAULT_DIR:-/var/lib/hmm-api-data/vault}"

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

# 1b. Vault read access.
#
# The vault is drwxr-x--- hmm-api:hmm-api, so hmm-backup cannot read it and the
# tar step fails with "Cannot stat: Permission denied". Group membership is
# enough — deliberately no chmod and no chown on application data.
if getent group hmm-api >/dev/null && ! id -nG hmm-backup | grep -qw hmm-api; then
    log "Adding hmm-backup to the hmm-api group (vault read access)"
    usermod -aG hmm-api hmm-backup
fi

# 1c. A Postgres role that can actually dump both databases.
#
# There is no pre-existing credential that works. HmmIdp is owned by hmm_idp
# and HmmNotes by hmm_api — two roles, two passwords — while `postgres`
# authenticates by *peer* on the local socket and so has no TCP password at
# all. hmm-backup.sh dumps both databases as ONE user over TCP, so it needs a
# role of its own.
#
# pg_read_all_data (PG14+) is exactly a backup's privilege: SELECT on
# everything, write nothing. The role is NOT a superuser and cannot create
# databases or roles. Its password is generated here and written only into
# ${ENV_FILE} — it is never echoed.
if [[ "${PG_USER}" == "hmm_backup" ]]; then
    if sudo -u postgres psql -tAc \
         "SELECT 1 FROM pg_roles WHERE rolname='hmm_backup'" 2>/dev/null | grep -q 1; then
        log "Postgres role hmm_backup already exists — leaving its password alone."
        BACKUP_ROLE_PW=""
    else
        log "Creating read-only Postgres role hmm_backup"
        # Over-generate: stripping non-alphanumerics from base64 loses
        # characters, so 24 bytes cannot reliably yield 32 usable ones.
        BACKUP_ROLE_PW="$(openssl rand -base64 64 | tr -dc 'A-Za-z0-9' | head -c 32)"
        [[ ${#BACKUP_ROLE_PW} -eq 32 ]] || { echo "ERROR: password generation failed" >&2; exit 1; }
        sudo -u postgres psql -v ON_ERROR_STOP=1 -q <<SQL
CREATE ROLE hmm_backup LOGIN PASSWORD '${BACKUP_ROLE_PW}';
GRANT pg_read_all_data TO hmm_backup;
GRANT CONNECT ON DATABASE "${IDP_PG_DB}" TO hmm_backup;
GRANT CONNECT ON DATABASE "${API_PG_DB}" TO hmm_backup;
SQL
    fi
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
    # When this script minted the role above, wire its password straight in.
    # The old behaviour — a REPLACE_ME placeholder plus "ACTION REQUIRED" —
    # meant a fresh install always failed its first run, and on a box where
    # nobody noticed, produced a timer that had never once succeeded.
    PW_LINE="${BACKUP_ROLE_PW:-REPLACE_WITH_PG_PASSWORD_BEFORE_FIRST_RUN}"
    log "Writing ${ENV_FILE}"
    cat > "${ENV_FILE}" <<EOF
# /etc/hmm-backup.env — sourced by hmm-backup.service
# Mode 0640, owned by root:hmm-backup.

PGPASSWORD=${PW_LINE}
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
    if [[ -z "${BACKUP_ROLE_PW:-}" ]]; then
        log "ACTION REQUIRED: edit ${ENV_FILE} and set PGPASSWORD"
    fi
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
log "  1. Test a manual run: systemctl start hmm-backup.service"
log "  2. Check the result : journalctl -u hmm-backup -e"
log "  3. Confirm timer    : systemctl list-timers hmm-backup.timer"
log ""
log "  Verify by CONTENTS, not exit code. Check the .sql.gz files are the size"
log "  you would expect for the data you have, and that the vault tarball is"
log "  not an empty 112-byte archive unless the vault really is empty."
log "  Note pg_stat_user_tables.n_live_tup can read 0 on a populated table"
log "  (stale statistics) — use COUNT(*) if a dump looks suspiciously small."
