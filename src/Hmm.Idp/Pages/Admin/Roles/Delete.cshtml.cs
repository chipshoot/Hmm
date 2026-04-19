using Hmm.Idp.Pages.Admin.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class DeleteModel : PageModel
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public DeleteModel(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public ApplicationRole Role { get; set; }
    public int UserCount { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        Role = await _roleManager.FindByIdAsync(id);
        if (Role == null) return NotFound();

        if (string.Equals(Role.Name, "Administrator", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("The Administrator role cannot be deleted.");
        }

        var users = await _userManager.GetUsersInRoleAsync(Role.Name!);
        UserCount = users.Count;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        if (string.Equals(role.Name, "Administrator", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("The Administrator role cannot be deleted.");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            Role = role;
            return Page();
        }

        return RedirectToPage("./Index");
    }
}
