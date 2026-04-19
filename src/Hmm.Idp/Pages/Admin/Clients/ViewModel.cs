// Models/ClientViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Hmm.Idp.Pages.Admin.Clients;

public class ViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Client ID")]
    public string ClientId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Client Name")]
    public string ClientName { get; set; }

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string Description { get; set; }

    [Display(Name = "Enabled")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "Allowed Grant Types")]
    public List<string> AllowedGrantTypes { get; set; } = new();

    [Display(Name = "Allow Offline Access")]
    public bool AllowOfflineAccess { get; set; }

    [Display(Name = "Update Access Token Claims On Refresh")]
    public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

    [Display(Name = "Require PKCE")]
    public bool RequirePkce { get; set; } = true;

    [Display(Name = "Client Secret (leave blank to keep current)")]
    public string ClientSecret { get; set; }

    [Display(Name = "Access Token Lifetime (seconds)")]
    [Range(60, int.MaxValue)]
    public int AccessTokenLifetime { get; set; } = 3600;

    [Display(Name = "Identity Token Lifetime (seconds)")]
    [Range(60, int.MaxValue)]
    public int IdentityTokenLifetime { get; set; } = 300;

    [Display(Name = "Absolute Refresh Token Lifetime (seconds)")]
    [Range(60, int.MaxValue)]
    public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;

    [Display(Name = "Sliding Refresh Token Lifetime (seconds)")]
    [Range(60, int.MaxValue)]
    public int SlidingRefreshTokenLifetime { get; set; } = 1296000;

    [Display(Name = "Redirect URIs (one per line)")]
    public string RedirectUris { get; set; }

    [Display(Name = "Post-Logout Redirect URIs (one per line)")]
    public string PostLogoutRedirectUris { get; set; }

    [Display(Name = "Allowed Scopes (comma separated)")]
    public string AllowedScopes { get; set; }

    [Display(Name = "Allowed CORS Origins (one per line)")]
    public string AllowedCorsOrigins { get; set; }

    [Display(Name = "Client Claims (one per line, format: type=value)")]
    public string ClientClaims { get; set; }
}
