using Hmm.Idp.Pages.Admin.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Pages.Admin.Roles;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public List<RoleRow> Roles { get; set; } = new();

    public async Task OnGetAsync()
    {
        var roles = await _roleManager.Roles.OrderBy(r => r.Name).ToListAsync();
        foreach (var role in roles)
        {
            var users = await _userManager.GetUsersInRoleAsync(role.Name!);
            Roles.Add(new RoleRow
            {
                Id = role.Id,
                Name = role.Name!,
                Description = role.Description,
                UserCount = users.Count
            });
        }
    }

    public class RoleRow
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int UserCount { get; set; }
    }
}
