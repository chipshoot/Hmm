<#
.SYNOPSIS
    Gets a test access token from the Hmm Identity Provider.

.DESCRIPTION
    This script authenticates against the Hmm.Idp and returns a JWT access token
    that can be used for API testing.

.PARAMETER Username
    The username to authenticate with. Default: testuser@hmm.local

.PARAMETER Password
    The password for the user. Default: TestPassword123#

.PARAMETER IdpUrl
    The URL of the Identity Provider. Default: https://localhost:5001

.PARAMETER ClientId
    The client ID to use. Default: hmm.functest

.PARAMETER ClientSecret
    The client secret. Default: FuncTestSecret123#

.PARAMETER Scope
    The scopes to request. Default: openid profile email hmmapi

.PARAMETER GrantType
    The grant type to use: 'password' or 'client_credentials'. Default: password

.EXAMPLE
    .\scripts\Get-TestToken.ps1
    # Gets a token using default credentials

.EXAMPLE
    .\scripts\Get-TestToken.ps1 -Username "alice@hmm.local" -Password "AlicePassword123!"
    # Gets a token for a specific user

.EXAMPLE
    .\scripts\Get-TestToken.ps1 -GrantType client_credentials
    # Gets a token using client credentials (no user)

.EXAMPLE
    $token = .\scripts\Get-TestToken.ps1 -Raw
    # Gets just the token string (no formatting)

.NOTES
    Requires the Hmm.Idp to be running and a test user to be registered.
#>

param(
    [string]$Username = "testuser@hmm.local",
    [string]$Password = "TestPassword123#",
    [string]$IdpUrl = "https://localhost:5001",
    [string]$ClientId = "hmm.functest",
    [string]$ClientSecret = "FuncTestSecret123#",
    [string]$Scope = "openid profile email hmmapi",
    [ValidateSet("password", "client_credentials")]
    [string]$GrantType = "password",
    [switch]$Raw = $false,
    [switch]$SkipCertCheck = $true
)

$ErrorActionPreference = "Stop"

# Skip certificate validation for local development
if ($SkipCertCheck) {
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $null = [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
    } else {
        Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
        [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    }
}

$tokenEndpoint = "$IdpUrl/connect/token"

# Build request body based on grant type
$body = @{
    client_id     = $ClientId
    client_secret = $ClientSecret
    grant_type    = $GrantType
    scope         = $Scope
}

if ($GrantType -eq "password") {
    $body.username = $Username
    $body.password = $Password
}

if (-not $Raw) {
    Write-Host "Requesting token from: $tokenEndpoint" -ForegroundColor Cyan
    Write-Host "Grant Type: $GrantType" -ForegroundColor Gray
    if ($GrantType -eq "password") {
        Write-Host "Username: $Username" -ForegroundColor Gray
    }
}

try {
    $params = @{
        Uri         = $tokenEndpoint
        Method      = "POST"
        Body        = $body
        ContentType = "application/x-www-form-urlencoded"
    }

    # Handle certificate validation for PowerShell 6+
    if ($SkipCertCheck -and $PSVersionTable.PSVersion.Major -ge 6) {
        $params.SkipCertificateCheck = $true
    }

    $response = Invoke-RestMethod @params

    if ($Raw) {
        Write-Output $response.access_token
    } else {
        Write-Host ""
        Write-Host "Token acquired successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Access Token:" -ForegroundColor Yellow
        Write-Host $response.access_token -ForegroundColor White
        Write-Host ""
        Write-Host "Token Type: $($response.token_type)" -ForegroundColor Gray
        Write-Host "Expires In: $($response.expires_in) seconds" -ForegroundColor Gray
        if ($response.refresh_token) {
            Write-Host "Refresh Token: $($response.refresh_token)" -ForegroundColor Gray
        }
        Write-Host ""
        Write-Host "Copy this for use in HTTP file or Postman:" -ForegroundColor Yellow
        Write-Host "Authorization: Bearer $($response.access_token)" -ForegroundColor Cyan
    }
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host ""
    Write-Host "Failed to get token!" -ForegroundColor Red
    Write-Host "Status Code: $statusCode" -ForegroundColor Red

    try {
        $errorBody = $_.ErrorDetails.Message | ConvertFrom-Json
        Write-Host "Error: $($errorBody.error)" -ForegroundColor Red
        if ($errorBody.error_description) {
            Write-Host "Description: $($errorBody.error_description)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "Error: $_" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Ensure Hmm.Idp is running: dotnet run --project src/Hmm.Idp" -ForegroundColor Gray
    Write-Host "2. Ensure the test user exists (run Register-TestUser.ps1)" -ForegroundColor Gray
    Write-Host "3. Check if client is configured in IDP database" -ForegroundColor Gray

    exit 1
}
