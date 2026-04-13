using System.ComponentModel.DataAnnotations;

namespace Hmm.Idp.Pages.Admin.Roles;

public class ViewModel
{
    public string Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Role Name")]
    public string Name { get; set; }

    [StringLength(500)]
    [Display(Name = "Description")]
    public string Description { get; set; }
}
