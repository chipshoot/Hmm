# Production Deployment Guide

Deploy the HomeMadeMessage API and Identity Provider on a local MacBook using Docker containers with Cloudflare Tunnel for public HTTPS access.

## Architecture

```
Flutter App / Browser
        |
   HTTPS (TLS)
        |
  Cloudflare Edge
        |
  Encrypted tunnel (outbound from Mac)
        |
  cloudflared (macOS service)
        |
    Docker containers on MacBook
    +--------------------------+
    | hmm-api     :5010        |  <-- SQLite DB on OneDrive
    | hmm-idp     :5001        |  <-- SQL Server (idp-sqlserver)
    | idp-sqlserver :14333     |
    | hmm-seq     :5341/8081   |
    +--------------------------+
```

## Prerequisites

- macOS with Docker Desktop installed
- A domain name added to Cloudflare (e.g., `homemademessage.com`)
- Cloudflare account (free tier is sufficient)
- OneDrive installed and syncing on the Mac

## Step 1: Clone and Configure

```bash
git clone https://github.com/chipshoot/Hmm.git
cd Hmm
```

### Configure environment variables

Edit `docker/.env` to set secure passwords for production:

```bash
# SQL Server (IDP database)
IDP_SA_PASSWORD=YourSecurePassword123!
IDP_SQLSERVER_PORT=14333

# Seq Logging
SEQ_INGESTION_PORT=5341
SEQ_UI_PORT=8081

# IDP
IDP_PORT=5001

# API
API_HTTP_PORT=5010
```

### Configure SQLite database path

Edit `docker/compose.api-sqlite.yml` to point the volume to your OneDrive folder:

```yaml
volumes:
  - ~/Library/CloudStorage/OneDrive-Personal/hmm-data:/app/data
```

Create the directory if it doesn't exist:

```bash
mkdir -p ~/Library/CloudStorage/OneDrive-Personal/hmm-data
```

## Step 2: Deploy Docker Containers

### Using the deploy script

```bash
# First deployment (builds images)
./docker/hmm-deploy.sh --start --build

# Subsequent starts (uses existing images)
./docker/hmm-deploy.sh --start

# Stop all containers
./docker/hmm-deploy.sh --stop

# View logs
./docker/hmm-deploy.sh --logs

# Rebuild from scratch (after code changes)
./docker/hmm-deploy.sh --start --rebuild

# Check health status
./docker/hmm-deploy.sh --status
```

### Manual deployment

```bash
cd docker

# Start production stack
docker compose -f compose.base-sqlite.yml -f compose.idp.yml -f compose.api-sqlite.yml up -d --build

# Verify containers are running
docker ps

# Check API logs
docker logs hmm-api

# Stop everything
docker compose -f compose.base-sqlite.yml -f compose.idp.yml -f compose.api-sqlite.yml down
```

### Verify services

| Service | URL | Expected |
|---------|-----|----------|
| API Swagger | http://localhost:5010/swagger | Swagger UI |
| API endpoint | http://localhost:5010/api/v1/authors | 401 Unauthorized |
| IDP | http://localhost:5001 | IdentityServer page |
| Seq dashboard | http://localhost:8081 | Seq log viewer |

## Step 3: Set Up Cloudflare Tunnel

### Install cloudflared

```bash
brew install cloudflared
```

### Authenticate with Cloudflare

```bash
cloudflared tunnel login
```

This opens a browser to authorize your Cloudflare account.

### Create tunnel

```bash
cloudflared tunnel create hmm-prod
```

Note the tunnel ID from the output (e.g., `a1b2c3d4-...`).

### Configure tunnel routes

Create `~/.cloudflared/config.yml`:

```yaml
tunnel: hmm-prod
credentials-file: /Users/<your-username>/.cloudflared/<tunnel-id>.json

ingress:
  - hostname: api.homemademessage.com
    service: http://localhost:5010
  - hostname: auth.homemademessage.com
    service: http://localhost:5001
  - service: http_status:404
```

