// Pages/Admin/Clients/Edit.cshtml.cs
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Hmm.Idp.Pages.Admin.Clients;

[Authorize(Roles = "Administrator")]
public class EditModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public EditModel(IConfigurationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ViewModel Client { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var client = await _context.Clients
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.ClientSecrets)
            .Include(c => c.AllowedGrantTypes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
        {
            return NotFound();
        }

        Client = new ViewModel
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            AllowOfflineAccess = client.AllowOfflineAccess,
            UpdateAccessTokenClaimsOnRefresh = client.UpdateAccessTokenClaimsOnRefresh,
            RequirePkce = client.RequirePkce,
            RedirectUris = string.Join(Environment.NewLine, client.RedirectUris?.Select(r => r.RedirectUri) ?? Array.Empty<string>()),
            PostLogoutRedirectUris = string.Join(Environment.NewLine, client.PostLogoutRedirectUris?.Select(p => p.PostLogoutRedirectUri) ?? Array.Empty<string>()),
            AllowedScopes = string.Join(",", client.AllowedScopes?.Select(s => s.Scope) ?? Array.Empty<string>())
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var client = await _context.Clients
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.ClientSecrets)
            .FirstOrDefaultAsync(c => c.Id == Client.Id);

        if (client == null)
        {
            return NotFound();
        }

        // Update basic properties
        client.ClientId = Client.ClientId;
        client.ClientName = Client.ClientName;
        client.AllowOfflineAccess = Client.AllowOfflineAccess;
        client.UpdateAccessTokenClaimsOnRefresh = Client.UpdateAccessTokenClaimsOnRefresh;
        client.RequirePkce = Client.RequirePkce;

        // Update redirect URIs
        client.RedirectUris.Clear();
        if (!string.IsNullOrWhiteSpace(Client.RedirectUris))
        {
            var uris = Client.RedirectUris.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var uri in uris)
            {
                client.RedirectUris.Add(new Duende.IdentityServer.EntityFramework.Entities.ClientRedirectUri
                {
                    RedirectUri = uri.Trim(),
                    Client = client
                });
            }
        }

        // Update post-logout redirect URIs
        client.PostLogoutRedirectUris.Clear();
        if (!string.IsNullOrWhiteSpace(Client.PostLogoutRedirectUris))
        {
            var uris = Client.PostLogoutRedirectUris.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var uri in uris)
            {
                client.PostLogoutRedirectUris.Add(new Duende.IdentityServer.EntityFramework.Entities.ClientPostLogoutRedirectUri
                {
                    PostLogoutRedirectUri = uri.Trim(),
                    Client = client
                });
            }
        }

        // Update allowed scopes
        client.AllowedScopes.Clear();
        if (!string.IsNullOrWhiteSpace(Client.AllowedScopes))
        {
            var scopes = Client.AllowedScopes.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var scope in scopes)
            {
                client.AllowedScopes.Add(new Duende.IdentityServer.EntityFramework.Entities.ClientScope
                {
                    Scope = scope.Trim(),
                    Client = client
                });
            }
        }

        // Update client secret if provided
        if (!string.IsNullOrWhiteSpace(Client.ClientSecret))
        {
            client.ClientSecrets.Clear();
            client.ClientSecrets.Add(new Duende.IdentityServer.EntityFramework.Entities.ClientSecret
            {
                Value = Client.ClientSecret.Sha256(),
                Type = "SharedSecret",
                Client = client
            });
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ClientExists(client.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToPage("./Index");
    }

    private bool ClientExists(int id)
    {
        return _context.Clients.Any(c => c.Id == id);
    }
}
