// Pages/Admin/Clients/Index.cshtml.cs
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Pages.Admin.Clients;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public IndexModel(IConfigurationDbContext context)
    {
        _context = context;
    }

    public IList<Client> Clients { get; set; }

    public async Task OnGetAsync()
    {
        Clients = await _context.Clients
            .Include(c => c.AllowedScopes)
            .ToListAsync();
    }
}