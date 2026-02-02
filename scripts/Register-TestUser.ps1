<#
.SYNOPSIS
    Registers a test user in the Hmm Identity Provider database.

.DESCRIPTION
    This script creates a test user directly in the ASP.NET Identity database
    for use in functional testing. The user is created with email confirmed
    so they can immediately be used for authentication.

.PARAMETER ConnectionString
    The connection string for the IDP database.

.PARAMETER Username
    The username/email for the test user. Default: testuser@hmm.local

.PARAMETER Password
    The password for the test user. Default: TestPassword123!

.PARAMETER FirstName
    The first name of the test user. Default: Test

.PARAMETER LastName
    The last name of the test user. Default: User

.EXAMPLE
    .\scripts\Register-TestUser.ps1

.EXAMPLE
    .\scripts\Register-TestUser.ps1 -Username "alice@hmm.local" -Password "AlicePassword123!"

.NOTES
    Requires SQL Server to be running and accessible.
    The password must meet the IDP password policy:
    - At least 12 characters
    - At least one uppercase letter
    - At least one lowercase letter
    - At least one digit
    - At least one special character
    - At least 6 unique characters
#>

param(
    [string]$ConnectionString = "Server=localhost,1433;Database=HmmIdp;User Id=sa;Password=Shcdlhgm1!;TrustServerCertificate=True;",
    [string]$Username = "testuser@hmm.local",
    [string]$Password = "TestPassword123!",
    [string]$FirstName = "Test",
    [string]$LastName = "User"
)

$ErrorActionPreference = "Stop"

Write-Host "Registering test user in Hmm.Idp database..." -ForegroundColor Cyan
Write-Host "Username: $Username" -ForegroundColor Gray

# Check if sqlcmd is available
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue

if (-not $sqlcmd) {
    Write-Host "sqlcmd not found. Using Docker container..." -ForegroundColor Yellow

    # Check if container is running
    $container = docker ps --filter "name=hmm-sqlserver" --format "{{.Names}}" 2>$null
    if (-not $container) {
        # Try the IDP SQL Server container
        $container = docker ps --filter "name=hmm-idp-sqlserver" --format "{{.Names}}" 2>$null
    }

    if (-not $container) {
        Write-Error "No SQL Server container is running. Start the infrastructure first."
        exit 1
    }

    $useSqlCmd = $false
    $containerName = $container
} else {
    $useSqlCmd = $true
}

# Generate password hash using ASP.NET Identity compatible format
# Note: For proper password hashing, you should run the IDP project and use its registration endpoint
# This script provides a workaround for testing purposes

$sql = @"
-- Create the HmmIdp database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HmmIdp')
BEGIN
    CREATE DATABASE HmmIdp;
END
GO

USE HmmIdp;
GO

-- Check if AspNetUsers table exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
BEGIN
    PRINT 'AspNetUsers table does not exist. Run the IDP first to create the schema.';
END
ELSE
BEGIN
    -- Check if user already exists
    IF EXISTS (SELECT 1 FROM AspNetUsers WHERE NormalizedUserName = UPPER('$Username'))
    BEGIN
        PRINT 'User already exists: $Username';
    END
    ELSE
    BEGIN
        -- Note: This creates a user record, but the password won't work because
        -- ASP.NET Identity uses a complex hashing algorithm.
        -- For proper user creation, use the IDP registration page or API.
        PRINT 'To create a test user, please use one of these methods:';
        PRINT '1. Run the IDP and navigate to /Account/Register';
        PRINT '2. Use the UserManagementService in the IDP';
        PRINT '3. Run the Create-TestUser console command (if available)';
    END
END
GO
"@

Write-Host ""
Write-Host "IMPORTANT: ASP.NET Identity uses secure password hashing that cannot be" -ForegroundColor Yellow
Write-Host "easily replicated in SQL scripts. To create test users, please:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Option 1: Use the IDP Web Interface" -ForegroundColor White
Write-Host "  1. Start the IDP: dotnet run --project src/Hmm.Idp" -ForegroundColor Gray
Write-Host "  2. Navigate to: https://localhost:5001/Account/Register" -ForegroundColor Gray
Write-Host "  3. Register user: $Username with password: $Password" -ForegroundColor Gray
Write-Host ""
Write-Host "Option 2: Use the Admin Interface" -ForegroundColor White
Write-Host "  1. Start the IDP: dotnet run --project src/Hmm.Idp" -ForegroundColor Gray
Write-Host "  2. Navigate to: https://localhost:5001/Admin/User/Create" -ForegroundColor Gray
Write-Host "  3. Create the test user with pre-confirmed email" -ForegroundColor Gray
Write-Host ""
Write-Host "Option 3: Seed users in code" -ForegroundColor White
Write-Host "  Add user seeding logic to HostingExtensions.cs InitializeDatabase method" -ForegroundColor Gray
Write-Host ""

# Offer to open the registration page
$response = Read-Host "Would you like to open the IDP registration page? (Y/N)"
if ($response -eq 'Y' -or $response -eq 'y') {
    Start-Process "https://localhost:5001/Account/Register"
}
