-- Database Initialization Script for Hmm.Idp
-- This script creates the database if it doesn't exist
-- Run this before starting the application

USE master;
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HmmIdp')
BEGIN
    CREATE DATABASE [HmmIdp];
    PRINT 'Database HmmIdp created successfully.';
END
ELSE
BEGIN
    PRINT 'Database HmmIdp already exists.';
END
GO

USE [HmmIdp];
GO

PRINT 'Database initialization complete.';
GO

-- ============================================================================
-- Seed IdentityServer Configuration Data
-- These INSERT statements mirror the data previously in Config.cs
-- All inserts use IF NOT EXISTS guards for idempotency
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Identity Resources: openid, profile, email
-- ----------------------------------------------------------------------------

-- openid
IF NOT EXISTS (SELECT 1 FROM IdentityResources WHERE [Name] = 'openid')
BEGIN
    INSERT INTO IdentityResources ([Enabled], [Name], [DisplayName], [Description], [Required], [Emphasize], [ShowInDiscoveryDocument], [Created], [NonEditable])
    VALUES (1, 'openid', 'Your user identifier', NULL, 1, 0, 1, GETUTCDATE(), 0);

    DECLARE @openidId INT = SCOPE_IDENTITY();

    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@openidId, 'sub');

    PRINT 'Seeded IdentityResource: openid';
END
GO

-- profile
IF NOT EXISTS (SELECT 1 FROM IdentityResources WHERE [Name] = 'profile')
BEGIN
    INSERT INTO IdentityResources ([Enabled], [Name], [DisplayName], [Description], [Required], [Emphasize], [ShowInDiscoveryDocument], [Created], [NonEditable])
    VALUES (1, 'profile', 'User profile', 'Your user profile information (first name, last name, etc.)', 0, 1, 1, GETUTCDATE(), 0);

    DECLARE @profileId INT = SCOPE_IDENTITY();

    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'name');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'family_name');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'given_name');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'middle_name');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'nickname');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'preferred_username');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'profile');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'picture');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'website');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'gender');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'birthdate');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'zoneinfo');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'locale');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@profileId, 'updated_at');

    PRINT 'Seeded IdentityResource: profile';
END
GO

-- email
IF NOT EXISTS (SELECT 1 FROM IdentityResources WHERE [Name] = 'email')
BEGIN
    INSERT INTO IdentityResources ([Enabled], [Name], [DisplayName], [Description], [Required], [Emphasize], [ShowInDiscoveryDocument], [Created], [NonEditable])
    VALUES (1, 'email', 'Your email address', NULL, 0, 1, 1, GETUTCDATE(), 0);

    DECLARE @emailId INT = SCOPE_IDENTITY();

    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@emailId, 'email');
    INSERT INTO IdentityResourceClaims ([IdentityResourceId], [Type]) VALUES (@emailId, 'email_verified');

    PRINT 'Seeded IdentityResource: email';
END
GO

-- ----------------------------------------------------------------------------
-- API Scopes: hmmapi
-- ----------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM ApiScopes WHERE [Name] = 'hmmapi')
BEGIN
    INSERT INTO ApiScopes ([Enabled], [Name], [DisplayName], [Description], [Required], [Emphasize], [ShowInDiscoveryDocument], [Created], [NonEditable])
    VALUES (1, 'hmmapi', 'Hmm API', NULL, 0, 0, 1, GETUTCDATE(), 0);

    DECLARE @hmmapiScopeId INT = SCOPE_IDENTITY();

    INSERT INTO ApiScopeClaims ([ScopeId], [Type]) VALUES (@hmmapiScopeId, 'name');
    INSERT INTO ApiScopeClaims ([ScopeId], [Type]) VALUES (@hmmapiScopeId, 'email');
    INSERT INTO ApiScopeClaims ([ScopeId], [Type]) VALUES (@hmmapiScopeId, 'role');

    PRINT 'Seeded ApiScope: hmmapi';
END
GO

-- ----------------------------------------------------------------------------
-- API Resources: hmmapi
-- ----------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM ApiResources WHERE [Name] = 'hmmapi')
BEGIN
    INSERT INTO ApiResources ([Enabled], [Name], [DisplayName], [Description], [AllowedAccessTokenSigningAlgorithms], [ShowInDiscoveryDocument], [RequireResourceIndicator], [Created], [NonEditable])
    VALUES (1, 'hmmapi', 'Hmm API', NULL, NULL, 1, 0, GETUTCDATE(), 0);

    DECLARE @hmmapiResourceId INT = SCOPE_IDENTITY();

    INSERT INTO ApiResourceScopes ([ApiResourceId], [Scope]) VALUES (@hmmapiResourceId, 'hmmapi');

    INSERT INTO ApiResourceClaims ([ApiResourceId], [Type]) VALUES (@hmmapiResourceId, 'name');
    INSERT INTO ApiResourceClaims ([ApiResourceId], [Type]) VALUES (@hmmapiResourceId, 'email');
    INSERT INTO ApiResourceClaims ([ApiResourceId], [Type]) VALUES (@hmmapiResourceId, 'role');

    PRINT 'Seeded ApiResource: hmmapi';
