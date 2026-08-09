#!/usr/bin/env bash
# ============================================================
# hmm-release.sh — one front door for deploying Hmm to every target,
# with a backup gate and a real rollback path.
#
# Targets span two repos:
#   iphone / ipad / android → hmm_console (Flutter client)
#   vps / docker            → Hmm         (.NET backend)
#
# This script does NOT reimplement the existing deploy scripts. It
# wraps them and adds the two things they lack:
#
#   1. A backup that GATES the deploy. If --backup-first is given and
#      the backup fails, the deploy does not run. No silent continue.
#   2. A restore point captured BEFORE the change, so --rollback has
#      something real to go back to.
#
# Why (2) matters: scripts/deploy-api.sh does
#   sudo rsync -a --delete "$STAGE/" "$REMOTE_DIR/"
# which destroys the previous release in place. Before this script,
# "roll back the API" meant "git checkout the old commit and rebuild".
# Here we snapshot $REMOTE_DIR first, so rollback is an extract.
#
# Usage:
#   ./hmm-release.sh --target vps    --deploy --backup-first
#   ./hmm-release.sh --target iphone --deploy --backup-first
#   ./hmm-release.sh --target ipad   --deploy
#   ./hmm-release.sh --target vps    --rollback            # latest
#   ./hmm-release.sh --target vps    --rollback 20260809T101500Z
#   ./hmm-release.sh --target docker --backup
#   ./hmm-release.sh --target iphone --list
#
# Actions (exactly one):
#   --deploy            build + install/push
#   --backup            back up only, deploy nothing
#   --rollback [ID]     restore a prior restore point (default: newest)
#   --list              show restore points for the target
#   --status            show current state of the target
#
# Flags:
#   --backup-first      back up, then deploy. Backup failure ABORTS.
#   --service api|idp|both   (vps only, default: api)
#   --allow-no-data-backup   proceed on a target whose app data cannot
#                            be pulled (see ANDROID CAVEAT below)
#   --yes, -y           skip the confirmation prompt
#   --dry-run           print what would happen, change nothing
#
# Environment:
#   HMM_RELEASE_STORE   local restore points (default ~/.hmm-releases)
#   CONSOLE_DIR         Flutter repo (default ~/projects/hmm_console)
#   API_VPS_HOST/USER/SSH_KEY   inherited by the wrapped deploy-*.sh
#   VPS_RELEASE_DIR     remote snapshots (default /var/backups/hmm-releases)
#
# ------------------------------------------------------------
# ANDROID CAVEAT — read before trusting --backup on android.
# A release-signed APK is not debuggable, so `adb run-as` cannot read
# its private data, and `adb backup` is a no-op on Android 12+ (Google
# removed it). There is NO reliable unrooted way to pull this app's
# local database off an Android device. This script therefore treats
# android app-data backup as UNAVAILABLE and refuses to deploy under
# --backup-first unless you pass --allow-no-data-backup. The real
# safety net on mobile is OneDrive sync, not a local dump.
#
# iOS CAN be pulled (devicectl reaches the app data container for
# development-signed apps), so iphone/ipad backup is implemented.
#
# ------------------------------------------------------------
# THE INSTALL-IN-PLACE RULE (learned the hard way, 2026-08)
# `flutter install` UNINSTALLS before installing, which wipes the iOS
# container and every unsynced note in it. This script only ever uses
#   xcrun devicectl device install app    (iOS — installs in place)
#   adb install -r                        (Android — replaces, keeps data)
# Do not "simplify" either into `flutter install`.
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HMM_DIR="$(dirname "$SCRIPT_DIR")"
CONSOLE_DIR="${CONSOLE_DIR:-$HOME/projects/hmm_console}"

RELEASE_STORE="${HMM_RELEASE_STORE:-$HOME/.hmm-releases}"
VPS_RELEASE_DIR="${VPS_RELEASE_DIR:-/var/backups/hmm-releases}"

VPS_HOST="${API_VPS_HOST:-132.145.102.175}"
VPS_USER="${API_VPS_USER:-ubuntu}"
SSH_KEY="${API_SSH_KEY:-$HOME/.ssh/20220830-2236.key}"
SSH_OPTS=(-i "$SSH_KEY" -o StrictHostKeyChecking=accept-new)

