using Microsoft.AspNetCore.Http;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace Hmm.ServiceApi.Configuration;

/// <summary>
/// Selects the appropriate authentication scheme based on the JWT token issuer.
/// Supports multiple identity providers (Hmm.Idp, Firebase, Auth0, Azure AD).
/// </summary>
public static class MultiAuthSchemeSelector
{
    public const string MultiAuthScheme = "MultiAuth";
    public const string HmmIdpScheme = "HmmIdp";
    public const string FirebaseScheme = "Firebase";
    public const string Auth0Scheme = "Auth0";
    public const string AzureAdScheme = "AzureAd";

    /// <summary>
    /// Determines which authentication scheme to use based on the token's issuer claim.
    /// </summary>
    public static string SelectScheme(HttpContext context, ExternalAuthSettings externalAuth)
    {
        var authorization = context.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return HmmIdpScheme;
        }

        var token = authorization["Bearer ".Length..].Trim();

        try
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(token))
            {
                return HmmIdpScheme;
            }

            var jwtToken = handler.ReadJwtToken(token);
            var issuer = jwtToken.Issuer;

            // Check for Firebase token
            if (externalAuth?.EnableFirebase == true &&
                !string.IsNullOrEmpty(externalAuth.FirebaseProjectId) &&
                issuer.Contains("securetoken.google.com", StringComparison.OrdinalIgnoreCase))
            {
                return FirebaseScheme;
            }

            // Check for Auth0 token
            if (externalAuth?.EnableAuth0 == true &&
                !string.IsNullOrEmpty(externalAuth.Auth0Domain) &&
                issuer.Contains(externalAuth.Auth0Domain, StringComparison.OrdinalIgnoreCase))
            {
                return Auth0Scheme;
            }

            // Check for Azure AD token
            if (externalAuth?.EnableAzureAd == true &&
                !string.IsNullOrEmpty(externalAuth.AzureAdTenantId) &&
                issuer.Contains("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase))
            {
                return AzureAdScheme;
            }
        }
        catch
        {
            // If token parsing fails, fall back to default scheme
        }

        // Default to internal IDP
        return HmmIdpScheme;
    }
}
