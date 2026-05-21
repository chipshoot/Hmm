# Getting Access Tokens for API Testing

## 🎯 Quick Answer

Your API uses **IdentityServer/Duende IdentityServer** for authentication. Here are **3 easy ways** to get a token:

---

## ✅ Method 1: Using .http File (EASIEST - Recommended)

### Step-by-Step:

1. **Open** `src/Hmm.ServiceApi/Hmm.ServiceApi.http` in Visual Studio

2. **Find the token request section** (around line 40)

3. **Click the green play button (▶)** next to this request:
   ```http
   ### OPTION 1: Get Token - Test User (recommended for testing) ⭐
   # @name getToken
   POST {{idpUrl}}/connect/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=password&client_id=hmm.functest&client_secret=FuncTestSecret123#&username=testuser@hmm.local&password=TestPassword123#&scope=openid profile email hmmapi
   ```

4. **Copy the `access_token`** from the response (in the HTTP Response window)

5. **Paste it** in the token variable (around line 60):
   ```
   @hmmIdpToken = {{getToken.response.body.access_token}}
   ```
   Or manually:
   ```
   @hmmIdpToken = eyJhbGc...  (paste your token)
   ```

6. **Test an API endpoint** - scroll down and click ▶ next to any request:
   ```http
   ### Get all notes
   GET {{baseUrl}}/v{{apiVersion}}/notes
   Authorization: Bearer {{token}}
   ```

### Available Test Users:

| User | Username | Password | Role | Description |
|------|----------|----------|------|-------------|
| **Test User** ⭐ | `testuser@hmm.local` | `TestPassword123#` | User | Best for testing |
| Alice | `alice` | `Alice@12345678#` | User | Sample user |
| Bob | `bob` | `Bob@123456789#` | User | Sample user |
| Admin | `admin@hmm.local` | `Admin@12345678#` | Administrator | Admin privileges |
| M2M | (Client Credentials) | N/A | API Client | Machine-to-machine |

---

## ✅ Method 2: Using PowerShell Script (AUTOMATIC)

### Quick Command:

```powershell
# Get token and copy to clipboard
.\Get-HmmApiToken.ps1 -User testuser -CopyToClipboard

# Then paste in .http file:
# @hmmIdpToken = <Ctrl+V>
```

### Available Options:

```powershell
# Test user (default)
.\Get-HmmApiToken.ps1 -User testuser -CopyToClipboard

# Alice
.\Get-HmmApiToken.ps1 -User alice -CopyToClipboard

# Bob
.\Get-HmmApiToken.ps1 -User bob -CopyToClipboard

# Admin
.\Get-HmmApiToken.ps1 -User admin -CopyToClipboard

# Machine-to-Machine (Client Credentials)
.\Get-HmmApiToken.ps1 -User serviceapi -CopyToClipboard

# Custom Identity Server URL
.\Get-HmmApiToken.ps1 -User testuser -IdpUrl "http://localhost:5001" -CopyToClipboard
```

The script will:
- ✅ Call the token endpoint
- ✅ Display the token
- ✅ Copy to clipboard (if flag used)
- ✅ Show token expiration time
- ✅ Provide usage examples

---

## ✅ Method 3: Using cURL or Postman

### cURL (Windows PowerShell):

```powershell
# Get token for test user
curl -X POST https://localhost:5001/connect/token `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "grant_type=password&client_id=hmm.functest&client_secret=FuncTestSecret123#&username=testuser@hmm.local&password=TestPassword123#&scope=openid profile email hmmapi" `
  --insecure

# Then copy the access_token from the JSON response
```

### Using Postman:

1. **Create a POST request** to `https://localhost:5001/connect/token`

2. **Set headers:**
   - Content-Type: `application/x-www-form-urlencoded`

3. **Set body** (x-www-form-urlencoded):
   ```
   grant_type: password
   client_id: hmm.functest
   client_secret: FuncTestSecret123#
   username: testuser@hmm.local
   password: TestPassword123#
   scope: openid profile email hmmapi
   ```

4. **Click Send**

5. **Copy `access_token`** from response

---

## 📋 Complete Workflow Example

### Scenario: Test the Authors API

```powershell
# 1. Start the infrastructure (if not running)
.\start-dev-env.ps1

# 2. Start Identity Server (Hmm.Idp project)
#    - Run it separately OR ensure it's running

# 3. Start the API (Hmm.ServiceApi project)
#    - Visual Studio: Select profile and press F5

# 4. Open Hmm.ServiceApi.http file

# 5. Get a token (click ▶ next to the token request)
# 6. Copy access_token from response
# 7. Paste in @hmmIdpToken variable
# 8. Test API endpoints (click ▶ next to any API request)
```

---

## 🔐 Authentication Details

### Identity Server Configuration:

**Token Endpoint:**
- Local: `https://localhost:5001/connect/token`
- Docker: `http://localhost:5001/connect/token`

**Test Client Configuration:**

