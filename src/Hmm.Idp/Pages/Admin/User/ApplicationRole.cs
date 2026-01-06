using Microsoft.AspNetCore.Identity;

namespace Hmm.Idp.Pages.Admin.User;

public class ApplicationRole : IdentityRole
{
    public string Description { get; set; }
}