IOS_BUNDLE_ID="${IOS_BUNDLE_ID:-com.pivotpointsol.hmmConsole}"
ANDROID_APP_ID="${ANDROID_APP_ID:-com.pivotpointsol.hmm_console}"

TS="$(date -u +%Y%m%dT%H%M%SZ)"

# ----- Args ------------------------------------------------------
TARGET=""
ACTION=""
ROLLBACK_ID=""
BACKUP_FIRST=0
VPS_SERVICE="api"
ALLOW_NO_DATA_BACKUP=0
ASSUME_YES=0
DRY_RUN=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --target)   TARGET="${2:-}"; shift ;;
    --deploy)   ACTION="deploy" ;;
    --backup)   ACTION="backup" ;;
    --rollback) ACTION="rollback"
                [[ "${2:-}" =~ ^[0-9]{8}T ]] && { ROLLBACK_ID="$2"; shift; } ;;
    --list)     ACTION="list" ;;
    --status)   ACTION="status" ;;
    --backup-first) BACKUP_FIRST=1 ;;
    --service)  VPS_SERVICE="${2:-}"; shift ;;
    --allow-no-data-backup) ALLOW_NO_DATA_BACKUP=1 ;;
    --yes|-y)   ASSUME_YES=1 ;;
    --dry-run)  DRY_RUN=1 ;;
    --help|-h)  sed -n '2,70p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 0 ;;
    *) echo "Unknown option: $1 (use --help)" >&2; exit 1 ;;
  esac
  shift
done

# ----- Helpers ---------------------------------------------------
banner() { echo "============================================================"; echo "$1"; echo "============================================================"; }
die()    { echo "ERROR: $*" >&2; exit 1; }
note()   { echo "  $*"; }

run() {
  if [[ "$DRY_RUN" -eq 1 ]]; then echo "  [dry-run] $*"; return 0; fi
  "$@"
}

confirm() {
  [[ "$ASSUME_YES" -eq 1 || "$DRY_RUN" -eq 1 ]] && return 0
  local prompt="$1"
  read -r -p "$prompt [y/N] " reply
  [[ "$reply" =~ ^[Yy]$ ]] || die "aborted by user"
}

store_dir() { echo "$RELEASE_STORE/$1"; }

# Newest restore point id for a target, or empty.
latest_id() {
  local d; d="$(store_dir "$1")"
  [[ -d "$d" ]] || return 0
  ls -1 "$d" 2>/dev/null | sort | tail -1
}

require_ssh() {
  # --dry-run still performs read-only probes (is the backup script
  # installed? does the snapshot exist?) because a dry run that reports
  # "would back up" when backup is impossible is worse than useless.
  # It just never asserts reachability, so it degrades to a plan
  # printout when the box is down rather than hard-failing.
  [[ "$DRY_RUN" -eq 1 ]] && return 0
  [[ -f "$SSH_KEY" ]] || die "SSH key not found at $SSH_KEY"
  ssh "${SSH_OPTS[@]}" -o ConnectTimeout=10 "$VPS_USER@$VPS_HOST" true 2>/dev/null \
    || die "cannot SSH to $VPS_USER@$VPS_HOST (ssh-add the key if it has a passphrase)"
}

# Callers use this inside $(...), where a die() would exit only the
# subshell and let the parent carry on with an empty loop — a deploy
# that reports success having done nothing. So this function never
# validates; validate_service() below runs once up front instead.
vps_units() {
  case "$VPS_SERVICE" in
    api)  echo "hmm-api" ;;
    idp)  echo "hmm-idp" ;;
    both) echo "hmm-api hmm-idp" ;;
  esac
}

validate_service() {
  case "$VPS_SERVICE" in
    api|idp|both) ;;
    *) die "--service must be api, idp or both (got '$VPS_SERVICE')" ;;
  esac
}

# ============================================================
# VPS
# ============================================================