### Add DNS records

```bash
cloudflared tunnel route dns hmm-prod api.homemademessage.com
cloudflared tunnel route dns hmm-prod auth.homemademessage.com
```

### Test the tunnel

```bash
# Run in foreground first to verify
cloudflared tunnel run hmm-prod
```

From another terminal or your phone, verify:
```bash
curl https://api.homemademessage.com/swagger/v1/swagger.json
```

### Install as macOS service (auto-start on boot)

```bash
sudo cloudflared service install
```

This creates a LaunchDaemon at `/Library/LaunchDaemons/com.cloudflare.cloudflared.plist` that starts the tunnel automatically on boot.

To uninstall: `sudo cloudflared service uninstall`

## Step 4: Prevent Mac from Sleeping

```bash
# Prevent sleep when on power adapter (display can sleep after 10 min)
sudo pmset -c sleep 0 displaysleep 10 disksleep 0
```

Or go to **System Settings > Energy Saver** and enable "Prevent automatic sleeping when the display is off".

## Step 5: Update API Configuration for Production

Update the IDP base URL in `compose.api-sqlite.yml` environment to match your Cloudflare domain if the Flutter app authenticates via the public URL:

```yaml
environment:
  - AppSettings__IdpBaseUrl=https://auth.homemademessage.com
```

Or keep `http://hmm-idp:80` if the API validates tokens internally within the Docker network (recommended - avoids external roundtrip).

## Updating the Application

After code changes on your Windows development PC:

```bash
# On Windows: push changes
git add . && git commit -m "your changes" && git push

# On Mac: pull and redeploy
cd ~/Hmm
git pull
./docker/hmm-deploy.sh --start --rebuild
```

The Cloudflare Tunnel automatically reconnects - no tunnel reconfiguration needed.

## Running Development/Test Alongside Production

Production and test stacks use different ports and container names - they can run simultaneously:

| | Production | Test |
|---|-----------|------|
| Compose files | `compose.base-sqlite.yml` + `compose.idp.yml` + `compose.api-sqlite.yml` | `docker-compose.test.yml` |
| API port | 5010 | 8080 |
| IDP port | 5001 | N/A |
| DB | SQLite (OneDrive) | SQL Server (:14330) |
| Container prefix | `hmm-` | `hmm-*-test` |
| Cloudflare | Connected | Not connected |

Start the test stack:
```bash
docker compose -f docker/docker-compose.test.yml up -d
```

## Backup and Recovery

### SQLite database

The database file at `~/Library/CloudStorage/OneDrive-Personal/hmm-data/hmm.db` syncs automatically via OneDrive. For manual backup:

```bash
# Stop API before backing up (ensures WAL is checkpointed)
docker stop hmm-api
cp ~/Library/CloudStorage/OneDrive-Personal/hmm-data/hmm.db ~/backups/hmm-$(date +%Y%m%d).db
docker start hmm-api
```

### IDP SQL Server database

```bash
# Backup via Docker
docker exec hmm-idp-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$IDP_SA_PASSWORD" -C \
  -Q "BACKUP DATABASE [HmmIdp] TO DISK = '/var/opt/mssql/backup/HmmIdp.bak'"

# Copy backup to host
docker cp hmm-idp-sqlserver:/var/opt/mssql/backup/HmmIdp.bak ~/backups/
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| API can't write to SQLite | Check OneDrive folder permissions: `chmod 777 ~/Library/CloudStorage/OneDrive-Personal/hmm-data` |
| Tunnel not connecting | Check `cloudflared tunnel info hmm-prod` and verify DNS records |
| Containers won't start | Check logs: `docker logs hmm-api` or `docker logs hmm-idp` |
| Mac went to sleep | Verify power settings: `pmset -g` |
| Port conflict | Check: `lsof -i :5010` and stop conflicting process |
| SQLite locked | Ensure only one API instance accesses the `.db` file |
| Docker not starting on boot | Enable Docker Desktop: Settings > General > "Start Docker Desktop when you sign in" |