END
GO

-- ----------------------------------------------------------------------------
-- Clients
-- ----------------------------------------------------------------------------
-- Secret hashes are Base64-encoded SHA256, matching Duende's .Sha256() extension.
-- Computed via: SHA256(UTF8(secret)) -> Base64 string
--
-- FuncTestSecret123!     -> WV8Cywlu2jFiCqDIPSwttp6FlkGfaQ/J49N/XEeYK9E=
-- M2MSecret456!          -> SReDVYb6SZ1mIlJRxE5RW4WJTpL/WUqEpKAhwJJ0FDk=
-- WebSecret789!          -> OWfNilXDtNbH89AVTT3FvInWETVlUnlsR7VKlIJ6OdQ=
-- ServiceApiSecret!@#456 -> kaf0/dH1UOF8QWzWxeSnH/j8WUjLEt6OuTKvaSvzpU8=

-- ---- Client: hmm.functest (Resource Owner Password Grant) ----
IF NOT EXISTS (SELECT 1 FROM Clients WHERE [ClientId] = 'hmm.functest')
BEGIN
    INSERT INTO Clients (
        [Enabled], [ClientId], [ProtocolType], [RequireClientSecret], [ClientName],
        [RequireConsent], [AllowRememberConsent], [AlwaysIncludeUserClaimsInIdToken],
        [RequirePkce], [AllowPlainTextPkce], [RequireRequestObject],
        [AllowAccessTokensViaBrowser], [FrontChannelLogoutSessionRequired],
        [BackChannelLogoutSessionRequired], [AllowOfflineAccess],
        [IdentityTokenLifetime], [AccessTokenLifetime], [AuthorizationCodeLifetime],
        [AbsoluteRefreshTokenLifetime], [SlidingRefreshTokenLifetime],
        [RefreshTokenUsage], [UpdateAccessTokenClaimsOnRefresh],
        [RefreshTokenExpiration], [AccessTokenType],
        [EnableLocalLogin], [IncludeJwtId], [AlwaysSendClientClaims],
        [ClientClaimsPrefix], [DeviceCodeLifetime],
        [DPoPClockSkew], [DPoPValidationMode], [RequireDPoP], [RequirePushedAuthorization],
        [Created], [NonEditable]
    ) VALUES (
        1, 'hmm.functest', 'oidc', 1, 'Hmm Functional Testing Client',
        0, 1, 0,
        1, 0, 0,
        0, 1,
        1, 1,                          -- AllowOfflineAccess = true
        300, 3600, 300,                -- Token lifetimes
        2592000, 86400,                -- AbsoluteRefreshTokenLifetime=30d, SlidingRefreshTokenLifetime=24h
        0, 0,                          -- RefreshTokenUsage=ReUse, UpdateAccessTokenClaimsOnRefresh=false
        1, 0,                          -- RefreshTokenExpiration=Sliding, AccessTokenType=Jwt
        1, 0, 0,
        'client_', 300,
        '00:00:00', 0, 0, 0,
        GETUTCDATE(), 0
    );

    DECLARE @funcTestId INT = SCOPE_IDENTITY();

    INSERT INTO ClientGrantTypes ([ClientId], [GrantType]) VALUES (@funcTestId, 'password');

    INSERT INTO ClientSecrets ([ClientId], [Value], [Type], [Created])
    VALUES (@funcTestId, 'WV8Cywlu2jFiCqDIPSwttp6FlkGfaQ/J49N/XEeYK9E=', 'SharedSecret', GETUTCDATE());

    INSERT INTO ClientScopes ([ClientId], [Scope]) VALUES (@funcTestId, 'openid');
    INSERT INTO ClientScopes ([ClientId], [Scope]) VALUES (@funcTestId, 'profile');
    INSERT INTO ClientScopes ([ClientId], [Scope]) VALUES (@funcTestId, 'email');
    INSERT INTO ClientScopes ([ClientId], [Scope]) VALUES (@funcTestId, 'hmmapi');

    PRINT 'Seeded Client: hmm.functest';
END
GO

