// Pages/Admin/Clients/Edit.cshtml.cs
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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

    public List<string> AvailableGrantTypes { get; } = GrantTypeOptions.All;

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
            .Include(c => c.AllowedCorsOrigins)
            .Include(c => c.Claims)
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
            Description = client.Description,
            Enabled = client.Enabled,
            AllowOfflineAccess = client.AllowOfflineAccess,
            UpdateAccessTokenClaimsOnRefresh = client.UpdateAccessTokenClaimsOnRefresh,
            RequirePkce = client.RequirePkce,
            AccessTokenLifetime = client.AccessTokenLifetime,
            IdentityTokenLifetime = client.IdentityTokenLifetime,
            AbsoluteRefreshTokenLifetime = client.AbsoluteRefreshTokenLifetime,
            SlidingRefreshTokenLifetime = client.SlidingRefreshTokenLifetime,
            AllowedGrantTypes = client.AllowedGrantTypes?.Select(g => g.GrantType).ToList() ?? new List<string>(),
            RedirectUris = string.Join(Environment.NewLine, client.RedirectUris?.Select(r => r.RedirectUri) ?? Array.Empty<string>()),
            PostLogoutRedirectUris = string.Join(Environment.NewLine, client.PostLogoutRedirectUris?.Select(p => p.PostLogoutRedirectUri) ?? Array.Empty<string>()),
            AllowedScopes = string.Join(",", client.AllowedScopes?.Select(s => s.Scope) ?? Array.Empty<string>()),
            AllowedCorsOrigins = string.Join(Environment.NewLine, client.AllowedCorsOrigins?.Select(o => o.Origin) ?? Array.Empty<string>()),
            ClientClaims = string.Join(Environment.NewLine, client.Claims?.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>())
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
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.AllowedCorsOrigins)
            .Include(c => c.Claims)
            .FirstOrDefaultAsync(c => c.Id == Client.Id);

        if (client == null)
        {
            return NotFound();
        }

        // Update basic properties
        client.ClientId = Client.ClientId;
        client.ClientName = Client.ClientName;
        client.Description = Client.Description;
        client.Enabled = Client.Enabled;
        client.AllowOfflineAccess = Client.AllowOfflineAccess;
        client.UpdateAccessTokenClaimsOnRefresh = Client.UpdateAccessTokenClaimsOnRefresh;
        client.RequirePkce = Client.RequirePkce;
        client.AccessTokenLifetime = Client.AccessTokenLifetime;
        client.IdentityTokenLifetime = Client.IdentityTokenLifetime;
        client.AbsoluteRefreshTokenLifetime = Client.AbsoluteRefreshTokenLifetime;
        client.SlidingRefreshTokenLifetime = Client.SlidingRefreshTokenLifetime;

        // Update grant types
        var selectedGrantTypes = (Client.AllowedGrantTypes ?? new List<string>())
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Distinct()
            .ToList();
        if (selectedGrantTypes.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "At least one grant type must be selected.");
            return Page();
        }
        client.AllowedGrantTypes.Clear();
        foreach (var gt in selectedGrantTypes)
        {
            client.AllowedGrantTypes.Add(new ClientGrantType { GrantType = gt, Client = client });
        }

        // Update redirect URIs
        client.RedirectUris.Clear();
        foreach (var uri in SplitLines(Client.RedirectUris))
        {
            client.RedirectUris.Add(new ClientRedirectUri { RedirectUri = uri, Client = client });
        }

        // Update post-logout redirect URIs
        client.PostLogoutRedirectUris.Clear();
        foreach (var uri in SplitLines(Client.PostLogoutRedirectUris))
        {
            client.PostLogoutRedirectUris.Add(new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = uri, Client = client });
        }

        // Update allowed scopes
        client.AllowedScopes.Clear();
        foreach (var scope in SplitCsv(Client.AllowedScopes))
        {
            client.AllowedScopes.Add(new ClientScope { Scope = scope, Client = client });
        }

        // Update CORS origins
        client.AllowedCorsOrigins.Clear();
        foreach (var origin in SplitLines(Client.AllowedCorsOrigins))
        {
            client.AllowedCorsOrigins.Add(new ClientCorsOrigin { Origin = origin, Client = client });
        }

        // Update client claims
        client.Claims.Clear();
        foreach (var (type, value) in ParseClaims(Client.ClientClaims))
        {
            client.Claims.Add(new Duende.IdentityServer.EntityFramework.Entities.ClientClaim { Type = type, Value = value, Client = client });
        }

        // Update client secret if provided
        if (!string.IsNullOrWhiteSpace(Client.ClientSecret))
        {
            client.ClientSecrets.Clear();
            client.ClientSecrets.Add(new ClientSecret
            {
                Value = Client.ClientSecret.Sha256(),
                Type = "SharedSecret",
                Client = client,
                Created = DateTime.UtcNow
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
