# Production deploy — bare-metal VPS

Local-laptop Docker deploy (Cloudflare Tunnel + Docker compose) is
covered by `DEPLOYMENT_GUIDE.md`. **This** doc covers the
production target: an Oracle Cloud Ubuntu VPS running each .NET
service as a systemd unit, with system-package Postgres and Caddy
as a TLS-terminating reverse proxy (auto Let's Encrypt).

## Architecture

```
Flutter / browser
        |
   HTTPS (TLS terminated at Caddy via Let's Encrypt)
        |
  Caddy (systemd, ports 80/443)
        |
  127.0.0.1:8080  hmm-idp.service        ─┐
  127.0.0.1:8081  hmm-api.service        ─┤  Loopback-only listeners
  127.0.0.1:5432  postgresql.service     ─┘  (Caddy reverse-proxies)
        |
  /opt/hmm-{idp,api}        app binaries
  /var/log/hmm-{idp,api}    rolling JSON logs
  /var/lib/hmm-idp/dp-keys  DataProtection keyring
  /var/lib/hmm-api-data     SQLite fallback / staging
  /etc/hmm-{idp,api}        env files (mode 0640)
  /var/backups/hmm          nightly archives
```

The Docker compose tree under `docker/` is dev-only and not used
on the production VPS.

## One-time provisioning

Run on a fresh VPS as `root`:

```bash
sudo bash scripts/setup-idp-vps.sh
sudo bash scripts/setup-api-vps.sh
sudo bash scripts/setup-backup-vps.sh
```

What each script does is documented in its header comment. A
summary of the end state:

| Path | Owner | Contents |
| --- | --- | --- |
| `/opt/hmm-{idp,api}` | `hmm-{idp,api}` | published .NET app |
| `/etc/hmm-{idp,api}/{idp,api}.env` | `root:hmm-{idp,api}`, 0640 | secrets (DB password, admin bootstrap, IDP_SEED_TEST_USERS=false) |
| `/var/log/hmm-{idp,api}/app-*.json` | `hmm-{idp,api}` | structured JSON logs, 14-day rotation |
| `/var/lib/hmm-idp/dp-keys/` | `hmm-idp`, 0700 | ASP.NET DataProtection keys (signs Duende's signing-key rows) |
| `/var/lib/hmm-api/vault/` | `hmm-api` | attachment bytes |
| `/etc/systemd/system/hmm-{idp,api}.service` | `root` | systemd unit, hardened (`NoNewPrivileges`, `ProtectSystem=strict`, `ReadWritePaths=`) |
| `/etc/systemd/system/hmm-backup.{service,timer}` | `root` | nightly backup at 02:30 |
| `/var/backups/hmm/` | `hmm-backup`, 0750 | `HmmIdp-*.sql.gz`, `HmmNotes-*.sql.gz`, `hmm-vault-*.tar.gz` |

## Secrets — what to fill in before first boot

The setup scripts write env files with placeholder values that
**fail closed** — the service refuses to seed an admin user
without `IDP_INITIAL_ADMIN_PASSWORD`, and Postgres passwords are
generated randomly and embedded in the env file. Things to
double-check before starting the services:

```ini
# /etc/hmm-idp/idp.env
IDP_INITIAL_ADMIN_EMAIL=admin@example.com
IDP_INITIAL_ADMIN_PASSWORD=<strong, ≥12 chars, complex>
IDP_SEED_TEST_USERS=false                       # NEVER true in prod
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=HmmIdp;Username=hmm_idp;Password=<generated>

# /etc/hmm-api/api.env
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=HmmNotes;Username=hmm_api;Password=<generated>
AppSettings__IdpBaseUrl=https://idp.example.com

# /etc/hmm-backup.env
PGPASSWORD=<the postgres superuser password>
```

systemd's `EnvironmentFile=` honours these on every restart.
Nothing in the codebase falls back to a public default credential
— missing-env-var deploys fail loudly rather than silently
opening a default admin.

## Caddy (TLS + reverse proxy)

Installed and configured by `setup-idp-vps.sh` (the API script
appends its own site block). The Caddyfile at
`/etc/caddy/Caddyfile` looks like:

```
idp.homemademessage.com {
    encode gzip zstd
    reverse_proxy 127.0.0.1:8080 {
        header_up X-Forwarded-Proto {scheme}
        header_up X-Forwarded-Host {host}
    }
    log { output file /var/log/caddy/idp-access.log { roll_size 10mb roll_keep 5 } }
}

api.homemademessage.com {
    encode gzip zstd
    reverse_proxy 127.0.0.1:8081 {
        header_up X-Forwarded-Proto {scheme}
        header_up X-Forwarded-Host {host}
    }
    log { output file /var/log/caddy/api-access.log { roll_size 10mb roll_keep 5 } }
}
```

Caddy fetches a Let's Encrypt cert on first request for each
hostname — DNS must already resolve to the VPS before the first
443 hit, otherwise Let's Encrypt's HTTP-01 challenge fails.
Caddy retries on subsequent requests if DNS isn't ready yet.

The IDP and API listen on loopback only.
`app.UseForwardedHeaders()` in both services trusts loopback by
default — exactly the Caddy-on-the-same-host topology. See
`Startup.cs` / `HostingExtensions.cs`.

## Day-2 operations

| Task | Command |
| --- | --- |
| Tail live logs | `journalctl -u hmm-api -u hmm-idp -f` |
| Recent errors | `journalctl -u hmm-api --since today -p warning` |
| Stop / restart | `systemctl restart hmm-api hmm-idp` |
| Manual backup | `systemctl start hmm-backup.service` |
| Check backup schedule | `systemctl list-timers hmm-backup.timer` |
| Apply migrations | Auto — both services run `Database.Migrate()` on boot (see `AutomobileAppStartupFilter`). |
| Rotate IDP signing key | Duende's automatic key management handles this; manual rotation: delete rows from `IdentityServer.Keys` and restart. |
| Update app | `git pull && dotnet publish -c Release -o /opt/hmm-api/... && systemctl restart hmm-api` |

## Restore from backup

Pin in muscle memory: **Postgres first, vault second.**

```bash
# 1. Restore postgres (idempotent — DROP + recreate happens in the dump)
zcat /var/backups/hmm/HmmIdp-<ts>.sql.gz   | sudo -u postgres psql HmmIdp
zcat /var/backups/hmm/HmmNotes-<ts>.sql.gz | sudo -u postgres psql HmmNotes

# 2. THEN extract the vault tarball
sudo systemctl stop hmm-api
sudo -u hmm-api tar -C /var/lib/hmm-api/vault -xzf \
    /var/backups/hmm/hmm-vault-<ts>.tar.gz
sudo systemctl start hmm-api
```

Rationale: `Notes.attachments` rows in postgres reference vault
paths. Bytes without rows are orphans; rows without bytes show
placeholder UI until the vault arrives.

## What's in this hardening pass

Phase: pre-production hardening (commit anchor `<TBD>`).

| Item | Before | After |
| --- | --- | --- |
| `ForwardedHeaders` middleware | Implicit / env-var only | Explicit in code (`Startup.cs` / `HostingExtensions.cs`) for both services |
| Rate limiting | None | Per-IP 10/min on `/Account/Login`, `/connect/token`; 120/min default on IDP; 240/min on API |
| Production logging | Plain-text file sink, 14-day rotation | `CompactJsonFormatter` on Console (→ journald) + File (→ `/var/log/hmm-*/app-*.json`) with `Service` enricher + `FromLogContext` for trace correlation |
| Nightly backup | Manual `pg_dump` via Docker | systemd timer `hmm-backup.timer` → `hmm-backup.service` → `hmm-backup.sh`; 14-day retention; hardened unit |

## What's NOT in this pass — known deferrals

- **Subscription gating** (`RequireActiveSubscriptionAttribute`) — every authenticated user can read/write everything today
- **`Devices` entity** — `MigrationLog.DeviceIdentifier` is a free-form string; multi-device audit is approximate
- **CI/CD** — `git pull && systemctl restart` is fine for a one-box deploy; if/when you want a pipeline, see GitHub Actions
- **Off-box log shipping** — local journald + 14-day file archive is enough for one VPS; Loki/Better Stack when you want dashboards or multi-host correlation
- **HTTPS at the app** — Caddy terminates TLS; the app sees plain HTTP from `127.0.0.1`. If you ever move off Caddy (e.g. to Cloudflare Tunnel or nginx), make sure the replacement still trusts loopback as a proxy and forwards `X-Forwarded-Proto` / `-Host`, otherwise the IDP discovery doc will start advertising `http://` again.
