using Hmm.Idp.Pages.Admin.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class DetailsModel : PageModel
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public ApplicationRole Role { get; set; }
    public IList<ApplicationUser> UsersInRole { get; set; } = new List<ApplicationUser>();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        Role = await _roleManager.FindByIdAsync(id);
        if (Role == null) return NotFound();

        UsersInRole = await _userManager.GetUsersInRoleAsync(Role.Name!);
        return Page();
    }
}
