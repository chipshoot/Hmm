#!/usr/bin/env bash
# ============================================================
# deploy-idp.sh — publish Hmm.Idp locally and push it to the
# Oracle VPS as a native systemd service.
#
# Pairs with: scripts/setup-idp-vps.sh (one-time VPS bootstrap)
#
# Usage:
#   ./deploy-idp.sh --deploy            # publish + upload + restart
#   ./deploy-idp.sh --deploy --no-publish   # skip publish, just upload
#   ./deploy-idp.sh --publish           # build only (no upload)
#   ./deploy-idp.sh --restart           # restart service on VPS
#   ./deploy-idp.sh --status            # remote systemctl status
#   ./deploy-idp.sh --logs              # follow remote journalctl
#   ./deploy-idp.sh --ssh               # interactive SSH
#
# Override defaults via environment:
#   IDP_VPS_HOST    (default: 132.145.102.175)
#   IDP_VPS_USER    (default: ubuntu)
#   IDP_SSH_KEY     (default: ~/.ssh/20220830-2236.key)
#   IDP_REMOTE_DIR  (default: /opt/hmm-idp)
#   IDP_SRC         (default: ../src/Hmm.Idp relative to this script)
#   IDP_PUBLISH_OUT (default: $TMPDIR/hmm-idp-publish)
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ----- Defaults --------------------------------------------------
VPS_HOST="${IDP_VPS_HOST:-132.145.102.175}"
VPS_USER="${IDP_VPS_USER:-ubuntu}"
SSH_KEY="${IDP_SSH_KEY:-$HOME/.ssh/20220830-2236.key}"
REMOTE_DIR="${IDP_REMOTE_DIR:-/opt/hmm-idp}"
REMOTE_STAGE="${IDP_REMOTE_STAGE:-/tmp/hmm-idp-staging}"
APP_USER="${IDP_APP_USER:-hmm-idp}"
SERVICE="${IDP_SERVICE:-hmm-idp}"
SRC_DIR="${IDP_SRC:-$SCRIPT_DIR/../src/Hmm.Idp}"
PUBLISH_OUT="${IDP_PUBLISH_OUT:-${TMPDIR:-/tmp}/hmm-idp-publish}"

SSH_OPTS=(-i "$SSH_KEY" -o StrictHostKeyChecking=accept-new)
RSYNC_RSH="ssh ${SSH_OPTS[*]}"

# ----- Args ------------------------------------------------------
ACTION=""
DO_PUBLISH=1

while [[ $# -gt 0 ]]; do
  case "$1" in
    --deploy)     ACTION="deploy" ;;
    --publish)    ACTION="publish" ;;
    --restart)    ACTION="restart" ;;
    --status)     ACTION="status" ;;
    --logs)       ACTION="logs" ;;
    --ssh)        ACTION="ssh" ;;
    --no-publish) DO_PUBLISH=0 ;;
    --help|-h)
      cat <<'HELP'
deploy-idp.sh — publish Hmm.Idp locally and push it to the Oracle VPS
as a native systemd service.

Pairs with: scripts/setup-idp-vps.sh (one-time VPS bootstrap)

Actions:
  --deploy [--no-publish]   publish + upload + restart
  --publish                 dotnet publish only (no upload)
  --restart                 restart hmm-idp service on VPS
  --status                  remote systemctl status
  --logs                    follow remote journalctl
  --ssh                     interactive SSH

Override defaults via environment:
  IDP_VPS_HOST    (default: 132.145.102.175)
  IDP_VPS_USER    (default: ubuntu)
  IDP_SSH_KEY     (default: ~/.ssh/20220830-2236.key)
  IDP_REMOTE_DIR  (default: /opt/hmm-idp)
  IDP_SRC         (default: ../src/Hmm.Idp relative to this script)
  IDP_PUBLISH_OUT (default: $TMPDIR/hmm-idp-publish)
HELP
      exit 0 ;;
    *) echo "Unknown option: $1 (use --help)"; exit 1 ;;
  esac
  shift
done
[[ -z "$ACTION" ]] && { echo "ERROR: no action (use --help)"; exit 1; }