# Runs the backup script already installed on the box by
# setup-backup-vps.sh. Loud-fails if it isn't there — a missing
# backup script must never read as "backup succeeded".
vps_backup() {
  require_ssh
  banner "VPS data backup (Postgres x2 + attachment vault)"
  local remote="/opt/hmm-backup/hmm-backup.sh"
  ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" "test -x $remote" \
    || die "$remote not found on the VPS. Run scripts/setup-backup-vps.sh first."
  if [[ "$DRY_RUN" -eq 1 ]]; then echo "  [dry-run] ssh … sudo $remote"; return 0; fi
  # The script sources /etc/hmm-backup.env for PGPASSWORD; run it the
  # same way the systemd timer does so behaviour matches production.
  ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" \
    "sudo systemd-run --wait --collect --quiet --unit=hmm-backup-adhoc-$TS \
       --property=EnvironmentFile=/etc/hmm-backup.env $remote" \
    || die "VPS backup FAILED — not proceeding."
  note "backup complete (see /var/backups/hmm on the box)"
}

# Snapshot the live release directory so --rollback has a target.
# deploy-api.sh rsync --delete's over this dir, so it must run BEFORE.
vps_snapshot() {
  require_ssh
  banner "Snapshotting current release(s) for rollback"
  for unit in $(vps_units); do
    local dir="/opt/$unit"
    local out="$VPS_RELEASE_DIR/$unit-$TS.tar.gz"
    if [[ "$DRY_RUN" -eq 1 ]]; then echo "  [dry-run] snapshot $dir → $out"; continue; fi
    ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" bash <<EOF || die "snapshot of $dir failed"
set -euo pipefail
sudo mkdir -p '$VPS_RELEASE_DIR'
if [ ! -d '$dir' ] || [ -z "\$(sudo ls -A '$dir' 2>/dev/null)" ]; then
  echo "  $dir empty or missing — nothing to snapshot (first deploy?)"
  exit 0
fi
sudo tar -C '$dir' -czf '$out' .
sudo ls -lh '$out'
EOF
    note "snapshot: $out"
  done
  # Keep the local ledger so --list works without SSH round-trips.
  mkdir -p "$(store_dir "vps")/$TS"
  printf 'services=%s\nhost=%s\nremote_dir=%s\n' \
    "$VPS_SERVICE" "$VPS_HOST" "$VPS_RELEASE_DIR" > "$(store_dir "vps")/$TS/manifest"
}

vps_deploy() {
  banner "Deploying backend to VPS ($VPS_SERVICE)"
  for unit in $(vps_units); do
    local script="$SCRIPT_DIR/deploy-${unit#hmm-}.sh"
    [[ -x "$script" ]] || die "missing $script"
    if [[ "$DRY_RUN" -eq 1 ]]; then echo "  [dry-run] $script --deploy"; continue; fi
    "$script" --deploy || die "$script failed — roll back with: $0 --target vps --rollback"
  done
}

vps_rollback() {
  require_ssh
  local id="${ROLLBACK_ID:-$(latest_id vps)}"
  [[ -n "$id" ]] || die "no restore point found. Run --list, or check $VPS_RELEASE_DIR on the box."
  banner "Rolling back VPS ($VPS_SERVICE) to $id"
  confirm "This REPLACES the running binaries in /opt with the $id snapshot. Continue?"
  for unit in $(vps_units); do
    local dir="/opt/$unit"
    local snap="$VPS_RELEASE_DIR/$unit-$id.tar.gz"
    if [[ "$DRY_RUN" -eq 1 ]]; then echo "  [dry-run] restore $snap → $dir"; continue; fi
    ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" bash <<EOF || die "rollback of $unit failed"
set -euo pipefail
# Guard: only ever touch /opt/hmm-* — never a path typo'd into root.
case '$dir' in /opt/hmm-*) ;; *) echo "refusing to clear '$dir'"; exit 1 ;; esac
[ -f '$snap' ] || { echo "snapshot not found: $snap"; exit 1; }
sudo systemctl stop $unit || true
sudo find '$dir' -mindepth 1 -delete
sudo tar -C '$dir' -xzf '$snap'
sudo chown -R $unit:$unit '$dir'
sudo systemctl start $unit
sleep 2
sudo systemctl is-active $unit
EOF
    note "$unit rolled back to $id"
  done
  echo ""
  note "Verify: $SCRIPT_DIR/deploy-${VPS_SERVICE}.sh --logs"
}

