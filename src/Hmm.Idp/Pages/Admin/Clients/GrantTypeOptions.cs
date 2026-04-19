namespace Hmm.Idp.Pages.Admin.Clients;

/// <summary>
/// Static list of grant types supported by the admin UI.
/// Values match Duende's <see cref="Duende.IdentityServer.IdentityServerConstants.StandardGrantTypes"/>.
/// </summary>
public static class GrantTypeOptions
{
    public static readonly List<string> All = new()
    {
        "authorization_code",    // Code flow
        "client_credentials",    // Client credentials
        "password",              // Resource owner password
        "refresh_token",         // Refresh token (combined with others)
        "implicit",              // Implicit
        "urn:ietf:params:oauth:grant-type:device_code", // Device flow
    };
}
