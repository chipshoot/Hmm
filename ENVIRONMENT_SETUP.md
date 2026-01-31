# Environment Setup Guide - Hmm Service API

This guide explains how to run the Hmm Service API in different environments.

## Available Environments

1. **IIS Express** - Local development using IIS Express
2. **Kestrel (Development)** - Local development using Kestrel web server
3. **Docker** - Containerized deployment

## Configuration Files

- `appsettings.json` - Base configuration (shared by all environments)
- `appsettings.Development.json` - IIS Express and Kestrel local development
- `appsettings.Docker.json` - Docker container environment

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

## Switching Between Environments

### In Visual Studio

Use the **dropdown menu** next to the play button:
- Select **"IIS Express"** for IIS Express
- Select **"Hmm.ServiceApi"** for Kestrel
- Select **"Docker"** for Docker

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

| Feature | IIS Express/Kestrel | Docker |
|---------|---------------------|--------|
| Database Host | `localhost` | `postgres` (container name) |
| Seq Logging | `http://localhost:5341` | `http://seq:5341` (container) |
| SSL Certificate | Development cert | Mount from host |
| Startup Time | Fast | Slower (image build) |
| Isolation | No | Full container isolation |
| Best For | Quick debugging | Production-like testing |

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
