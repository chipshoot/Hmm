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

    [Display(Name = "Allow Offline Access")]
    public bool AllowOfflineAccess { get; set; }

    [Display(Name = "Update Access Token Claims On Refresh")]
    public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

    [Display(Name = "Require PKCE")]
    public bool RequirePkce { get; set; } = true;

    [Required]
    [Display(Name = "Client Secret")]
    public string ClientSecret { get; set; }
    
    [Display(Name = "Redirect URIs (one per line)")]
    public string RedirectUris { get; set; }
    
    [Display(Name = "Post-Logout Redirect URIs (one per line)")]
    public string PostLogoutRedirectUris { get; set; }

    [Display(Name = "Allowed Scopes (comma separated)")]
    public string AllowedScopes { get; set; }
}
