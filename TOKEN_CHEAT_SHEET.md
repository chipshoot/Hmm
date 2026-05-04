# 🔑 Token Testing - Quick Cheat Sheet

## 🚀 Fastest Method (30 seconds)

### In Visual Studio - Using .http File:

```
1. Open: src/Hmm.ServiceApi/Hmm.ServiceApi.http

2. Scroll to line ~42

3. Click ▶ button next to:
   ### OPTION 1: Get Token - Test User (recommended for testing) ⭐

4. In HTTP Response window, copy the access_token value

5. Scroll to line ~72, paste token:
   @hmmIdpToken = eyJhbGciOiJSUzI1NiIs...

6. Scroll to any API request, click ▶ to test!
```

**Result:** You're now authenticated and can test all endpoints! 🎉

---

## 🔐 Available Test Users

```
┌─────────────┬──────────────────────┬────────────────────┬──────────────┐
│ User        │ Username             │ Password           │ Role         │
├─────────────┼──────────────────────┼────────────────────┼──────────────┤
│ Test User ⭐│ testuser@hmm.local   │ TestPassword123#   │ User         │
│ Alice       │ alice                │ Alice@12345678#    │ User         │
│ Bob         │ bob                  │ Bob@123456789#     │ User         │
│ Admin       │ admin@hmm.local      │ Admin@12345678#    │ Administrator│
└─────────────┴──────────────────────┴────────────────────┴──────────────┘
```

**Recommendation:** Use `testuser@hmm.local` for routine testing.

---

## 💻 Alternative: PowerShell Script

```powershell
# Method A: Display token
.\Get-HmmApiToken.ps1 -User testuser

# Method B: Copy to clipboard
.\Get-HmmApiToken.ps1 -User testuser -CopyToClipboard

# Then paste in .http file: @hmmIdpToken = <Ctrl+V>
```

**Options:** `-User` can be: `testuser`, `alice`, `bob`, `admin`, `serviceapi`

---

## 🌐 Manual Token Request

### Using cURL:

```powershell
curl -X POST https://localhost:5001/connect/token `
  -H "Content-Type: application/x-www-form-urlencoded" `
  -d "grant_type=password&client_id=hmm.functest&client_secret=FuncTestSecret123#&username=testuser@hmm.local&password=TestPassword123#&scope=openid profile email hmmapi" `
  --insecure | ConvertFrom-Json | Select-Object -ExpandProperty access_token
```

### Using Invoke-RestMethod:

```powershell
$response = Invoke-RestMethod -Uri "https://localhost:5001/connect/token" `
  -Method Post `
  -Body @{
    grant_type = "password"
    client_id = "hmm.functest"
    client_secret = "FuncTestSecret123#"
    username = "testuser@hmm.local"
    password = "TestPassword123#"
    scope = "openid profile email hmmapi"
  } `
  -SkipCertificateCheck

$token = $response.access_token
Write-Host $token
$token | Set-Clipboard
```

---

## ✅ Complete Test Workflow

### Step-by-Step:

```powershell
# 1. Start infrastructure (PostgreSQL + Seq)
.\start-dev-env.ps1

# 2. Verify both projects are running:
#    - Hmm.Idp (Identity Server) at https://localhost:5001
#    - Hmm.ServiceApi (API) at https://localhost:5002 or https://localhost:44349

# 3. Get a token (choose ONE method):

   # METHOD A: .http file
   # - Open Hmm.ServiceApi.http
   # - Click ▶ next to "Get Token - Test User"
   # - Copy access_token
   # - Paste in @hmmIdpToken

   # METHOD B: PowerShell script
   .\Get-HmmApiToken.ps1 -User testuser -CopyToClipboard

# 4. Test API endpoints
#    - In .http file, click ▶ next to any request
#    - All requests will use: Authorization: Bearer {{token}}
```

---

## ⏰ Token Expiration

- **Lifetime:** 1 hour (3600 seconds)
- **When expired:** API returns `401 Unauthorized`
- **Solution:** Get a new token (repeat steps above)

---

## 🔍 Verify Token Works

### Quick Test:

```http
### Test Authentication
GET {{baseUrl}}/v1.0/authors
Authorization: Bearer {{token}}
```

**Expected:** `200 OK` with authors data

**If 401:** Token expired or invalid - get a new token

---

## 🎯 Pro Tips

1. **Auto-refresh in .http file:**
   ```
   @token = {{getToken.response.body.access_token}}
   ```
   Visual Studio auto-extracts the token from the response!

2. **Keep Identity Server running:**
   Keep `Hmm.Idp` running in a separate Visual Studio instance or terminal to avoid restarting it constantly.

3. **Multiple projects:**
   Right-click solution → Properties → Multiple Startup Projects:
   - Hmm.Idp: Start
   - Hmm.ServiceApi: Start

4. **Decode tokens:**
   Visit https://jwt.io and paste your token to see claims and expiration.

5. **Test different users:**
   Use different test users to verify role-based authorization works correctly.

---

## 📱 Quick Commands Reference

```powershell
# Get token for testuser (copy to clipboard)
.\Get-HmmApiToken.ps1 -User testuser -CopyToClipboard

# Get token for admin
.\Get-HmmApiToken.ps1 -User admin -CopyToClipboard

# Get token for machine-to-machine
.\Get-HmmApiToken.ps1 -User serviceapi -CopyToClipboard

# Check if Identity Server is running
curl https://localhost:5001/.well-known/openid-configuration --insecure

# Test token (replace with your token)
curl -H "Authorization: Bearer YOUR_TOKEN" https://localhost:5002/v1.0/authors
```

---

## 🎉 You're Ready!

**Easiest path:** Open `Hmm.ServiceApi.http` → Click ▶ on token request → Copy token → Paste in variable → Test APIs!

For more details, see **AUTHENTICATION_GUIDE.md**
