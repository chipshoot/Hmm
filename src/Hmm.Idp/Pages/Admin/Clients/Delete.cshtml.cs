// Pages/Admin/Clients/Delete.cshtml.cs
using Duende.IdentityServer.EntityFramework.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Pages.Admin.Clients;

[Authorize(Roles = "Administrator")]
public class DeleteModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public DeleteModel(IConfigurationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Duende.IdentityServer.EntityFramework.Entities.Client Client { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Client = await _context.Clients.FirstOrDefaultAsync(m => m.Id == id);

        if (Client == null)
        {
            return NotFound();
        }
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var client = await _context.Clients.FindAsync(Client.Id);

        if (client != null)
        {
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}