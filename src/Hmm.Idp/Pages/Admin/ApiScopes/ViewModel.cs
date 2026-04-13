using System.ComponentModel.DataAnnotations;

namespace Hmm.Idp.Pages.Admin.ApiScopes;

public class ViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; }

    [StringLength(200)]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; }

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string Description { get; set; }

    [Display(Name = "Enabled")]
    public bool Enabled { get; set; } = true;

    [Display(Name = "Show In Discovery Document")]
    public bool ShowInDiscoveryDocument { get; set; } = true;

    [Display(Name = "Required")]
    public bool Required { get; set; }

    [Display(Name = "Emphasize (highlight on consent screen)")]
    public bool Emphasize { get; set; }

    [Display(Name = "User Claims (comma separated)")]
    public string UserClaims { get; set; }
}
