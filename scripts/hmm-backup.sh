#!/usr/bin/env bash
# ============================================================
# hmm-backup.sh — Production backup for the bare-metal VPS deploy
# ============================================================
#
# Designed to be invoked by the hmm-backup.timer systemd unit (or
# a cron entry). Produces three artifacts per run, timestamped to
# the second, under $BACKUP_DIR:
#
#   HmmIdp-<ts>.sql.gz       — IDP postgres dump (gzipped)
#   HmmNotes-<ts>.sql.gz     — API  postgres dump (gzipped)
#   hmm-vault-<ts>.tar.gz    — Attachment vault tarball
#
# Then retains the most recent $RETAIN_DAYS days of each (default
# 14), prunes older ones. Empty / failed dumps are deleted so the
# retention window never holds half-written archives.
#
# Restore order (CRITICAL — non-symmetric with backup order):
#   1. Restore the Postgres dumps FIRST
#        psql -h 127.0.0.1 -U postgres HmmNotes < HmmNotes-<ts>.sql
#        psql -h 127.0.0.1 -U postgres HmmIdp   < HmmIdp-<ts>.sql
#   2. THEN extract the vault tarball into the vault root
#        tar -C /var/lib/hmm-api/vault -xzf hmm-vault-<ts>.tar.gz
#
# Postgres holds the Notes.attachments JSON that references vault
# paths. Bytes without rows are orphans; rows without bytes show
# placeholder UI until the vault arrives — both states are
# user-visible. Pin restore order in the runbook.
#
# Environment overrides (see also /etc/hmm-backup.env, which the
# timer's EnvironmentFile= clause sources before this runs):
#   BACKUP_DIR       Default /var/backups/hmm
#   VAULT_DIR        Default /var/lib/hmm-api/vault
#   IDP_PG_DB        Default HmmIdp
#   API_PG_DB        Default HmmNotes
#   PG_HOST          Default 127.0.0.1
#   PG_USER          Default postgres
#   RETAIN_DAYS      Default 14
#
# ============================================================

set -euo pipefail

BACKUP_DIR="${BACKUP_DIR:-/var/backups/hmm}"
VAULT_DIR="${VAULT_DIR:-/var/lib/hmm-api/vault}"
IDP_PG_DB="${IDP_PG_DB:-HmmIdp}"
API_PG_DB="${API_PG_DB:-HmmNotes}"
PG_HOST="${PG_HOST:-127.0.0.1}"
PG_USER="${PG_USER:-postgres}"
RETAIN_DAYS="${RETAIN_DAYS:-14}"

# PGPASSWORD is sourced from /etc/hmm-backup.env (chmod 0640,
# owned by root:hmm-backup) — never echoed. If absent we rely on
# ~/.pgpass / peer auth; loud-fail rather than hang.
if [[ -z "${PGPASSWORD:-}" && ! -f "${HOME}/.pgpass" ]]; then
    echo "ERROR: PGPASSWORD unset and no ~/.pgpass — refusing to hang on a TTY prompt." >&2
    exit 2
fi

mkdir -p "${BACKUP_DIR}"
TS="$(date -u +%Y%m%dT%H%M%SZ)"

backup_db() {
    local db="$1"
    local out="${BACKUP_DIR}/${db}-${TS}.sql.gz"
    if pg_dump -h "${PG_HOST}" -U "${PG_USER}" "${db}" 2>>"${BACKUP_DIR}/.last-error.log" \
        | gzip -9 > "${out}"; then
        # pg_dump exits 0 even when the DB doesn't exist (it just
        # prints a header). Drop archives smaller than 100 bytes
        # so the retention window stays clean.
        if [[ $(stat -c%s "${out}") -lt 100 ]]; then
            echo "WARN: ${out} is suspiciously small — removing." >&2
            rm -f "${out}"
            return 1
        fi
        echo "  ✓ ${out}"
    else
        echo "ERROR: pg_dump ${db} failed; see ${BACKUP_DIR}/.last-error.log" >&2
        rm -f "${out}"
        return 1
    fi
}

backup_vault() {
    local out="${BACKUP_DIR}/hmm-vault-${TS}.tar.gz"
    if [[ ! -d "${VAULT_DIR}" ]]; then
        echo "WARN: ${VAULT_DIR} does not exist; skipping vault backup."
        return 0
    fi
    # -C into the vault root so paths inside the tarball are
    # relative (portable to a different mount point on restore).
    # Empty vault still produces a valid empty tar — symmetric
    # with the docker-side backup.
    if tar -C "${VAULT_DIR}" -czf "${out}" .; then
        echo "  ✓ ${out}"
    else
        echo "ERROR: vault tar failed." >&2
        rm -f "${out}"
        return 1
    fi
}

prune_old() {
    # Keep the most recent $RETAIN_DAYS days. find -mtime is
    # date-of-modification; archives are written once and never
    # touched after, so mtime ≈ creation time. Pruning by glob
    # rather than wholesale `find -delete` so we never touch
    # unrelated files even if BACKUP_DIR is shared.
    find "${BACKUP_DIR}" -maxdepth 1 -type f \
        \( -name 'HmmIdp-*.sql.gz' \
        -o -name 'HmmNotes-*.sql.gz' \
        -o -name 'hmm-vault-*.tar.gz' \) \
        -mtime "+${RETAIN_DAYS}" -print -delete
}

echo "============================================================"
echo "hmm-backup ${TS}"
echo "  dir       : ${BACKUP_DIR}"
echo "  retain    : ${RETAIN_DAYS} days"
echo "============================================================"

failed=0
backup_db "${IDP_PG_DB}" || failed=1
backup_db "${API_PG_DB}" || failed=1
backup_vault             || failed=1

echo "Pruning archives older than ${RETAIN_DAYS} days..."
prune_old

echo "Summary:"
ls -lh "${BACKUP_DIR}"/*"${TS}"* 2>/dev/null || true
echo "============================================================"

exit "${failed}"
