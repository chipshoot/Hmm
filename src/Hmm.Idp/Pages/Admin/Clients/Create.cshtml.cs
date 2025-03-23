// Pages/Admin/Clients/Create.cshtml.cs
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IdentityServerClient = Duende.IdentityServer.Models.Client;

namespace Hmm.Idp.Pages.Admin.Clients;

[Authorize(Roles = "Administrator")]
public class CreateModel : PageModel
{
    private readonly IConfigurationDbContext _context;

    public CreateModel(IConfigurationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ViewModel Client { get; set; }

    public IActionResult OnGet()
    {
        // Initialize default values
        Client = new ViewModel
        {
            RequirePkce = true,
            AllowedScopes = $"{IdentityServerConstants.StandardScopes.OpenId},{IdentityServerConstants.StandardScopes.Profile}"
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Create a Duende IdentityServer client from our view model
        var client = new IdentityServerClient
        {
            ClientId = Client.ClientId,
            ClientName = Client.ClientName,
            AllowOfflineAccess = Client.AllowOfflineAccess,
            UpdateAccessTokenClaimsOnRefresh = Client.UpdateAccessTokenClaimsOnRefresh,
            RequirePkce = Client.RequirePkce,
            AllowedGrantTypes = Duende.IdentityServer.Models.GrantTypes.Code,
        };

        // Add redirect URIs
        if (!string.IsNullOrWhiteSpace(Client.RedirectUris))
        {
            var uris = Client.RedirectUris.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var uri in uris)
            {
                client.RedirectUris.Add(uri.Trim());
            }
        }

        // Add post-logout redirect URIs
        if (!string.IsNullOrWhiteSpace(Client.PostLogoutRedirectUris))
        {
            var uris = Client.PostLogoutRedirectUris.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var uri in uris)
            {
                client.PostLogoutRedirectUris.Add(uri.Trim());
            }
        }

        // Add allowed scopes
        if (!string.IsNullOrWhiteSpace(Client.AllowedScopes))
        {
            var scopes = Client.AllowedScopes.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var scope in scopes)
            {
                client.AllowedScopes.Add(scope.Trim());
            }
        }

        // Add client secret
        if (!string.IsNullOrWhiteSpace(Client.ClientSecret))
        {
            client.ClientSecrets.Add(new Duende.IdentityServer.Models.Secret(Client.ClientSecret.Sha256()));
        }

        // Map from model to entity
        var entity = client.ToEntity();

        // Save to database
        _context.Clients.Add(entity);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}