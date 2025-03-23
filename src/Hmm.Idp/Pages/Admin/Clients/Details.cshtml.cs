// Pages/Admin/Clients/Details.cshtml.cs
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Pages.Admin.Clients;

[Authorize(Roles = "Administrator")]
public class DetailsModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public DetailsModel(IConfigurationDbContext context)
    {
        _context = context;
    }

    public Duende.IdentityServer.EntityFramework.Entities.Client Client { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Client = await _context.Clients
            .Include(c => c.AllowedScopes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedGrantTypes)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (Client == null)
        {
            return NotFound();
        }
        
        return Page();
    }
}