# ----- Helpers ---------------------------------------------------
banner() {
  echo "============================================================"
  echo "$1"
  echo "============================================================"
}

ensure_local_tools() {
  command -v dotnet >/dev/null || { echo "ERROR: dotnet not on PATH"; exit 1; }
  command -v rsync  >/dev/null || { echo "ERROR: rsync not on PATH"; exit 1; }
  command -v ssh    >/dev/null || { echo "ERROR: ssh not on PATH"; exit 1; }
  [[ -f "$SSH_KEY" ]] || { echo "ERROR: SSH key not found at $SSH_KEY"; exit 1; }
}

ensure_remote_reachable() {
  if ! ssh "${SSH_OPTS[@]}" -o ConnectTimeout=10 "$VPS_USER@$VPS_HOST" 'true' 2>/dev/null; then
    echo "ERROR: cannot SSH to $VPS_USER@$VPS_HOST with $SSH_KEY"
    echo "       (ssh-add the key if it has a passphrase)"
    exit 1
  fi
}

publish_local() {
  banner "Publishing Hmm.Idp (Release, framework-dependent)"
  echo "  source: $SRC_DIR"
  echo "  output: $PUBLISH_OUT"
  rm -rf "$PUBLISH_OUT"
  mkdir -p "$PUBLISH_OUT"
  # No explicit .csproj — dotnet finds the single project in $SRC_DIR.
  # This keeps the script template-clean so deploy-api.sh is a near-pure copy
  # with renamed defaults at the top.
  ( cd "$SRC_DIR" && dotnet publish -c Release -o "$PUBLISH_OUT" --nologo )
  echo ""
  echo "  $(find "$PUBLISH_OUT" -type f | wc -l | tr -d ' ') files, $(du -sh "$PUBLISH_OUT" | cut -f1)"
}

upload_and_install() {
  banner "Uploading to $VPS_USER@$VPS_HOST:$REMOTE_DIR"
  ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" "mkdir -p '$REMOTE_STAGE'"

  rsync -avz --delete \
    --exclude '*.pdb' --exclude 'appsettings.Development.json' \
    -e "$RSYNC_RSH" \
    "$PUBLISH_OUT/" "$VPS_USER@$VPS_HOST:$REMOTE_STAGE/"

  banner "Atomic swap into $REMOTE_DIR + restart $SERVICE"
  ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" bash <<EOF
set -euo pipefail
sudo systemctl stop $SERVICE 2>/dev/null || true
sudo rsync -a --delete '$REMOTE_STAGE/' '$REMOTE_DIR/'
sudo chown -R $APP_USER:$APP_USER '$REMOTE_DIR'
sudo systemctl enable $SERVICE
sudo systemctl start $SERVICE
sleep 2
sudo systemctl is-active $SERVICE
EOF
}

# ----- Actions ---------------------------------------------------
case "$ACTION" in
  publish)
    ensure_local_tools
    publish_local
    ;;

  deploy)
    ensure_local_tools
    ensure_remote_reachable
    [[ "$DO_PUBLISH" -eq 1 ]] && publish_local
    [[ -d "$PUBLISH_OUT" ]] || { echo "ERROR: $PUBLISH_OUT missing — run --publish first"; exit 1; }
    upload_and_install
    banner "Done"
    echo "  Tail logs:    ./deploy-idp.sh --logs"
    echo "  Verify OIDC:  curl -s https://idp.homemademessage.com/.well-known/openid-configuration | jq .issuer"
    ;;

  restart)
    ensure_remote_reachable
    ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" "sudo systemctl restart $SERVICE && sudo systemctl is-active $SERVICE"
    ;;

  status)
    ensure_remote_reachable
    ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" "sudo systemctl status $SERVICE --no-pager -l | head -40"
    ;;

  logs)
    ensure_remote_reachable
    ssh "${SSH_OPTS[@]}" -t "$VPS_USER@$VPS_HOST" "sudo journalctl -u $SERVICE -f --no-pager"
    ;;

  ssh)
    exec ssh "${SSH_OPTS[@]}" -t "$VPS_USER@$VPS_HOST"
    ;;
esac
