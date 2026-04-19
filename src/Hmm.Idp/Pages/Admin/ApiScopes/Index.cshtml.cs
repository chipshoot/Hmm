using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Pages.Admin.ApiScopes;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public IndexModel(IConfigurationDbContext context)
    {
        _context = context;
    }

    public IList<ApiScope> Scopes { get; set; } = new List<ApiScope>();

    public async Task OnGetAsync()
    {
        Scopes = await _context.ApiScopes
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}