-- ---- Client: hmm.m2m (Client Credentials Grant) ----
IF NOT EXISTS (SELECT 1 FROM Clients WHERE [ClientId] = 'hmm.m2m')
BEGIN
    INSERT INTO Clients (
        [Enabled], [ClientId], [ProtocolType], [RequireClientSecret], [ClientName],
        [RequireConsent], [AllowRememberConsent], [AlwaysIncludeUserClaimsInIdToken],
        [RequirePkce], [AllowPlainTextPkce], [RequireRequestObject],
        [AllowAccessTokensViaBrowser], [FrontChannelLogoutSessionRequired],
        [BackChannelLogoutSessionRequired], [AllowOfflineAccess],
        [IdentityTokenLifetime], [AccessTokenLifetime], [AuthorizationCodeLifetime],
        [AbsoluteRefreshTokenLifetime], [SlidingRefreshTokenLifetime],
        [RefreshTokenUsage], [UpdateAccessTokenClaimsOnRefresh],
        [RefreshTokenExpiration], [AccessTokenType],
        [EnableLocalLogin], [IncludeJwtId], [AlwaysSendClientClaims],
        [ClientClaimsPrefix], [DeviceCodeLifetime],
        [DPoPClockSkew], [DPoPValidationMode], [RequireDPoP], [RequirePushedAuthorization],
        [Created], [NonEditable]
    ) VALUES (
        1, 'hmm.m2m', 'oidc', 1, 'Hmm Machine-to-Machine Client',
        0, 1, 0,
        1, 0, 0,
        0, 1,
        1, 0,                          -- AllowOfflineAccess = false (default)
        300, 3600, 300,                -- Token lifetimes
        2592000, 1296000,              -- Default refresh token lifetimes
        0, 0,                          -- RefreshTokenUsage=ReUse, UpdateAccessTokenClaimsOnRefresh=false
        0, 0,                          -- RefreshTokenExpiration=Absolute (default), AccessTokenType=Jwt
        1, 0, 0,
        'client_', 300,
        '00:00:00', 0, 0, 0,
        GETUTCDATE(), 0
    );

    DECLARE @m2mId INT = SCOPE_IDENTITY();

    INSERT INTO ClientGrantTypes ([ClientId], [GrantType]) VALUES (@m2mId, 'client_credentials');

    INSERT INTO ClientSecrets ([ClientId], [Value], [Type], [Created])
    VALUES (@m2mId, 'SReDVYb6SZ1mIlJRxE5RW4WJTpL/WUqEpKAhwJJ0FDk=', 'SharedSecret', GETUTCDATE());

    INSERT INTO ClientScopes ([ClientId], [Scope]) VALUES (@m2mId, 'hmmapi');

    PRINT 'Seeded Client: hmm.m2m';
END
GO

-- ---- Client: hmm.web (Authorization Code with PKCE) ----
IF NOT EXISTS (SELECT 1 FROM Clients WHERE [ClientId] = 'hmm.web')
BEGIN
    INSERT INTO Clients (
        [Enabled], [ClientId], [ProtocolType], [RequireClientSecret], [ClientName],
        [RequireConsent], [AllowRememberConsent], [AlwaysIncludeUserClaimsInIdToken],
        [RequirePkce], [AllowPlainTextPkce], [RequireRequestObject],
        [AllowAccessTokensViaBrowser], [FrontChannelLogoutSessionRequired],
        [BackChannelLogoutSessionRequired], [AllowOfflineAccess],
        [IdentityTokenLifetime], [AccessTokenLifetime], [AuthorizationCodeLifetime],
        [AbsoluteRefreshTokenLifetime], [SlidingRefreshTokenLifetime],
        [RefreshTokenUsage], [UpdateAccessTokenClaimsOnRefresh],
        [RefreshTokenExpiration], [AccessTokenType],
        [EnableLocalLogin], [IncludeJwtId], [AlwaysSendClientClaims],
        [ClientClaimsPrefix], [DeviceCodeLifetime],
        [DPoPClockSkew], [DPoPValidationMode], [RequireDPoP], [RequirePushedAuthorization],
        [Created], [NonEditable]
    ) VALUES (
        1, 'hmm.web', 'oidc', 1, 'Hmm Web Application',
        0, 1, 0,
        1, 0, 0,                       -- RequirePkce = true
        0, 1,
        1, 1,                          -- AllowOfflineAccess = true
        300, 3600, 300,                -- Token lifetimes
        2592000, 1296000,              -- Default refresh token lifetimes
        0, 1,                          -- RefreshTokenUsage=ReUse, UpdateAccessTokenClaimsOnRefresh=true
        0, 0,                          -- RefreshTokenExpiration=Absolute (default), AccessTokenType=Jwt
        1, 0, 0,
        'client_', 300,
        '00:00:00', 0, 0, 0,
        GETUTCDATE(), 0
    );

    DECLARE @webId INT = SCOPE_IDENTITY();

    INSERT INTO ClientGrantTypes ([ClientId], [GrantType]) VALUES (@webId, 'authorization_code');

    INSERT INTO ClientSecrets ([ClientId], [Value], [Type], [Created])
    VALUES (@webId, 'OWfNilXDtNbH89AVTT3FvInWETVlUnlsR7VKlIJ6OdQ=', 'SharedSecret', GETUTCDATE());

    INSERT INTO ClientScopes ([ClientId], [Scope]) VALUES (@webId, 'openid');
    INSERT INTO ClientScopes ([ClientId], [Scope]) VALUES (@webId, 'profile');
    INSERT INTO ClientScopes ([ClientId], [Scope]) VALUES (@webId, 'email');
    INSERT INTO ClientScopes ([ClientId], [Scope]) VALUES (@webId, 'hmmapi');

    INSERT INTO ClientRedirectUris ([ClientId], [RedirectUri]) VALUES (@webId, 'https://localhost:5002/signin-oidc');
    INSERT INTO ClientRedirectUris ([ClientId], [RedirectUri]) VALUES (@webId, 'https://localhost:44342/signin-oidc');

    INSERT INTO ClientPostLogoutRedirectUris ([ClientId], [PostLogoutRedirectUri]) VALUES (@webId, 'https://localhost:5002/signout-callback-oidc');
    INSERT INTO ClientPostLogoutRedirectUris ([ClientId], [PostLogoutRedirectUri]) VALUES (@webId, 'https://localhost:44342/signout-callback-oidc');

    PRINT 'Seeded Client: hmm.web';
