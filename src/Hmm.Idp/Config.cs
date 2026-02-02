using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Hmm.Idp;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email()
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope("hmmapi", "Hmm API")
        {
            UserClaims = ["name", "email", "role"]
        }
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource("hmmapi", "Hmm API")
        {
            Scopes = { "hmmapi" },
            UserClaims = ["name", "email", "role"]
        }
    ];

    public static IEnumerable<Client> Clients =>
    [
        // Functional Testing Client - Resource Owner Password Grant
        // Use this for automated testing and scripts
        new Client
        {
            ClientId = "hmm.functest",
            ClientName = "Hmm Functional Testing Client",
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            RequireClientSecret = true,
            ClientSecrets =
            {
                new Secret("FuncTestSecret123!".Sha256())
            },
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                IdentityServerConstants.StandardScopes.Email,
                "hmmapi"
            },
            AccessTokenLifetime = 3600, // 1 hour
            AllowOfflineAccess = true,
            RefreshTokenUsage = TokenUsage.ReUse,
            RefreshTokenExpiration = TokenExpiration.Sliding,
            SlidingRefreshTokenLifetime = 86400 // 24 hours
        },

        // Machine-to-Machine Client - Client Credentials Grant
        // Use this for service-to-service communication
        new Client
        {
            ClientId = "hmm.m2m",
            ClientName = "Hmm Machine-to-Machine Client",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            RequireClientSecret = true,
            ClientSecrets =
            {
                new Secret("M2MSecret456!".Sha256())
            },
            AllowedScopes =
            {
                "hmmapi"
            },
            AccessTokenLifetime = 3600
        },

        // Interactive Web Client - Authorization Code with PKCE
        // Use this for web applications
        new Client
        {
            ClientId = "hmm.web",
            ClientName = "Hmm Web Application",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = true,
            ClientSecrets =
            {
                new Secret("WebSecret789!".Sha256())
            },
            RedirectUris =
            {
                "https://localhost:5002/signin-oidc",
                "https://localhost:44342/signin-oidc"
            },
            PostLogoutRedirectUris =
            {
                "https://localhost:5002/signout-callback-oidc",
                "https://localhost:44342/signout-callback-oidc"
            },
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                IdentityServerConstants.StandardScopes.Email,
                "hmmapi"
            },
            AllowOfflineAccess = true,
            UpdateAccessTokenClaimsOnRefresh = true
        },

        // Hmm.ServiceApi Client - For API token validation and introspection
        // Use this for the ServiceApi to validate tokens
        new Client
        {
            ClientId = "hmm.serviceapi",
            ClientName = "Hmm Service API",
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            RequireClientSecret = true,
            ClientSecrets =
            {
                new Secret("ServiceApiSecret!@#456".Sha256())
            },
            AllowedScopes =
            {
                "hmmapi"
            },
            AccessTokenLifetime = 3600,
            // Allow this client to introspect tokens
            Properties =
            {
                { "AllowTokenIntrospection", "true" }
            }
        }
    ];
}