vps_list() {
  require_ssh
  banner "VPS restore points ($VPS_RELEASE_DIR)"
  ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" \
    "sudo ls -lh $VPS_RELEASE_DIR 2>/dev/null || echo '  (none yet)'"
  echo ""
  banner "VPS data backups (/var/backups/hmm)"
  ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" \
    "sudo ls -lh /var/backups/hmm 2>/dev/null | tail -12 || echo '  (none yet)'"
}

vps_status() {
  require_ssh
  for unit in $(vps_units); do
    ssh "${SSH_OPTS[@]}" "$VPS_USER@$VPS_HOST" \
      "sudo systemctl status $unit --no-pager -l | head -12"
    echo ""
  done
}

# ============================================================
# Docker (local dev stack)
# ============================================================

docker_backup() {
  banner "Docker stack backup"
  local script="$HMM_DIR/docker/hmm-deploy.sh"
  [[ -x "$script" ]] || die "missing $script"
  local dest; dest="$(store_dir docker)/$TS"
  if [[ "$DRY_RUN" -eq 1 ]]; then echo "  [dry-run] HMM_BACKUP_DIR=$dest $script --backup"; return 0; fi
  mkdir -p "$dest"
  HMM_BACKUP_DIR="$dest" "$script" --backup || die "docker backup FAILED — not proceeding."
  # A backup dir with no dumps in it is a failed backup wearing a hat.
  compgen -G "$dest/*.sql" >/dev/null || die "docker backup produced no SQL dumps (is the stack running?)"
  note "restore point: $dest"
}

docker_deploy() {
  banner "Rebuilding + restarting local Docker stack"
  local script="$HMM_DIR/docker/hmm-deploy.sh"
  [[ -x "$script" ]] || die "missing $script"
  run "$script" --start --build || die "docker deploy failed"
}

# Docker rollback restores DATA, not images — images are rebuilt from
# source. Order is load-bearing: Postgres first (it holds the
# attachments JSON that references vault paths), vault second.
docker_rollback() {
  local id="${ROLLBACK_ID:-$(latest_id docker)}"
  [[ -n "$id" ]] || die "no docker restore point. Run --target docker --backup first."
  local src; src="$(store_dir docker)/$id"
  [[ -d "$src" ]] || die "restore point not found: $src"
  banner "Restoring Docker stack data from $id"
  confirm "This OVERWRITES the HmmNotes + HmmIdp databases in the running containers. Continue?"
  if [[ "$DRY_RUN" -eq 1 ]]; then echo "  [dry-run] restore $src (Postgres, then vault)"; return 0; fi

  docker ps --format '{{.Names}}' | grep -q hmm-api || die "hmm-api container not running"

  local notes idp vault
  notes="$(ls -1 "$src"/HmmNotes-*.sql 2>/dev/null | tail -1 || true)"
  idp="$(ls -1 "$src"/HmmIdp-*.sql 2>/dev/null | tail -1 || true)"
  vault="$(ls -1 "$src"/hmm-vault-*.tar.gz 2>/dev/null | tail -1 || true)"

  # 1. Postgres FIRST.
  [[ -n "$notes" ]] && { note "restoring HmmNotes"; docker exec -i hmm-api su postgres -c "psql -h 127.0.0.1 HmmNotes" < "$notes" >/dev/null || die "HmmNotes restore failed"; }
  [[ -n "$idp"   ]] && { note "restoring HmmIdp";   docker exec -i hmm-idp su postgres -c "psql -h 127.0.0.1 HmmIdp"   < "$idp"   >/dev/null || die "HmmIdp restore failed"; }
  # 2. Vault SECOND.
  [[ -n "$vault" ]] && { note "restoring vault";    docker exec -i hmm-api tar -C /var/lib/hmm-vault -xzf - < "$vault" || die "vault restore failed"; }

  note "restored. Restart the stack so EF picks up the schema: $HMM_DIR/docker/hmm-deploy.sh --start"
}

