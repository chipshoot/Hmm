using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Pages.Admin.ApiScopes;

[Authorize(Roles = "Administrator")]
public class DetailsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public DetailsModel(IConfigurationDbContext context)
    {
        _context = context;
    }

    public ApiScope Scope { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        Scope = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (Scope == null) return NotFound();
        return Page();
    }
}
