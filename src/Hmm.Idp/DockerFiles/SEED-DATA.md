# Hmm.Idp Seed Data Documentation

This document describes the seed data that is automatically created when running Hmm.Idp in Development or Docker mode.

## Environment Variables

- `SEED_DATA=true` - Set this environment variable to enable seeding in any environment
- `ASPNETCORE_ENVIRONMENT=Development` or `Docker` - Automatically enables seeding

## Seeded Clients

The following OAuth2/OIDC clients are automatically created:

### 1. hmm.functest (Functional Testing)
- **Grant Type:** Resource Owner Password
- **Client Secret:** `FuncTestSecret123!`
- **Use Case:** Automated testing and scripts
- **Scopes:** openid, profile, email, hmmapi
- **Token Lifetime:** 1 hour
- **Refresh Token:** Enabled (24-hour sliding)

### 2. hmm.m2m (Machine-to-Machine)
- **Grant Type:** Client Credentials
- **Client Secret:** `M2MSecret456!`
- **Use Case:** Service-to-service communication
- **Scopes:** hmmapi
- **Token Lifetime:** 1 hour

### 3. hmm.web (Web Application)
- **Grant Type:** Authorization Code with PKCE
- **Client Secret:** `WebSecret789!`
- **Use Case:** Interactive web applications
- **Redirect URIs:**
  - https://localhost:5002/signin-oidc
  - https://localhost:44342/signin-oidc
- **Scopes:** openid, profile, email, hmmapi
- **Refresh Token:** Enabled

### 4. hmm.serviceapi (Service API)
- **Grant Type:** Client Credentials
- **Client Secret:** `ServiceApiSecret!@#456`
- **Use Case:** Hmm.ServiceApi token validation
- **Scopes:** hmmapi
- **Token Lifetime:** 1 hour

## Seeded Users

The following test users are automatically created:

### 1. Administrator
- **Username:** admin@hmm.local
- **Password:** `Admin@12345678!`
- **Email:** admin@hmm.local
- **Roles:** Administrator
- **Use Case:** System administration

### 2. Test User (for functional testing)
- **Username:** testuser@hmm.local
- **Password:** `TestPassword123!`
- **Email:** testuser@hmm.local
- **Roles:** User
- **Use Case:** API functional testing (matches .http file)

### 3. Alice (Test User)
- **Username:** alice
- **Password:** `Alice@12345678!`
- **Email:** alicesmith@email.com
- **Roles:** User
- **Use Case:** Standard test user

### 4. Bob (Test User)
- **Username:** bob
- **Password:** `Bob@123456789!`
- **Email:** bobsmith@email.com
- **Roles:** User
- **Use Case:** Standard test user

### 5. Service API User
- **Username:** serviceapi@hmm.local
- **Password:** `ServiceApi@123!`
- **Email:** serviceapi@hmm.local
- **Roles:** ApiClient
- **Use Case:** Service API authentication

## Seeded Roles

- **Administrator** - Full system access
- **User** - Standard user access
- **ApiClient** - API client access

## API Resources

### hmmapi
- **Display Name:** Hmm API
- **User Claims:** name, email, role

## Identity Resources

- **openid** - Subject identifier
- **profile** - User profile (name, family_name, given_name, etc.)
- **email** - Email address and verification status

## Getting Tokens

### Resource Owner Password (for testing)
```bash
curl -X POST https://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=hmm.functest" \
  -d "client_secret=FuncTestSecret123!" \
  -d "username=testuser@hmm.local" \
  -d "password=TestPassword123!" \
  -d "scope=openid profile email hmmapi"
```

### Client Credentials (for service-to-service)
```bash
curl -X POST https://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=hmm.m2m" \
  -d "client_secret=M2MSecret456!" \
  -d "scope=hmmapi"
```

## Docker Usage

```bash
# Start the IDP with SQL Server
cd src/Hmm.Idp/DockerFiles
docker-compose up -d

# View logs
docker-compose logs -f hmm-idp

# Stop services
docker-compose down

# Stop and remove volumes (reset database)
docker-compose down -v
```

## Port Configuration

| Service | Local Port | Container Port |
|---------|------------|----------------|
| Hmm.Idp | 5001 | 80 |
| SQL Server | 14333 | 1433 |