docker_list()   { banner "Docker restore points"; ls -1 "$(store_dir docker)" 2>/dev/null || echo "  (none yet)"; }
docker_status() { "$HMM_DIR/docker/hmm-deploy.sh" --status; }

# ============================================================
# iOS (iphone / ipad)
# ============================================================

# Resolve a paired device of the requested class. devicectl quirks
# (documented at length in deploy-prod-ios-device.sh): --json-output -
# does not go to stdout, and tunnelState is "disconnected" for a
# healthy idle device — gate on pairingState only.
ios_device_id() {
  local want_type="$1"   # iPhone | iPad
  local json; json="$(mktemp /tmp/devicectl-XXXXXX.json)"
  xcrun devicectl list devices --json-output "$json" >/dev/null 2>&1 || true
  local id
  id="$(python3 -c "
import json,sys
try:
    d=json.load(open('$json'))
except Exception:
    sys.exit(1)
for dev in d.get('result',{}).get('devices',[]):
    c=dev.get('connectionProperties',{}) or {}
    h=dev.get('hardwareProperties',{}) or {}
    if c.get('pairingState')=='paired' and h.get('deviceType')=='$want_type':
        print(dev['identifier']); sys.exit(0)
sys.exit(1)
" 2>/dev/null || true)"
  rm -f "$json"
  [[ -n "$id" ]] || die "no paired $want_type found. Cable in, device unlocked, 'Trust' accepted? Try: xcrun devicectl list devices"
  echo "$id"
}

ios_backup() {
  local want_type="$1" device; device="$(ios_device_id "$want_type")"
  banner "Pulling app data container off $want_type ($device)"
  local dest; dest="$(store_dir "${TARGET}")/$TS/appdata"
  if [[ "$DRY_RUN" -eq 1 ]]; then echo "  [dry-run] devicectl copy appDataContainer → $dest"; return 0; fi
  mkdir -p "$dest"
  # Reaches the sandbox because the app is development-signed. Fails on
  # a distribution-signed build — which is exactly when you want to know.
  xcrun devicectl device copy from \
      --device "$device" \
      --domain-type appDataContainer \
      --domain-identifier "$IOS_BUNDLE_ID" \
      --source . \
      --destination "$dest" 2>&1 \
    || die "could not pull app data from the $want_type.
       The app must be development-signed and installed for this to work.
       Your notes also live in OneDrive — but do not treat that as verified
       unless you have actually seen them there. Re-run with
       --allow-no-data-backup only if you accept losing local-only changes."
  note "app data → $dest"
  du -sh "$dest" 2>/dev/null || true
}

ios_deploy() {
  local want_type="$1"
  [[ -d "$CONSOLE_DIR" ]] || die "console repo not found at $CONSOLE_DIR"
  banner "Building + installing release build on $want_type"

  local script="$CONSOLE_DIR/scripts/deploy-prod-ios-device.sh"
  if [[ "$want_type" == "iPhone" && -x "$script" ]]; then
    # iPhone has a working, battle-tested script. Reuse it verbatim.
    run "$script" || die "iOS deploy failed"
  else
    # iPad path: same build, different device filter.
    local device; device="$(ios_device_id "$want_type")"
    if [[ "$DRY_RUN" -eq 1 ]]; then echo "  [dry-run] flutter build ios --release && devicectl install → $device"; return 0; fi
    ( cd "$CONSOLE_DIR" && flutter build ios --release \
        --dart-define=API_ENV=production \
        --dart-define=ONEDRIVE_CLIENT_ID="${ONEDRIVE_CLIENT_ID:-3056e225-6965-4c36-8542-db02f614e084}" ) \
      || die "flutter build ios failed"
    # install (not `flutter install`) — replaces in place, keeps the container.
    xcrun devicectl device install app --device "$device" \
      "$CONSOLE_DIR/build/ios/iphoneos/Runner.app" || die "devicectl install failed"
  fi

  # Archive the .app so --rollback can reinstall this exact build.
  local keep; keep="$(store_dir "${TARGET}")/$TS"
  if [[ "$DRY_RUN" -eq 0 && -d "$CONSOLE_DIR/build/ios/iphoneos/Runner.app" ]]; then
    mkdir -p "$keep"
    cp -R "$CONSOLE_DIR/build/ios/iphoneos/Runner.app" "$keep/" 2>/dev/null || true
    ( cd "$CONSOLE_DIR" && git rev-parse HEAD 2>/dev/null > "$keep/commit" ) || true
    note "archived build → $keep"
  fi
}