END
GO

-- ---- Client: hmm.serviceapi (Client Credentials + Token Introspection) ----
IF NOT EXISTS (SELECT 1 FROM Clients WHERE [ClientId] = 'hmm.serviceapi')
BEGIN
    INSERT INTO Clients (
        [Enabled], [ClientId], [ProtocolType], [RequireClientSecret], [ClientName],
        [RequireConsent], [AllowRememberConsent], [AlwaysIncludeUserClaimsInIdToken],
        [RequirePkce], [AllowPlainTextPkce], [RequireRequestObject],
        [AllowAccessTokensViaBrowser], [FrontChannelLogoutSessionRequired],
        [BackChannelLogoutSessionRequired], [AllowOfflineAccess],
        [IdentityTokenLifetime], [AccessTokenLifetime], [AuthorizationCodeLifetime],
        [AbsoluteRefreshTokenLifetime], [SlidingRefreshTokenLifetime],
        [RefreshTokenUsage], [UpdateAccessTokenClaimsOnRefresh],
        [RefreshTokenExpiration], [AccessTokenType],
        [EnableLocalLogin], [IncludeJwtId], [AlwaysSendClientClaims],
        [ClientClaimsPrefix], [DeviceCodeLifetime],
        [DPoPClockSkew], [DPoPValidationMode], [RequireDPoP], [RequirePushedAuthorization],
        [Created], [NonEditable]
    ) VALUES (
        1, 'hmm.serviceapi', 'oidc', 1, 'Hmm Service API',
        0, 1, 0,
        1, 0, 0,
        0, 1,
        1, 0,                          -- AllowOfflineAccess = false (default)
        300, 3600, 300,                -- Token lifetimes
        2592000, 1296000,              -- Default refresh token lifetimes
        0, 0,                          -- RefreshTokenUsage=ReUse, UpdateAccessTokenClaimsOnRefresh=false
        0, 0,                          -- RefreshTokenExpiration=Absolute (default), AccessTokenType=Jwt
        1, 0, 0,
        'client_', 300,
        '00:00:00', 0, 0, 0,
        GETUTCDATE(), 0
    );

    DECLARE @serviceApiId INT = SCOPE_IDENTITY();

    INSERT INTO ClientGrantTypes ([ClientId], [GrantType]) VALUES (@serviceApiId, 'client_credentials');

    INSERT INTO ClientSecrets ([ClientId], [Value], [Type], [Created])
    VALUES (@serviceApiId, 'kaf0/dH1UOF8QWzWxeSnH/j8WUjLEt6OuTKvaSvzpU8=', 'SharedSecret', GETUTCDATE());

    INSERT INTO ClientScopes ([ClientId], [Scope]) VALUES (@serviceApiId, 'hmmapi');

    INSERT INTO ClientProperties ([ClientId], [Key], [Value]) VALUES (@serviceApiId, 'AllowTokenIntrospection', 'true');

    PRINT 'Seeded Client: hmm.serviceapi';
END
GO

PRINT 'IdentityServer configuration seeding complete.';
GO
