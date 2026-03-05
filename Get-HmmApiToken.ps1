# Get-HmmApiToken.ps1
# Quick script to get an access token for testing Hmm Service API

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("testuser", "alice", "bob", "admin", "serviceapi")]
    [string]$User = "testuser",
    
    [Parameter(Mandatory=$false)]
    [string]$IdpUrl = "https://localhost:5001",
    
    [switch]$CopyToClipboard
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Hmm API Token Generator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# User credentials from SeedDataService
$credentials = @{
    testuser = @{
        Username = "testuser@hmm.local"
        Password = "TestPassword123#"
        ClientId = "hmm.functest"
        ClientSecret = "FuncTestSecret123#"
        Description = "Test User"
    }
    alice = @{
        Username = "alice"
        Password = "Alice@12345678#"
        ClientId = "hmm.functest"
        ClientSecret = "FuncTestSecret123#"
        Description = "Alice Smith"
    }
    bob = @{
        Username = "bob"
        Password = "Bob@123456789#"
        ClientId = "hmm.functest"
        ClientSecret = "FuncTestSecret123#"
        Description = "Bob Smith"
    }
    admin = @{
        Username = "admin@hmm.local"
        Password = "Admin@12345678#"
        ClientId = "hmm.functest"
        ClientSecret = "FuncTestSecret123#"
        Description = "Administrator"
    }
    serviceapi = @{
        ClientId = "hmm.m2m"
        ClientSecret = "M2MSecret456#"
        Description = "Machine-to-Machine (Client Credentials)"
        IsM2M = $true
    }
}

$cred = $credentials[$User]
Write-Host "Getting token for: $($cred.Description)" -ForegroundColor Yellow
Write-Host "Identity Server: $IdpUrl" -ForegroundColor Gray
Write-Host ""

try {
    $tokenEndpoint = "$IdpUrl/connect/token"
    
    if ($cred.IsM2M) {
        # Client Credentials flow (machine-to-machine)
        $body = @{
            grant_type = "client_credentials"
            client_id = $cred.ClientId
            client_secret = $cred.ClientSecret
            scope = "hmmapi"
        }
    }
    else {
        # Resource Owner Password Credentials flow
        $body = @{
            grant_type = "password"
            client_id = $cred.ClientId
            client_secret = $cred.ClientSecret
            username = $cred.Username
            password = $cred.Password
            scope = "openid profile email hmmapi"
        }
    }
    
    # Ignore SSL certificate errors for localhost development
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $response = Invoke-RestMethod -Uri $tokenEndpoint -Method Post -Body $body -SkipCertificateCheck
    }
    else {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
        $response = Invoke-RestMethod -Uri $tokenEndpoint -Method Post -Body $body
    }
    
    $token = $response.access_token
    
    Write-Host "✓ Token obtained successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Access Token:" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $token -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Token expires in: $($response.expires_in) seconds ($([math]::Round($response.expires_in/60)) minutes)" -ForegroundColor Gray
    Write-Host ""
    
    if ($CopyToClipboard) {
        $token | Set-Clipboard
        Write-Host "✓ Token copied to clipboard!" -ForegroundColor Green
        Write-Host ""
    }
    
    Write-Host "Usage in .http file:" -ForegroundColor White
    Write-Host "  @token = $token" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Usage in Authorization header:" -ForegroundColor White
    Write-Host "  Authorization: Bearer $token" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Test with curl:" -ForegroundColor White
    Write-Host "  curl -H `"Authorization: Bearer $token`" https://localhost:5001/v1.0/notes" -ForegroundColor Gray
    Write-Host ""
    
    # Decode token to show claims (requires jq or manual parsing)
    Write-Host "Tip: To copy token to clipboard, run:" -ForegroundColor Cyan
    Write-Host "  .\Get-HmmApiToken.ps1 -User $User -CopyToClipboard" -ForegroundColor Cyan
    Write-Host ""
    
}
catch {
    Write-Host "✗ Failed to get token" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Ensure Identity Server is running at $IdpUrl" -ForegroundColor White
    Write-Host "  2. Check that the database is seeded with test users" -ForegroundColor White
    Write-Host "  3. Verify the IdP URL is correct" -ForegroundColor White
    Write-Host ""
    exit 1
}