ios_rollback() {
  local want_type="$1"
  local id="${ROLLBACK_ID:-$(latest_id "$TARGET")}"
  [[ -n "$id" ]] || die "no archived $TARGET build to roll back to."
  local app; app="$(store_dir "$TARGET")/$id/Runner.app"
  [[ -d "$app" ]] || die "no Runner.app archived under $id (that restore point may be data-only)"
  local device; device="$(ios_device_id "$want_type")"
  banner "Reinstalling archived build $id on $want_type"
  [[ -f "$(store_dir "$TARGET")/$id/commit" ]] && note "commit: $(cat "$(store_dir "$TARGET")/$id/commit")"
  confirm "Reinstall the $id build over what is on the device?"
  run xcrun devicectl device install app --device "$device" "$app" || die "reinstall failed"
  note "rolled back. App data is untouched — install replaces the binary in place."
}

ios_list()   { banner "$TARGET restore points"; ls -1 "$(store_dir "$TARGET")" 2>/dev/null || echo "  (none yet)"; }
ios_status() { xcrun devicectl list devices 2>/dev/null | head -20; }

# ============================================================
# Android
# ============================================================

ANDROID_HOME="${ANDROID_HOME:-$HOME/Library/Android/sdk}"
ADB="$ANDROID_HOME/platform-tools/adb"

android_device() {
  [[ -x "$ADB" ]] || die "adb not found at $ADB (set ANDROID_HOME)"
  local d
  d="$("$ADB" devices 2>/dev/null | awk '/^[A-Za-z0-9].*\tdevice$/ && $1 !~ /^emulator-/ { print $1; exit }' || true)"
  [[ -n "$d" ]] || die "no physical Android device. USB debugging on, prompt accepted? Try: $ADB devices"
  echo "$d"
}

# See ANDROID CAVEAT in the header. This does not pretend to succeed.
android_backup() {
  banner "Android app-data backup"
  echo "  UNAVAILABLE on this app."
  echo "  The release APK is not debuggable, so 'adb run-as' cannot read its"
  echo "  data, and 'adb backup' was removed in Android 12+. There is no"
  echo "  unrooted way to pull the local DB."
  echo ""
  echo "  What protects your data here is OneDrive sync, not this script."
  if [[ "$ALLOW_NO_DATA_BACKUP" -eq 1 ]]; then
    note "--allow-no-data-backup given: continuing without a local copy."
    return 0
  fi
  die "refusing to continue. Sync the device to OneDrive and confirm your notes
       are in the cloud, then re-run with --allow-no-data-backup."
}

android_deploy() {
  [[ -d "$CONSOLE_DIR" ]] || die "console repo not found at $CONSOLE_DIR"
  local script="$CONSOLE_DIR/scripts/deploy-prod-android-device.sh"
  [[ -x "$script" ]] || die "missing $script"
  banner "Building + installing release APK on Android"
  run "$script" || die "android deploy failed"

  local apk="$CONSOLE_DIR/build/app/outputs/flutter-apk/app-release.apk"
  if [[ "$DRY_RUN" -eq 0 && -f "$apk" ]]; then
    local keep; keep="$(store_dir android)/$TS"
    mkdir -p "$keep"
    cp "$apk" "$keep/"
    ( cd "$CONSOLE_DIR" && git rev-parse HEAD 2>/dev/null > "$keep/commit" ) || true
    note "archived APK → $keep"
  fi
}

