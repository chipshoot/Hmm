using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Hmm.Contract;

namespace Hmm.Idp;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
            new IdentityResources.Profile()
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
        [];

    public static IEnumerable<Client> Clients =>
    [
        new()
        {
                ClientName = HmmConstants.HmmWebConsoleName,
                ClientId = HmmConstants.HmmWebConsoleId,
                AllowOfflineAccess = true,
                UpdateAccessTokenClaimsOnRefresh = true,
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RedirectUris = new List<string>
                {
                    "https://localhost:44342/signin-oidc"
                },
                PostLogoutRedirectUris = new List<string>
                {
                    "https://localhost:44342/signout-callback-oidc"
                },
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Address,
                    "roles",
                    HmmConstants.HmmApiId
                },
                ClientSecrets =
                {
                    new Secret("secret".Sha256())
                }
            }
    ];
}