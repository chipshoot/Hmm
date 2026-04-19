using Hmm.Idp.Pages.Admin.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class EditModel : PageModel
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public EditModel(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    [BindProperty]
    public ViewModel Role { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        Role = new ViewModel
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var role = await _roleManager.FindByIdAsync(Role.Id);
        if (role == null) return NotFound();

        // Prevent renaming the Administrator role
        if (string.Equals(role.Name, "Administrator", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Role.Name, "Administrator", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Role.Name", "The Administrator role cannot be renamed.");
            return Page();
        }

        role.Name = Role.Name;
        role.Description = Role.Description;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        return RedirectToPage("./Index");
    }
}