android_rollback() {
  local id="${ROLLBACK_ID:-$(latest_id android)}"
  [[ -n "$id" ]] || die "no archived APK to roll back to."
  local apk; apk="$(store_dir android)/$id/app-release.apk"
  [[ -f "$apk" ]] || die "no APK archived under $id"
  local device; device="$(android_device)"
  banner "Reinstalling archived APK $id"
  [[ -f "$(store_dir android)/$id/commit" ]] && note "commit: $(cat "$(store_dir android)/$id/commit")"
  echo ""
  echo "  NOTE: adb refuses a downgrade if the archived build has a lower"
  echo "  versionCode. If this fails with INSTALL_FAILED_VERSION_DOWNGRADE,"
  echo "  the only way back is uninstall + reinstall — WHICH WIPES APP DATA."
  echo "  That is a data-loss operation; this script will not do it for you."
  confirm "Attempt in-place reinstall of $id?"
  run "$ADB" -s "$device" install -r -t "$apk" || die "reinstall failed (see note above)"
  note "rolled back."
}

android_list()   { banner "Android restore points"; ls -1 "$(store_dir android)" 2>/dev/null || echo "  (none yet)"; }
android_status() { "$ADB" devices -l 2>/dev/null || true; }

# ============================================================
# Dispatch
# ============================================================
[[ -n "$TARGET" ]] || die "no --target (iphone|ipad|android|vps|docker). Use --help."
[[ -n "$ACTION" ]] || die "no action (--deploy|--backup|--rollback|--list|--status). Use --help."

case "$TARGET" in
  iphone|ipad|android|docker) ;;
  vps) validate_service ;;
  *) die "unknown --target '$TARGET' (iphone|ipad|android|vps|docker)" ;;
esac

echo ""
banner "hmm-release  target=$TARGET  action=$ACTION  id=$TS$([[ $DRY_RUN -eq 1 ]] && echo '  [DRY RUN]')"
echo ""

# --backup-first is the gate: any failure inside the backup path exits
# non-zero via die(), so the deploy below is never reached.
if [[ "$ACTION" == "deploy" && "$BACKUP_FIRST" -eq 1 ]]; then
  case "$TARGET" in
    vps)     vps_backup ;;
    docker)  docker_backup ;;
    iphone)  ios_backup iPhone ;;
    ipad)    ios_backup iPad ;;
    android) android_backup ;;
  esac
  echo ""
fi

case "$TARGET:$ACTION" in
  vps:deploy)      vps_snapshot; echo ""; vps_deploy ;;
  vps:backup)      vps_backup; vps_snapshot ;;
  vps:rollback)    vps_rollback ;;
  vps:list)        vps_list ;;
  vps:status)      vps_status ;;

  docker:deploy)   docker_deploy ;;
  docker:backup)   docker_backup ;;
  docker:rollback) docker_rollback ;;
  docker:list)     docker_list ;;
  docker:status)   docker_status ;;

  iphone:deploy)   ios_deploy iPhone ;;
  iphone:backup)   ios_backup iPhone ;;
  iphone:rollback) ios_rollback iPhone ;;
  iphone:list)     ios_list ;;
  iphone:status)   ios_status ;;

  ipad:deploy)     ios_deploy iPad ;;
  ipad:backup)     ios_backup iPad ;;
  ipad:rollback)   ios_rollback iPad ;;
  ipad:list)       ios_list ;;
  ipad:status)     ios_status ;;

  android:deploy)   android_deploy ;;
  android:backup)   android_backup ;;
  android:rollback) android_rollback ;;
  android:list)     android_list ;;
  android:status)   android_status ;;
esac

echo ""
banner "Done — $TARGET/$ACTION"

# Only advertise a rollback that actually exists. Every deploy path
# that captures a restore point creates $RELEASE_STORE/<target>/<TS>;
# docker --deploy without --backup-first deliberately captures nothing,
# and printing an id for it would promise a recovery that isn't there.
if [[ "$ACTION" == "deploy" ]]; then
  if [[ "$DRY_RUN" -eq 1 ]]; then
    echo "  [dry-run] no restore point created."
  elif [[ -d "$(store_dir "$TARGET")/$TS" ]]; then
    echo "  Roll back with: $0 --target $TARGET --rollback $TS"
  else
    echo "  NO restore point was captured for this deploy."
    echo "  Re-run with --backup-first next time if you want one."
  fi
fi
exit 0
