# 🎯 How to Get Access Token - QUICK START

## ⚡ **EASIEST METHOD** (Takes 30 seconds)

### Using Visual Studio .http File:

1. **Open file:** `src/Hmm.ServiceApi/Hmm.ServiceApi.http`

2. **Scroll to line ~42** and find:
   ```http
   ### OPTION 1: Get Token - Test User (recommended for testing) ⭐
   # @name getToken
   POST {{idpUrl}}/connect/token
   ```

3. **Click the green play button (▶)** to the left of the request

4. **In the HTTP Response panel**, find and copy the `access_token` value:
   ```json
   {
     "access_token": "eyJhbGciOiJSUzI1NiIs...",  ← COPY THIS
     "expires_in": 3600,
     "token_type": "Bearer"
   }
   ```

5. **Scroll to line ~72** and paste:
   ```http
   @hmmIdpToken = eyJhbGciOiJSUzI1NiIs...  ← PASTE HERE
   ```

6. **Test any API endpoint!** Scroll down and click ▶ next to any request:
   ```http
   ### Get all notes
   GET {{baseUrl}}/api/v{{apiVersion}}/notes
   Authorization: Bearer {{token}}
   ```

**Done!** You're now authenticated and can test all endpoints.

---

## 🤖 **AUTOMATIC METHOD** (PowerShell Script)

```powershell
# Get token and copy to clipboard
.\Get-HmmApiToken.ps1 -User testuser -CopyToClipboard

# Then in .http file, paste:
# @hmmIdpToken = <Ctrl+V>
```

---

## 👥 **Available Test Users**

| Username | Password | Role |
|----------|----------|------|
| **testuser@hmm.local** ⭐ | `TestPassword123#` | User |
| alice | `Alice@12345678#` | User |
| bob | `Bob@123456789#` | User |
| admin@hmm.local | `Admin@12345678#` | Administrator |

**Recommendation:** Use `testuser@hmm.local` for most testing.

---

## ⚠️ **Prerequisites**

### Both Hmm.Idp AND Hmm.ServiceApi must be running!

#### Option A: Run Both in Visual Studio

1. Right-click **Solution** → **Properties**
2. Select **Multiple Startup Projects**
3. Set both to **Start**:
   - Hmm.Idp
   - Hmm.ServiceApi
4. Click OK and press **F5**

#### Option B: Run Separately

**Terminal 1:**
```powershell
cd src\Hmm.Idp
dotnet run
# Runs at https://localhost:5001
```

**Terminal 2:**
```powershell
cd src\Hmm.ServiceApi
dotnet run
# Runs at https://localhost:5002
```

#### Option C: Docker Full Stack

```powershell
docker-compose up -d
# Both services in containers
```

---

## 🧪 **Test It Works**

After getting a token, test with curl:

```powershell
# Replace <YOUR_TOKEN> with the actual token
curl -H "Authorization: Bearer <YOUR_TOKEN>" https://localhost:5002/api/v1.0/authors --insecure
```

**Expected:** JSON response with authors data

**If 401 Unauthorized:** 
- Token expired (get a new one)
- Wrong token endpoint URL
- API not configured correctly

---

## 📖 **Full Documentation**

- **TOKEN_CHEAT_SHEET.md** - Quick reference for all token methods
- **AUTHENTICATION_GUIDE.md** - Complete authentication documentation
- **ENVIRONMENT_SETUP.md** - Environment configuration guide

---

## 🎬 **Visual Studio Screenshot Guide**

### Where to Click:

```
┌─────────────────────────────────────────────────────────┐
│ Hmm.ServiceApi.http                                [×]  │
├─────────────────────────────────────────────────────────┤
│                                                          │
│ ### OPTION 1: Get Token - Test User ⭐                  │
│ # @name getToken                                        │
│ ▶ POST {{idpUrl}}/connect/token            ← CLICK HERE│
│ Content-Type: application/x-www-form-urlencoded         │
│                                                          │
│ grant_type=password&client_id=hmm.functest...           │
│                                                          │
└─────────────────────────────────────────────────────────┘

Response appears in HTTP Response panel ↓

┌─────────────────────────────────────────────────────────┐
│ HTTP Response                                      [×]  │
├─────────────────────────────────────────────────────────┤
│ HTTP/1.1 200 OK                                         │
│ Content-Type: application/json                          │
│                                                          │
│ {                                                        │
│   "access_token": "eyJhbGciOiJS...",  ← COPY THIS      │
│   "expires_in": 3600,                                   │
│   "token_type": "Bearer",                               │
│   "scope": "openid profile email hmmapi"               │
│ }                                                        │
└─────────────────────────────────────────────────────────┘

Then paste here ↓

┌─────────────────────────────────────────────────────────┐
│ @hmmIdpToken = eyJhbGciOiJS...         ← PASTE HERE    │
│                                                          │
│ @token = {{hmmIdpToken}}                                │
└─────────────────────────────────────────────────────────┘

Now test APIs ↓

┌─────────────────────────────────────────────────────────┐
│ ### Get all notes                                       │
│ ▶ GET {{baseUrl}}/api/v1.0/notes        ← CLICK TO TEST│
│ Authorization: Bearer {{token}}                         │
└─────────────────────────────────────────────────────────┘
```

---

## ⚡ **TL;DR - I Just Want a Token NOW**

```powershell
# Copy this to PowerShell:
.\Get-HmmApiToken.ps1 -User testuser -CopyToClipboard
```

Paste in .http file and start testing! 🚀

---

## 🆘 **Troubleshooting**

### "Cannot connect to https://localhost:5001"
→ **Identity Server (Hmm.Idp) is not running**
→ **Solution:** Start Hmm.Idp project in Visual Studio

### "Invalid username or password"
→ **Database not seeded**
→ **Solution:** Delete Idp database and restart (it will auto-seed)

### "401 Unauthorized" on API requests
→ **Token expired or invalid**
→ **Solution:** Get a fresh token (tokens expire after 1 hour)

### "Cannot find Get-HmmApiToken.ps1"
→ **Run from repository root directory**
→ **Solution:** `cd G:\Projects2\Hmm` then run script

---

## 🎓 **Learn More**

For detailed documentation:
- **AUTHENTICATION_GUIDE.md** - Full authentication documentation
- **TOKEN_CHEAT_SHEET.md** - All token generation methods
- **ENVIRONMENT_SETUP.md** - Environment and infrastructure setup

---

**You're all set! Happy testing!** 🎉
