using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Hmm.Idp.Pages.Admin.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using IdentityServerClient = Duende.IdentityServer.Models.Client;

namespace Hmm.Idp.Tests;

/// <summary>
/// Tests for <see cref="EditModel"/> (client admin page). Verifies the load → mutate → save
/// round trip, secret-preserve-on-blank behaviour, grant type reconciliation, and validation.
/// </summary>
public class ClientEditModelTests : IDisposable
{
    private readonly ConfigurationDbContext _context;
    private readonly EditModel _page;

    public ClientEditModelTests()
    {
        _context = TestHelpers.CreateConfigurationDbContext();
        _page = new EditModel(_context)
        {
            PageContext = new PageContext()
        };
    }

    public void Dispose() => _context.Dispose();

    private async Task<int> SeedClientAsync(Action<IdentityServerClient>? customize = null)
    {
        var model = new IdentityServerClient
        {
            ClientId = "seeded",
            ClientName = "Seeded Client",
            Enabled = true,
            RequirePkce = true,
            AllowedGrantTypes = { "authorization_code" },
            AllowedScopes = { "openid", "profile" },
            RedirectUris = { "https://original.example" },
            AllowedCorsOrigins = { "https://original.example" },
            ClientSecrets = { new Secret("OriginalSecret123!".Sha256()) },
            AccessTokenLifetime = 3600,
            IdentityTokenLifetime = 300
        };
        customize?.Invoke(model);

        var entity = model.ToEntity();
        _context.Clients.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    [Fact]
    public async Task OnGetAsync_PopulatesViewModel()
    {
        var id = await SeedClientAsync();

        var result = await _page.OnGetAsync(id);

        Assert.IsType<PageResult>(result);
        Assert.Equal("seeded", _page.Client.ClientId);
        Assert.Equal("Seeded Client", _page.Client.ClientName);
        Assert.Contains("authorization_code", _page.Client.AllowedGrantTypes);
        Assert.Contains("https://original.example", _page.Client.RedirectUris);
        Assert.Contains("openid", _page.Client.AllowedScopes);
        Assert.Contains("https://original.example", _page.Client.AllowedCorsOrigins);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenIdIsNull()
    {
        var result = await _page.OnGetAsync(null);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenClientDoesNotExist()
    {
        var result = await _page.OnGetAsync(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesBasicFields()
    {
        var id = await SeedClientAsync();
        await _page.OnGetAsync(id); // load

        _page.Client.ClientName = "Renamed Client";
        _page.Client.Description = "New description";
        _page.Client.Enabled = false;

        var result = await _page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        // Re-query with a fresh context to avoid change-tracker cache
        using var verify = TestHelpers.CreateConfigurationDbContext();
        // Copy data by re-seeding the same in-memory DB name is hard; verify through _context instead
        var saved = await _context.Clients.AsNoTracking().FirstAsync(c => c.Id == id);
        Assert.Equal("Renamed Client", saved.ClientName);
        Assert.Equal("New description", saved.Description);
        Assert.False(saved.Enabled);
    }

    [Fact]
    public async Task OnPostAsync_PreservesSecret_WhenNewSecretIsBlank()
    {
        var id = await SeedClientAsync();
        var originalSecretHash = await _context.Clients
            .Include(c => c.ClientSecrets)
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => c.ClientSecrets.First().Value)
            .FirstAsync();

        await _page.OnGetAsync(id);
        _page.Client.ClientSecret = ""; // blank — means keep current

        await _page.OnPostAsync();

        var updatedSecretHash = await _context.Clients
            .Include(c => c.ClientSecrets)
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => c.ClientSecrets.First().Value)
            .FirstAsync();

        Assert.Equal(originalSecretHash, updatedSecretHash);
    }

    [Fact]
    public async Task OnPostAsync_ReplacesSecret_WhenNewSecretProvided()
    {
        var id = await SeedClientAsync();
        var originalSecretHash = await _context.Clients
            .Include(c => c.ClientSecrets)
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => c.ClientSecrets.First().Value)
            .FirstAsync();

        await _page.OnGetAsync(id);
        _page.Client.ClientSecret = "NewSecret456!";

        await _page.OnPostAsync();

        var newSecrets = await _context.Clients
            .Include(c => c.ClientSecrets)
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => c.ClientSecrets.Select(s => s.Value).ToList())
            .FirstAsync();

        Assert.Single(newSecrets);
        Assert.NotEqual(originalSecretHash, newSecrets[0]);
        Assert.NotEqual("NewSecret456!", newSecrets[0]);
    }

    [Fact]
    public async Task OnPostAsync_ReplacesAllRedirectUris()
    {
        var id = await SeedClientAsync();
        await _page.OnGetAsync(id);

        _page.Client.RedirectUris = "https://new1.example\nhttps://new2.example";

        await _page.OnPostAsync();

        var saved = await _context.Clients
            .Include(c => c.RedirectUris)
            .AsNoTracking()
            .FirstAsync(c => c.Id == id);
        Assert.Equal(2, saved.RedirectUris.Count);
        Assert.DoesNotContain(saved.RedirectUris, u => u.RedirectUri == "https://original.example");
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPageWithError_WhenGrantTypesCleared()
    {
        var id = await SeedClientAsync();
        await _page.OnGetAsync(id);
        _page.Client.AllowedGrantTypes = new List<string>();

        var result = await _page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(_page.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_ReplacesGrantTypes()
    {
        var id = await SeedClientAsync();
        await _page.OnGetAsync(id);

        _page.Client.AllowedGrantTypes = new List<string> { "client_credentials" };

        await _page.OnPostAsync();

        var saved = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .AsNoTracking()
            .FirstAsync(c => c.Id == id);
        Assert.Single(saved.AllowedGrantTypes);
        Assert.Equal("client_credentials", saved.AllowedGrantTypes.First().GrantType);
    }

    [Fact]
    public async Task OnPostAsync_PersistsClientClaims()
    {
        var id = await SeedClientAsync();
        await _page.OnGetAsync(id);

        _page.Client.ClientClaims = "role=admin\ntier=gold";

        await _page.OnPostAsync();

        var saved = await _context.Clients
            .Include(c => c.Claims)
            .AsNoTracking()
            .FirstAsync(c => c.Id == id);
        Assert.Equal(2, saved.Claims.Count);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenClientDeletedBetweenGetAndPost()
    {
        var id = await SeedClientAsync();
        await _page.OnGetAsync(id);

        // Simulate concurrent delete
        var entity = await _context.Clients.FirstAsync(c => c.Id == id);
        _context.Clients.Remove(entity);
        await _context.SaveChangesAsync();

        var result = await _page.OnPostAsync();
        Assert.IsType<NotFoundResult>(result);
    }
}
