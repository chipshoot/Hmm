using Hmm.Idp.Pages.Admin.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hmm.Idp.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class CreateModel : PageModel
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public CreateModel(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    [BindProperty]
    public ViewModel Role { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        if (await _roleManager.RoleExistsAsync(Role.Name))
        {
            ModelState.AddModelError("Role.Name", "A role with this name already exists.");
            return Page();
        }

        var result = await _roleManager.CreateAsync(new ApplicationRole
        {
            Name = Role.Name,
            Description = Role.Description
        });

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
