# Functional Testing Environment Guide

This guide explains how to set up and use the functional testing environments for the Hmm.ServiceApi project.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Environment 1: Local IDE + Docker Infrastructure](#environment-1-local-ide--docker-infrastructure)
- [Environment 2: Full Docker Stack](#environment-2-full-docker-stack)
- [Database Initialization](#database-initialization)
- [Authentication Setup](#authentication-setup)
- [Getting Test Tokens](#getting-test-tokens)
- [Using the HTTP Test File](#using-the-http-test-file)
- [Viewing Logs in Seq](#viewing-logs-in-seq)
- [Troubleshooting](#troubleshooting)
- [Configuration Reference](#configuration-reference)

---

## Overview

Two functional testing environments are available:

| Environment | API Host | Database | Logging | Best For |
|-------------|----------|----------|---------|----------|
| **Environment 1** | IIS Express or Kestrel (local) | Docker SQL Server | Docker Seq | Debugging, development |
| **Environment 2** | Docker Container | Docker SQL Server | Docker Seq | Integration testing, CI/CD |

Both environments use:
- **SQL Server 2022** (Developer Edition) running in Docker
- **Seq** for structured logging with web UI

---

## Prerequisites

1. **Docker Desktop** installed and running
   - Download: https://www.docker.com/products/docker-desktop

2. **.NET SDK 8.0+** installed
   - Download: https://dotnet.microsoft.com/download

3. **Visual Studio 2022** (for IIS Express) or **VS Code** with C# extension

4. **PowerShell 5.1+** (included with Windows)

---

## Environment 1: Local IDE + Docker Infrastructure

Use this environment when you want to debug the API in Visual Studio or VS Code while using Docker for the database and logging infrastructure.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Your Machine                             │
│  ┌─────────────────────┐     ┌─────────────────────────────┐│
│  │   Visual Studio     │     │      Docker Desktop         ││
│  │   ───────────────   │     │  ┌─────────────────────────┐││
│  │   IIS Express or    │────▶│  │  SQL Server 2022        │││
│  │   Kestrel           │     │  │  localhost:1433         │││
│  │   localhost:44349   │     │  └─────────────────────────┘││
│  │   or :5001          │     │  ┌─────────────────────────┐││
│  │                     │────▶│  │  Seq                    │││
│  │                     │     │  │  UI: localhost:8081     │││
│  └─────────────────────┘     │  │  API: localhost:5341    │││
│                              │  └─────────────────────────┘││
│                              └─────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

### Quick Start

```powershell
# Step 1: Start infrastructure containers
.\scripts\Start-FuncTestInfra.ps1

# Step 2: Initialize database (first time only)
.\scripts\Init-SqlServerDb.ps1

# Step 3: Run API using one of these methods:

# Option A: Visual Studio
# Select "IIS Express SqlServer" or "Hmm.ServiceApi SqlServer" profile, then press F5

# Option B: Command line
dotnet run --project src/Hmm.ServiceApi --launch-profile "Hmm.ServiceApi SqlServer"

# Step 4: Open browser to test
# https://localhost:5001/swagger (Kestrel)
# https://localhost:44349/swagger (IIS Express)
```

### Stopping the Environment

```powershell
# Stop infrastructure containers (keeps data)
.\scripts\Stop-FuncTest.ps1

# Stop and remove all data (clean slate)
.\scripts\Stop-FuncTest.ps1 -RemoveVolumes
```

---

## Environment 2: Full Docker Stack

Use this environment when you want to run everything in Docker containers, simulating a production-like setup.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Docker Desktop                           │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                   hmm-network                            ││
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  ││
│  │  │  hmm-api    │  │  sqlserver  │  │      seq        │  ││
│  │  │  :5001/:5000│─▶│  :1433      │  │  :5341/:8081    │  ││
│  │  └─────────────┘  └─────────────┘  └─────────────────┘  ││
│  └─────────────────────────────────────────────────────────┘│
│                              │                               │
│                    Port Mappings                             │
│              localhost:5001 ─┘ (API HTTPS)                   │
│              localhost:5000    (API HTTP)                    │
│              localhost:1433    (SQL Server)                  │
│              localhost:8081    (Seq UI)                      │
│              localhost:5341    (Seq Ingestion)               │
└─────────────────────────────────────────────────────────────┘
```

### Quick Start

```powershell
# Step 1: Start full stack (builds API image)
.\scripts\Start-FuncTestFull.ps1 -Build

# Step 2: Initialize database (first time only)
.\scripts\Init-SqlServerDb.ps1

# Step 3: Open browser to test
# https://localhost:5001/swagger
```

### Rebuilding After Code Changes

```powershell
# Rebuild and restart API container
.\scripts\Start-FuncTestFull.ps1 -Build
```

### Viewing API Logs

```powershell
# Stream logs from API container
docker logs -f hmm-api

# Or use docker-compose
docker-compose -f docker/docker-compose.sqlserver.yml logs -f hmm-api
```

### Stopping the Environment

```powershell
# Stop all containers (keeps data)
.\scripts\Stop-FuncTest.ps1 -Full

# Stop and remove all data (clean slate)
.\scripts\Stop-FuncTest.ps1 -Full -RemoveVolumes
```

---

---

## Authentication Setup

The API requires JWT tokens from the Hmm.Idp (Identity Provider). You need to:
1. Start the IDP
2. Create a test user
3. Obtain a token

### Starting the Identity Provider

The IDP needs its own SQL Server database (separate from the API database).

```powershell
# Option 1: Start IDP database in Docker
docker-compose -f docker/docker-compose.idp.yml up -d

# Option 2: Use the same SQL Server as API (change IDP connection string)
# Edit src/Hmm.Idp/appsettings.json to use localhost,1433
```

Then start the IDP:

```powershell
# Start the Identity Provider
.\scripts\Start-Idp.ps1

# Or manually
dotnet run --project src/Hmm.Idp
```

The IDP will be available at: https://localhost:5001

### Creating a Test User

1. Navigate to https://localhost:5001/Account/Register
2. Create a user with:
   - Email: `testuser@hmm.local`
   - Password: `TestPassword123#` (must meet complexity requirements)
3. Confirm the email (in development, emails may be auto-confirmed)

**Password Requirements:**
- At least 12 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character
- At least 6 unique characters

### Pre-configured Clients

The IDP is pre-configured with these clients:

| Client ID | Grant Type | Secret | Use Case |
|-----------|------------|--------|----------|
| `hmm.functest` | Password | `FuncTestSecret123#` | Automated testing |
| `hmm.m2m` | Client Credentials | `M2MSecret456#` | Service-to-service |
| `hmm.web` | Authorization Code | `WebSecret789#` | Web applications |

---

## Getting Test Tokens

### Method 1: PowerShell Script (Recommended)

```powershell
# Get a token with default test user
.\scripts\Get-TestToken.ps1

# Get token for specific user
.\scripts\Get-TestToken.ps1 -Username "testuser@hmm.local" -Password "TestPassword123#"

# Get just the token (for scripting)
$token = .\scripts\Get-TestToken.ps1 -Raw

# Get client credentials token (no user)
.\scripts\Get-TestToken.ps1 -GrantType client_credentials -ClientId "hmm.m2m" -ClientSecret "M2MSecret456#"
```

### Method 2: cURL

```bash
# Resource Owner Password Grant
curl -k -X POST https://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=hmm.functest" \
  -d "client_secret=FuncTestSecret123#" \
  -d "username=testuser@hmm.local" \
  -d "password=TestPassword123#" \
  -d "scope=openid profile email hmmapi"

# Client Credentials Grant
curl -k -X POST https://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=hmm.m2m" \
  -d "client_secret=M2MSecret456#" \
  -d "scope=hmmapi"
```

### Method 3: HTTP File

Add this request to your HTTP file:

```http
### Get Access Token (Resource Owner Password)
# @name getToken
POST https://localhost:5001/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password
&client_id=hmm.functest
&client_secret=FuncTestSecret123#
&username=testuser@hmm.local
&password=TestPassword123#
&scope=openid profile email hmmapi

### Use the token
@token = {{getToken.response.body.access_token}}
```

### Method 4: Postman

1. Create a new request to `POST https://localhost:5001/connect/token`
2. Set Body to `x-www-form-urlencoded`
3. Add these key-value pairs:
   - `grant_type`: `password`
   - `client_id`: `hmm.functest`
   - `client_secret`: `FuncTestSecret123#`
   - `username`: `testuser@hmm.local`
   - `password`: `TestPassword123#`
   - `scope`: `openid profile email hmmapi`
4. Send and copy the `access_token` from the response

### Token Response

A successful token request returns:

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expires_in": 3600,
  "token_type": "Bearer",
  "refresh_token": "...",
  "scope": "openid profile email hmmapi"
}
```

### Using the Token

Add the token to your API requests:

```http
GET https://localhost:5001/api/v1/notes
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## Database Initialization

The database schema must be initialized before first use.

### Automatic Initialization

```powershell
# Run after starting containers
.\scripts\Init-SqlServerDb.ps1
```

This script:
1. Connects to SQL Server at `localhost,1433`
2. Runs `docker/sqlserver/init-db.sql`
3. Creates the `hmm` database with all tables

### Manual Initialization

If you prefer to run SQL manually:

```powershell
# Using sqlcmd (if installed locally)
sqlcmd -S localhost,1433 -U sa -P "Shcdlhgm1!" -i docker/sqlserver/init-db.sql -C

# Using Docker container
docker exec -it hmm-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Shcdlhgm1!" -C
```

### Connecting with SQL Server Management Studio (SSMS)

| Setting | Value |
|---------|-------|
| Server | `localhost,1433` |
| Authentication | SQL Server Authentication |
| Login | `sa` |
| Password | `Shcdlhgm1!` |
| Trust Server Certificate | Yes |

---

## Using the HTTP Test File

The file `src/Hmm.ServiceApi/Hmm.ServiceApi.http` contains pre-built API requests for testing.

### In Visual Studio 2022

1. Open `Hmm.ServiceApi.http`
2. Click "Send Request" above any request
3. View response in the adjacent pane

### In VS Code (REST Client Extension)

1. Install "REST Client" extension
2. Open `Hmm.ServiceApi.http`
3. Click "Send Request" above any request

### Switching Environments

Edit the `@baseUrl` variable at the top of the file:

```http
# For IIS Express
@baseUrl = https://localhost:44349

# For Kestrel or Docker
@baseUrl = https://localhost:5001
```

### Authentication

Most endpoints require a JWT token. Update the token variable:

```http
@token = YOUR_JWT_TOKEN_HERE
```

---

## Viewing Logs in Seq

Seq provides a powerful web UI for viewing and querying structured logs.

### Access Seq UI

Open http://localhost:8081 in your browser.

### Common Queries

```
# View all errors
@Level = 'Error'

# View requests to a specific endpoint
RequestPath like '/api/%'

# View slow requests (> 1 second)
ElapsedMilliseconds > 1000

# Filter by correlation ID
CorrelationId = 'abc123'
```

### Log Levels

| Level | Description |
|-------|-------------|
| Verbose | Detailed debugging information |
| Debug | Internal system events |
| Information | Normal operation events |
| Warning | Abnormal but handled situations |
| Error | Errors that need attention |
| Fatal | Critical failures |

---

## Troubleshooting

### Container Won't Start

```powershell
# Check container status
docker ps -a

# View container logs
docker logs hmm-sqlserver
docker logs hmm-seq
docker logs hmm-api

# Restart Docker Desktop if needed
```

### SQL Server Connection Refused

1. Ensure container is running: `docker ps`
2. Wait for health check to pass (up to 30 seconds)
3. Check if port 1433 is available: `netstat -an | findstr 1433`

### API Can't Connect to Database

1. Verify correct environment is selected in launch profile
2. Check connection string in appsettings file
3. Ensure database is initialized: `.\scripts\Init-SqlServerDb.ps1`

### Port Already in Use

```powershell
# Find process using the port
netstat -ano | findstr :1433
netstat -ano | findstr :5341

# Kill process by PID
taskkill /PID <pid> /F

# Or change ports in docker-compose files
```

### Reset Everything

```powershell
# Stop all containers and remove volumes
.\scripts\Stop-FuncTest.ps1 -Full -RemoveVolumes

# Remove containers manually if needed
docker rm -f hmm-sqlserver hmm-seq hmm-api

# Remove volumes
docker volume rm docker_sqlserver-data docker_seq-data

# Start fresh
.\scripts\Start-FuncTestInfra.ps1
.\scripts\Init-SqlServerDb.ps1
```

---

## Configuration Reference

### Docker Compose Files

| File | Description |
|------|-------------|
| `docker/docker-compose.infra.yml` | SQL Server + Seq (infrastructure only) |
| `docker/docker-compose.sqlserver.yml` | API + SQL Server + Seq (full stack) |

### App Settings Files

| File | Environment | Database Host |
|------|-------------|---------------|
| `appsettings.Development.json` | Development | localhost (PostgreSQL) |
| `appsettings.LocalSqlServer.json` | LocalSqlServer | localhost:1433 (SQL Server) |
| `appsettings.DockerSqlServer.json` | DockerSqlServer | sqlserver:1433 (container) |
| `appsettings.Docker.json` | Docker | postgres (container, PostgreSQL) |

### Launch Profiles

| Profile | Environment | Host |
|---------|-------------|------|
| IIS Express | Development | localhost:44349 |
| IIS Express SqlServer | LocalSqlServer | localhost:44349 |
| Hmm.ServiceApi | Development | localhost:5001 |
| Hmm.ServiceApi SqlServer | LocalSqlServer | localhost:5001 |
| Docker | Docker | Container |

### PowerShell Scripts

| Script | Description |
|--------|-------------|
| `scripts/Start-FuncTestInfra.ps1` | Start SQL Server + Seq |
| `scripts/Start-FuncTestFull.ps1` | Start API + SQL Server + Seq |
| `scripts/Stop-FuncTest.ps1` | Stop containers |
| `scripts/Init-SqlServerDb.ps1` | Initialize database schema |

### Service Endpoints

| Service | URL | Credentials |
|---------|-----|-------------|
| API (HTTPS) | https://localhost:5001 | JWT Token |
| API (HTTP) | http://localhost:5000 | JWT Token |
| API (IIS Express) | https://localhost:44349 | JWT Token |
| Swagger UI | https://localhost:5001/swagger | - |
| Seq UI | http://localhost:8081 | - |
| Seq Ingestion | http://localhost:5341 | - |
| SQL Server | localhost,1433 | sa / Shcdlhgm1! |

---

## Quick Reference Card

```powershell
# ===== Environment 1: Local IDE =====
.\scripts\Start-FuncTestInfra.ps1      # Start API infrastructure (SQL Server + Seq)
docker-compose -f docker/docker-compose.idp.yml up -d  # Start IDP database
.\scripts\Init-SqlServerDb.ps1          # Init API database (once)
.\scripts\Start-Idp.ps1                 # Start Identity Provider (new terminal)
# Register test user at https://localhost:5001/Account/Register
.\scripts\Get-TestToken.ps1             # Get access token
# Run API with "IIS Express SqlServer" or "Hmm.ServiceApi SqlServer" profile
.\scripts\Stop-FuncTest.ps1             # Stop infrastructure

# ===== Environment 2: Full Docker =====
.\scripts\Start-FuncTestFull.ps1 -Build # Start API + SQL Server + Seq
docker-compose -f docker/docker-compose.idp.yml up -d  # Start IDP database
.\scripts\Init-SqlServerDb.ps1          # Init API database (once)
.\scripts\Start-Idp.ps1                 # Start Identity Provider (new terminal)
.\scripts\Get-TestToken.ps1             # Get access token
# Access https://localhost:5001/swagger
.\scripts\Stop-FuncTest.ps1 -Full       # Stop everything

# ===== Authentication =====
.\scripts\Start-Idp.ps1                 # Start Identity Provider
.\scripts\Get-TestToken.ps1             # Get token (password grant)
.\scripts\Get-TestToken.ps1 -Raw        # Get just the token string
.\scripts\Get-TestToken.ps1 -GrantType client_credentials -ClientId hmm.m2m -ClientSecret "M2MSecret456#"

# ===== Useful Commands =====
docker ps                               # List running containers
docker logs -f hmm-api                  # Stream API logs
docker exec -it hmm-sqlserver bash      # Shell into SQL container

# ===== URLs =====
# API:         https://localhost:5001/swagger
# IDP:         https://localhost:5001 (when running locally)
# IDP Token:   https://localhost:5001/connect/token
# IDP Register: https://localhost:5001/Account/Register
# Seq:         http://localhost:8081
# API SQL:     localhost,1433 (sa / Shcdlhgm1!)
# IDP SQL:     localhost,14333 (sa / Password1!)

# ===== Test Credentials =====
# Test User:   testuser@hmm.local / TestPassword123#
# FuncTest:    hmm.functest / FuncTestSecret123#
# M2M Client:  hmm.m2m / M2MSecret456#
```
