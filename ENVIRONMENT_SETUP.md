# Environment Setup Guide - Hmm Service API

This guide explains how to run the Hmm Service API in different environments.

## Available Environments

1. **IIS Express** - Local development using IIS Express (PostgreSQL)
2. **Kestrel (Development)** - Local development using Kestrel web server (PostgreSQL)
3. **Docker** - Containerized deployment (PostgreSQL)
4. **IIS Express SqlServer** - Functional testing with Docker SQL Server
5. **Kestrel SqlServer** - Functional testing with Docker SQL Server
6. **Full Docker Stack** - API + SQL Server + Seq in Docker

## Configuration Files

- `appsettings.json` - Base configuration (shared by all environments)
- `appsettings.Development.json` - IIS Express and Kestrel local development (PostgreSQL)
- `appsettings.Docker.json` - Docker container environment (PostgreSQL)
- `appsettings.LocalSqlServer.json` - Local IDE with Docker SQL Server
- `appsettings.DockerSqlServer.json` - Docker API with Docker SQL Server

---

## 1. Running with IIS Express

### Prerequisites
- Visual Studio 2022+
- PostgreSQL installed locally on port 5432
- .NET 10 SDK

### Steps
1. Open the solution in Visual Studio
2. In the toolbar, select **"IIS Express"** from the dropdown
3. Press **F5** or click the green play button
4. Browser opens automatically to `https://localhost:44349/swagger`

### Configuration Used
- **appsettings.json** + **appsettings.Development.json**
- Database: `localhost:5432`
- Seq logging: `http://localhost:5341`

---

## 2. Running with Kestrel (Project Mode)

### Prerequisites
- .NET 10 SDK
- PostgreSQL installed locally on port 5432

### Steps

**Via Visual Studio:**
1. Select **"Hmm.ServiceApi"** from the dropdown
2. Press **F5** or click the green play button
3. Browser opens to `https://localhost:5001/swagger`

**Via Command Line:**
```powershell
cd src\Hmm.ServiceApi
dotnet run
```

### Configuration Used
- **appsettings.json** + **appsettings.Development.json**
- Database: `localhost:5432`
- URLs: `https://localhost:5001` and `http://localhost:5000`

---

## 3. Running with Docker

### Prerequisites
- Docker Desktop installed and running
- .NET 10 SDK (for building)

### Option A: Visual Studio with Docker Profile

1. Select **"Docker"** from the dropdown in Visual Studio
2. Press **F5** or click the green play button
3. Visual Studio builds the Docker image and starts the container
4. Browser opens to the containerized API

### Option B: Docker Compose (Recommended)

This starts the API, PostgreSQL, and Seq log server all together.

#### First-time Setup: Generate HTTPS Certificate

```powershell
# Generate development certificate
dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\aspnetapp.pfx -p YourSecurePassword

# Trust the certificate
dotnet dev-certs https --trust
```

#### Run Docker Compose

```powershell
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f hmm-api

# Stop all services
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

#### Access Points
- **API**: `https://localhost:5001/swagger` or `http://localhost:5000/swagger`
- **Seq Logs**: `http://localhost:5341`
- **PostgreSQL**: `localhost:5432`

### Configuration Used
- **appsettings.json** + **appsettings.Docker.json**
- Database: `postgres:5432` (container name)
- Seq logging: `http://seq:5341` (container name)

### Option C: Manual Docker Commands

```powershell
# Build the image
docker build -f src/Hmm.ServiceApi/Dockerfile -t hmm-api:latest .

# Run PostgreSQL
docker run -d --name hmm-postgres `
  -e POSTGRES_PASSWORD=Shcdlhgm1! `
  -p 5432:5432 `
  postgres:15-alpine

# Run the API
docker run -d --name hmm-api `
  -e ASPNETCORE_ENVIRONMENT=Docker `
  -e ASPNETCORE_URLS="https://+:443;http://+:80" `
  -p 5000:80 -p 5001:443 `
  --link hmm-postgres:postgres `
  hmm-api:latest
```

---

## 4. Functional Testing with SQL Server

These environments use Docker SQL Server and Seq for functional/integration testing.

### Prerequisites
- Docker Desktop installed and running
- .NET 10 SDK
- PowerShell 5.1+

### Environment 4a: IIS Express + Docker SQL Server

Best for debugging in Visual Studio while using production-like database.

```powershell
# Step 1: Start infrastructure (SQL Server + Seq)
.\scripts\Start-FuncTestInfra.ps1

# Step 2: Initialize database (first time only)
.\scripts\Init-SqlServerDb.ps1

# Step 3: In Visual Studio, select "IIS Express SqlServer" profile and press F5

# Step 4: Stop when done
.\scripts\Stop-FuncTest.ps1
```

**Configuration Used:**
- **appsettings.json** + **appsettings.LocalSqlServer.json**
- Database: `localhost,1433` (Docker SQL Server)
- Seq logging: `http://localhost:5341` (Docker Seq)
- API URL: `https://localhost:44349`

### Environment 4b: Kestrel + Docker SQL Server

Best for command-line development with SQL Server.

