using Duende.IdentityServer.EntityFramework.DbContexts;
using Hmm.Idp.Pages.Admin.Clients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Tests;

/// <summary>
/// Tests for <see cref="CreateModel"/> (client admin page). Verifies that the POST flow
/// persists all the new fields added in the admin overhaul, validates grant type selection,
/// and parses the line/CSV text inputs correctly.
/// </summary>
public class ClientCreateModelTests : IDisposable
{
    private readonly ConfigurationDbContext _context;
    private readonly CreateModel _page;

    public ClientCreateModelTests()
    {
        _context = TestHelpers.CreateConfigurationDbContext();
        _page = new CreateModel(_context)
        {
            PageContext = new PageContext()
        };
    }

    public void Dispose() => _context.Dispose();

    private static ViewModel MinimalValidClient(string clientId = "test-client") => new()
    {
        ClientId = clientId,
        ClientName = "Test Client",
        Enabled = true,
        RequirePkce = true,
        AllowedGrantTypes = new List<string> { "authorization_code" },
        AccessTokenLifetime = 3600,
        IdentityTokenLifetime = 300,
        AbsoluteRefreshTokenLifetime = 2592000,
        SlidingRefreshTokenLifetime = 1296000,
        AllowedScopes = "openid,profile"
    };

    [Fact]
    public void OnGet_InitializesDefaults()
    {
        _page.OnGet();

        Assert.NotNull(_page.Client);
        Assert.True(_page.Client.RequirePkce);
        Assert.True(_page.Client.Enabled);
        Assert.Contains("authorization_code", _page.Client.AllowedGrantTypes);
        Assert.NotEmpty(_page.AvailableGrantTypes);
    }

    [Fact]
    public async Task OnPostAsync_PersistsClient_WithHappyPath()
    {
        _page.Client = MinimalValidClient("happy-path");

        var result = await _page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);

        var saved = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.AllowedScopes)
            .FirstAsync(c => c.ClientId == "happy-path");

        Assert.Equal("Test Client", saved.ClientName);
        Assert.True(saved.Enabled);
        Assert.True(saved.RequirePkce);
        Assert.Equal(3600, saved.AccessTokenLifetime);
        Assert.Single(saved.AllowedGrantTypes);
        Assert.Equal("authorization_code", saved.AllowedGrantTypes.First().GrantType);
        Assert.Equal(2, saved.AllowedScopes.Count);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenNoGrantTypesSelected()
    {
        _page.Client = MinimalValidClient("no-grants");
        _page.Client.AllowedGrantTypes = new List<string>(); // empty

        var result = await _page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(_page.ModelState.IsValid);
        Assert.False(await _context.Clients.AnyAsync(c => c.ClientId == "no-grants"));
    }

    [Fact]
    public async Task OnPostAsync_PersistsMultipleGrantTypes()
    {
        _page.Client = MinimalValidClient("multi-grant");
        _page.Client.AllowedGrantTypes = new List<string>
        {
            "authorization_code",
            "client_credentials",
            "refresh_token"
        };

        var result = await _page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var saved = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .FirstAsync(c => c.ClientId == "multi-grant");
        Assert.Equal(3, saved.AllowedGrantTypes.Count);
    }

    [Fact]
    public async Task OnPostAsync_DeduplicatesDuplicateGrantTypes()
    {
        _page.Client = MinimalValidClient("dedup-grant");
        _page.Client.AllowedGrantTypes = new List<string>
        {
            "authorization_code",
            "authorization_code",
            "client_credentials"
        };

        await _page.OnPostAsync();

        var saved = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .FirstAsync(c => c.ClientId == "dedup-grant");
        Assert.Equal(2, saved.AllowedGrantTypes.Count);
    }

    [Fact]
    public async Task OnPostAsync_IgnoresEmptyGrantTypeEntries()
    {
        _page.Client = MinimalValidClient("blank-grants");
        _page.Client.AllowedGrantTypes = new List<string>
        {
            "authorization_code",
            "",
            "   ",
            "client_credentials"
        };

        var result = await _page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var saved = await _context.Clients
            .Include(c => c.AllowedGrantTypes)
            .FirstAsync(c => c.ClientId == "blank-grants");
        Assert.Equal(2, saved.AllowedGrantTypes.Count);
    }

    [Fact]
    public async Task OnPostAsync_ParsesRedirectUris()
    {
        _page.Client = MinimalValidClient("with-uris");
        _page.Client.RedirectUris = "https://a.example\nhttps://b.example\n   \nhttps://c.example";

        await _page.OnPostAsync();

        var saved = await _context.Clients
            .Include(c => c.RedirectUris)
            .FirstAsync(c => c.ClientId == "with-uris");
        Assert.Equal(3, saved.RedirectUris.Count);
    }

    [Fact]
    public async Task OnPostAsync_ParsesCorsOrigins()
    {
        _page.Client = MinimalValidClient("with-cors");
        _page.Client.AllowedCorsOrigins = "https://foo.com\nhttps://bar.com";

        await _page.OnPostAsync();

        var saved = await _context.Clients
            .Include(c => c.AllowedCorsOrigins)
            .FirstAsync(c => c.ClientId == "with-cors");
        Assert.Equal(2, saved.AllowedCorsOrigins.Count);
    }

    [Fact]
    public async Task OnPostAsync_ParsesClientClaims_FromTypeEqualsValueLines()
    {
        _page.Client = MinimalValidClient("with-claims");
        _page.Client.ClientClaims = "role=admin\ntier=premium\ninvalid-line-no-equals";

        await _page.OnPostAsync();

        var saved = await _context.Clients
            .Include(c => c.Claims)
            .FirstAsync(c => c.ClientId == "with-claims");
        Assert.Equal(2, saved.Claims.Count);
        Assert.Contains(saved.Claims, c => c.Type == "role" && c.Value == "admin");
        Assert.Contains(saved.Claims, c => c.Type == "tier" && c.Value == "premium");
    }

    [Fact]
    public async Task OnPostAsync_HashesClientSecret()
    {
        _page.Client = MinimalValidClient("with-secret");
        _page.Client.ClientSecret = "SuperSecret123!";

        await _page.OnPostAsync();

        var saved = await _context.Clients
            .Include(c => c.ClientSecrets)
            .FirstAsync(c => c.ClientId == "with-secret");
        Assert.Single(saved.ClientSecrets);
        // Hash should not equal the plaintext
        Assert.NotEqual("SuperSecret123!", saved.ClientSecrets.First().Value);
    }

    [Fact]
    public async Task OnPostAsync_PersistsTokenLifetimes()
    {
        _page.Client = MinimalValidClient("with-lifetimes");
        _page.Client.AccessTokenLifetime = 7200;
        _page.Client.IdentityTokenLifetime = 600;
        _page.Client.AbsoluteRefreshTokenLifetime = 86400;
        _page.Client.SlidingRefreshTokenLifetime = 43200;

        await _page.OnPostAsync();

        var saved = await _context.Clients.FirstAsync(c => c.ClientId == "with-lifetimes");
        Assert.Equal(7200, saved.AccessTokenLifetime);
        Assert.Equal(600, saved.IdentityTokenLifetime);
        Assert.Equal(86400, saved.AbsoluteRefreshTokenLifetime);
        Assert.Equal(43200, saved.SlidingRefreshTokenLifetime);
    }
}
