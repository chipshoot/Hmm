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

    public List<string> AvailableGrantTypes { get; } = GrantTypeOptions.All;

    public IActionResult OnGet()
    {
        // Initialize default values
        Client = new ViewModel
        {
            Enabled = true,
            RequirePkce = true,
            AllowedGrantTypes = new List<string> { "authorization_code" },
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
            Description = Client.Description,
            Enabled = Client.Enabled,
            AllowOfflineAccess = Client.AllowOfflineAccess,
            UpdateAccessTokenClaimsOnRefresh = Client.UpdateAccessTokenClaimsOnRefresh,
            RequirePkce = Client.RequirePkce,
            AccessTokenLifetime = Client.AccessTokenLifetime,
            IdentityTokenLifetime = Client.IdentityTokenLifetime,
            AbsoluteRefreshTokenLifetime = Client.AbsoluteRefreshTokenLifetime,
            SlidingRefreshTokenLifetime = Client.SlidingRefreshTokenLifetime,
            AllowedGrantTypes = (Client.AllowedGrantTypes ?? new List<string>())
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct()
                .ToList()
        };

        if (client.AllowedGrantTypes.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "At least one grant type must be selected.");
            return Page();
        }

        // Add redirect URIs
        foreach (var uri in SplitLines(Client.RedirectUris))
        {
            client.RedirectUris.Add(uri);
        }

        // Add post-logout redirect URIs
        foreach (var uri in SplitLines(Client.PostLogoutRedirectUris))
        {
            client.PostLogoutRedirectUris.Add(uri);
        }

        // Add allowed scopes
        foreach (var scope in SplitCsv(Client.AllowedScopes))
        {
            client.AllowedScopes.Add(scope);
        }

        // Add allowed CORS origins
        foreach (var origin in SplitLines(Client.AllowedCorsOrigins))
        {
            client.AllowedCorsOrigins.Add(origin);
        }

        // Add client claims
        foreach (var (type, value) in ParseClaims(Client.ClientClaims))
        {
            client.Claims.Add(new Duende.IdentityServer.Models.ClientClaim(type, value));
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

    private static IEnumerable<string> SplitLines(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) yield break;
        foreach (var line in input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0) yield return trimmed;
        }
    }

    private static IEnumerable<string> SplitCsv(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) yield break;
        foreach (var part in input.Split(new[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.Length > 0) yield return trimmed;
        }
    }

    private static IEnumerable<(string Type, string Value)> ParseClaims(string input)
    {
        foreach (var line in SplitLines(input))
        {
            var idx = line.IndexOf('=');
            if (idx <= 0 || idx == line.Length - 1) continue;
            yield return (line.Substring(0, idx).Trim(), line.Substring(idx + 1).Trim());
        }
    }
}
