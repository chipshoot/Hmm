using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Pages.Admin.ApiScopes;

[Authorize(Roles = "Administrator")]
public class DeleteModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public DeleteModel(IConfigurationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ApiScope Scope { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        Scope = await _context.ApiScopes.FirstOrDefaultAsync(s => s.Id == id);
        if (Scope == null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null) return NotFound();

        var scope = await _context.ApiScopes.FirstOrDefaultAsync(s => s.Id == id);
        if (scope == null) return NotFound();

        _context.ApiScopes.Remove(scope);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