```powershell
# Step 1: Start infrastructure
.\scripts\Start-FuncTestInfra.ps1

# Step 2: Initialize database (first time only)
.\scripts\Init-SqlServerDb.ps1

# Step 3: Run API
dotnet run --project src/Hmm.ServiceApi --launch-profile "Hmm.ServiceApi SqlServer"

# Step 4: Stop when done
.\scripts\Stop-FuncTest.ps1
```

**Configuration Used:**
- **appsettings.json** + **appsettings.LocalSqlServer.json**
- Database: `localhost,1433` (Docker SQL Server)
- Seq logging: `http://localhost:5341` (Docker Seq)
- API URL: `https://localhost:5001`

### Environment 4c: Full Docker Stack (API + SQL Server + Seq)

Best for integration testing in a production-like environment.

```powershell
# Step 1: Start everything
.\scripts\Start-FuncTestFull.ps1 -Build

# Step 2: Initialize database (first time only)
.\scripts\Init-SqlServerDb.ps1

# Step 3: Access API at https://localhost:5001/swagger

# Step 4: Stop when done
.\scripts\Stop-FuncTest.ps1 -Full
```

**Configuration Used:**
- **appsettings.json** + **appsettings.DockerSqlServer.json**
- Database: `sqlserver:1433` (container network)
- Seq logging: `http://seq:5341` (container network)
- API URL: `https://localhost:5001`

### Access Points (SQL Server Environments)

| Service | URL | Credentials |
|---------|-----|-------------|
| API (HTTPS) | https://localhost:5001 | JWT Token |
| API (IIS Express) | https://localhost:44349 | JWT Token |
| Swagger UI | https://localhost:5001/swagger | - |
| Seq UI | http://localhost:8081 | - |
| SQL Server | localhost,1433 | sa / Shcdlhgm1! |

---

## 5. Authentication Setup

The API requires JWT tokens from the Hmm.Idp (Identity Provider).

### Starting the Identity Provider

```powershell
# Step 1: Start IDP database (separate SQL Server instance)
docker-compose -f docker/docker-compose.idp.yml up -d

# Step 2: Start the Identity Provider
.\scripts\Start-Idp.ps1
# Or: dotnet run --project src/Hmm.Idp
```

The IDP runs at: https://localhost:5001

### Creating a Test User

1. Navigate to https://localhost:5001/Account/Register
2. Register with:
   - Email: `testuser@hmm.local`
   - Password: `TestPassword123!`

**Password Requirements:**
- At least 12 characters
- Uppercase, lowercase, digit, and special character
- At least 6 unique characters

### Getting a Test Token

```powershell
# Method 1: PowerShell script
.\scripts\Get-TestToken.ps1

# Method 2: Get just the token (for scripting)
$token = .\scripts\Get-TestToken.ps1 -Raw

# Method 3: Client credentials (no user)
.\scripts\Get-TestToken.ps1 -GrantType client_credentials -ClientId hmm.m2m -ClientSecret "M2MSecret456!"
```

### Pre-configured Clients

| Client ID | Grant Type | Secret | Use Case |
|-----------|------------|--------|----------|
| `hmm.functest` | Password | `FuncTestSecret123!` | Automated testing |
| `hmm.m2m` | Client Credentials | `M2MSecret456!` | Service-to-service |
| `hmm.web` | Authorization Code | `WebSecret789!` | Web applications |

### Token Request (cURL)

```bash
curl -k -X POST https://localhost:5001/connect/token \
  -d "grant_type=password" \
  -d "client_id=hmm.functest" \
  -d "client_secret=FuncTestSecret123!" \
  -d "username=testuser@hmm.local" \
  -d "password=TestPassword123!" \
  -d "scope=openid profile email hmmapi"
```

### Using the Token

Add to your API requests:
```
Authorization: Bearer <your-token-here>
```

---

## Switching Between Environments

### In Visual Studio

Use the **dropdown menu** next to the play button:
- Select **"IIS Express"** for IIS Express (PostgreSQL)
- Select **"IIS Express SqlServer"** for IIS Express (SQL Server)
- Select **"Hmm.ServiceApi"** for Kestrel (PostgreSQL)
- Select **"Hmm.ServiceApi SqlServer"** for Kestrel (SQL Server)
- Select **"Docker"** for Docker (PostgreSQL)

### Via Command Line

Set the `ASPNETCORE_ENVIRONMENT` variable:

**IIS Express / Kestrel (Development):**
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

**Docker:**
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Docker"
dotnet run
```

---

## Key Differences Between Environments

| Feature | IIS Express/Kestrel | Docker (PostgreSQL) | Local + SQL Server | Full Docker Stack |
|---------|---------------------|---------------------|--------------------|--------------------|
| Database | PostgreSQL localhost | PostgreSQL container | SQL Server Docker | SQL Server container |
| Database Host | `localhost:5432` | `postgres:5432` | `localhost,1433` | `sqlserver:1433` |
| Seq Logging | `localhost:5341` | `seq:5341` | `localhost:5341` | `seq:5341` |
| API Host | Local process | Container | Local process | Container |
| Startup Time | Fast | Slower | Fast | Slower |
| Best For | Quick debugging | PostgreSQL testing | SQL Server testing | Integration testing |

---

## Testing the API

### Using the .http File

1. Open `src/Hmm.ServiceApi/Hmm.ServiceApi.http`
2. Update the `@baseUrl` variable:
   - IIS Express: `https://localhost:44349`
   - Kestrel: `https://localhost:5001`
   - Docker: `https://localhost:5001`