| Client ID | Grant Type | Secret | Scopes | Use Case |
|-----------|------------|--------|--------|----------|
| `hmm.functest` | Password (ROPC) | `FuncTestSecret123#` | openid, profile, email, hmmapi | User authentication testing |
| `hmm.m2m` | Client Credentials | `M2MSecret456#` | hmmapi | Machine-to-machine |
| `hmm.web` | Authorization Code + PKCE | `WebSecret789#` | openid, profile, email, hmmapi | Web application |
| `hmm.serviceapi` | Client Credentials | `ServiceApiSecret#@#456` | hmmapi | Service-to-service |

### Token Response Format:

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "expires_in": 3600,
  "token_type": "Bearer",
  "scope": "openid profile email hmmapi"
}
```

**Token Lifetime:** 1 hour (3600 seconds)

---

## 🚀 Recommended Workflow

### For Daily Development:

**Quick Method (Recommended):**
```powershell
# Get token and copy to clipboard
.\Get-HmmApiToken.ps1 -User testuser -CopyToClipboard

# Open .http file and paste:
# @hmmIdpToken = <Ctrl+V>

# Start testing APIs!
```

**Visual Studio .http File Method:**
1. Open `Hmm.ServiceApi.http`
2. Click ▶ next to "Get Token - Test User"
3. Copy `access_token` from response
4. Paste in `@hmmIdpToken` variable
5. Click ▶ on any API request to test

---

## ⚠️ Prerequisites

Before getting tokens:

### 1. Identity Server Must Be Running

Check if Hmm.Idp is running:
```powershell
# Test if Idp is accessible
curl https://localhost:5001/.well-known/openid-configuration --insecure
```

If not running:
- **Visual Studio:** Run the Hmm.Idp project
- **Command Line:** `cd src\Hmm.Idp && dotnet run`

### 2. Database Must Be Seeded

The test users (testuser, alice, bob, admin) are automatically seeded when:
- Environment is `Development` or `Docker`
- Database is created/updated

Verify seeding in logs:
```
[12:34:56 Information] Seeded IdentityResources
[12:34:56 Information] Seeded ApiScope: hmmapi
[12:34:56 Information] Created user: testuser@hmm.local
```

### 3. API Must Be Running

The Hmm.ServiceApi must be running to test endpoints (but not required to get tokens).

---

## 🧪 Testing Different Scenarios

### Test as Regular User:
```powershell
.\Get-HmmApiToken.ps1 -User testuser -CopyToClipboard
```

### Test as Administrator:
```powershell
.\Get-HmmApiToken.ps1 -User admin -CopyToClipboard
```

### Test Machine-to-Machine:
```powershell
.\Get-HmmApiToken.ps1 -User serviceapi -CopyToClipboard
```

### Test Token Expiration:
Tokens expire after 1 hour. If you get `401 Unauthorized`, get a fresh token.

---

## 🔍 Troubleshooting

### Error: "invalid_client"

**Cause:** Client credentials are incorrect

**Fix:** Ensure you're using the correct client credentials from SeedDataService:
- Client ID: `hmm.functest`
- Client Secret: `FuncTestSecret123#`

### Error: "invalid_grant" or "Invalid username or password"

**Cause:** User credentials are incorrect or user doesn't exist

**Fix:** 
1. Check username/password match SeedDataService
2. Ensure Idp database is seeded (check logs)
3. Verify user exists in database

### Error: "Unable to connect"

**Cause:** Identity Server is not running

**Fix:**
```powershell
# Start Hmm.Idp project
cd src\Hmm.Idp
dotnet run

# Or run in Visual Studio
```

### Token Gives 401 Unauthorized on API

**Causes:**
1. Token expired (older than 1 hour)
2. Wrong audience (token not for "hmmapi")
3. API not configured correctly

**Fix:**
1. Get a fresh token
2. Verify scope includes "hmmapi"
3. Check API's appsettings for IdpBaseUrl

---

## 📚 Additional Resources

### Decode Token (See Claims):

Visit https://jwt.io and paste your token to see the claims:
```json
{
  "iss": "https://localhost:5001",
  "aud": "hmmapi",
  "sub": "89b141ea-7b55-446e-a026-733236720466",
  "email": "testuser@hmm.local",
  "name": "Test User",
  "role": "User",
  "scope": ["email", "hmmapi", "openid", "profile"],
  "exp": 1771133255
}
```

### Token Introspection Endpoint:

```http
POST https://localhost:5001/connect/introspect
Content-Type: application/x-www-form-urlencoded
Authorization: Basic <base64(clientId:clientSecret)>

token=YOUR_ACCESS_TOKEN
```

### User Info Endpoint:

```http
GET https://localhost:5001/connect/userinfo
Authorization: Bearer YOUR_ACCESS_TOKEN
```

---

## 🎯 Summary

**Fastest way to test right now:**

1. Make sure Idp and API are running
2. Open `Hmm.ServiceApi.http` in Visual Studio
3. Scroll to line ~40 and click ▶ next to "Get Token - Test User"
4. Copy the `access_token` from response
5. Paste it at line ~60: `@hmmIdpToken = <paste here>`
6. Click ▶ on any API request below to test

**Or use the PowerShell script:**
```powershell
.\Get-HmmApiToken.ps1 -User testuser -CopyToClipboard
```

Then paste in the .http file and start testing! 🚀