3. Click the green play button (▶) next to any request

### Using Swagger UI

Navigate to the Swagger UI:
- IIS Express: `https://localhost:44349/swagger`
- Kestrel: `https://localhost:5001/swagger`
- Docker: `https://localhost:5001/swagger`

---

## Troubleshooting

### Database Connection Issues

**IIS Express/Kestrel:**
- Ensure PostgreSQL is running: `Get-Service postgresql-x64-*`
- Test connection: `psql -h localhost -U postgres`

**Docker:**
- Check PostgreSQL container: `docker ps | findstr postgres`
- Check logs: `docker logs hmm-postgres`

### Port Already in Use

```powershell
# Find process using port 5001
netstat -ano | findstr :5001

# Kill the process (replace PID)
taskkill /PID <PID> /F
```

### Docker Build Failures

```powershell
# Clean Docker cache
docker system prune -a

# Rebuild without cache
docker-compose build --no-cache
```

### SSL Certificate Issues (Docker)

```powershell
# Regenerate certificate
dotnet dev-certs https --clean
dotnet dev-certs https -ep $env:USERPROFILE\.aspnet\https\aspnetapp.pfx -p YourSecurePassword
dotnet dev-certs https --trust
```

---

## Environment Variables Reference

### Development (IIS Express/Kestrel)
```
ASPNETCORE_ENVIRONMENT=Development
```

### Docker
```
ASPNETCORE_ENVIRONMENT=Docker
ASPNETCORE_URLS=https://+:443;http://+:80
```

---

## Database Migrations

**Local Development:**
```powershell
cd src\Hmm.Core.Dal.EF
dotnet ef database update --project ../Hmm.ServiceApi
```

**Docker:**
```powershell
docker exec -it hmm-api dotnet ef database update
```

---

## Production Deployment

For production, create `appsettings.Production.json` with:
- Secure connection strings (use secrets management)
- Production database host
- Production logging configuration
- Disabled Swagger UI
- HTTPS enforcement

**Never commit sensitive data** like passwords or API keys to source control!

---

## Quick Reference Card

```powershell
# ===== PostgreSQL Development (Original) =====
# Requires PostgreSQL installed locally or via Docker
dotnet run --project src/Hmm.ServiceApi

# ===== SQL Server Functional Testing =====
# Environment 1: Local IDE + Docker SQL Server
.\scripts\Start-FuncTestInfra.ps1           # Start SQL Server + Seq
.\scripts\Init-SqlServerDb.ps1               # Init database (once)
# Select "IIS Express SqlServer" or "Hmm.ServiceApi SqlServer" in Visual Studio
.\scripts\Stop-FuncTest.ps1                  # Stop containers

# Environment 2: Full Docker Stack
.\scripts\Start-FuncTestFull.ps1 -Build      # Start API + SQL Server + Seq
.\scripts\Init-SqlServerDb.ps1               # Init database (once)
.\scripts\Stop-FuncTest.ps1 -Full            # Stop containers

# ===== Authentication =====
docker-compose -f docker/docker-compose.idp.yml up -d  # Start IDP database
.\scripts\Start-Idp.ps1                      # Start Identity Provider
# Register user at https://localhost:5001/Account/Register
.\scripts\Get-TestToken.ps1                  # Get access token

# ===== Service URLs =====
# API (Kestrel/Docker):  https://localhost:5001/swagger
# API (IIS Express):     https://localhost:44349/swagger
# IDP:                   https://localhost:5001
# Seq UI:                http://localhost:8081
# API SQL Server:        localhost,1433 (sa / Shcdlhgm1!)
# IDP SQL Server:        localhost,14333 (sa / Password1!)

# ===== Test Credentials =====
# User:        testuser@hmm.local / TestPassword123!
# FuncTest:    hmm.functest / FuncTestSecret123!
# M2M Client:  hmm.m2m / M2MSecret456!
```

---

## Helper Scripts Reference

| Script | Description |
|--------|-------------|
| `scripts/Start-FuncTestInfra.ps1` | Start SQL Server + Seq containers |
| `scripts/Start-FuncTestFull.ps1` | Start API + SQL Server + Seq containers |
| `scripts/Stop-FuncTest.ps1` | Stop functional testing containers |
| `scripts/Init-SqlServerDb.ps1` | Initialize SQL Server database schema |
| `scripts/Start-Idp.ps1` | Start the Identity Provider |
| `scripts/Get-TestToken.ps1` | Get JWT access token from IDP |
| `scripts/Register-TestUser.ps1` | Guide for creating test users |

---

## See Also

- [Functional Testing Guide](docs/FUNCTIONAL_TESTING_GUIDE.md) - Detailed functional testing documentation
- [CLAUDE.md](CLAUDE.md) - Development commands and architecture overview
