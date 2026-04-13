using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Hmm.Idp.Pages.Admin.ApiScopes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hmm.Idp.Tests;

/// <summary>
/// Tests for the new API Scope admin page models (Create/Edit/Details/Delete).
/// </summary>
public class ApiScopeModelTests : IDisposable
{
    private readonly ConfigurationDbContext _context;

    public ApiScopeModelTests()
    {
        _context = TestHelpers.CreateConfigurationDbContext();
    }

    public void Dispose() => _context.Dispose();

    private async Task<int> SeedScopeAsync(string name = "scope.test")
    {
        var entity = new ApiScope
        {
            Name = name,
            DisplayName = $"{name} display",
            Description = $"{name} desc",
            Enabled = true,
            ShowInDiscoveryDocument = true,
            UserClaims = new List<ApiScopeClaim>
            {
                new() { Type = "role" }
            }
        };
        _context.ApiScopes.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    // ── Create ────────────────────────────────────────────────────

    [Fact]
    public async Task Create_OnPostAsync_PersistsNewScope()
    {
        var page = new CreateModel(_context)
        {
            PageContext = new PageContext(),
            Scope = new ViewModel
            {
                Name = "scope.new",
                DisplayName = "New Scope",
                Description = "A brand new scope",
                Enabled = true,
                ShowInDiscoveryDocument = true,
                Required = false,
                Emphasize = true,
                UserClaims = "name, email, role"
            }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var saved = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .FirstAsync(s => s.Name == "scope.new");

        Assert.Equal("New Scope", saved.DisplayName);
        Assert.True(saved.Emphasize);
        Assert.Equal(3, saved.UserClaims!.Count);
        Assert.Contains(saved.UserClaims, c => c.Type == "name");
        Assert.Contains(saved.UserClaims, c => c.Type == "email");
        Assert.Contains(saved.UserClaims, c => c.Type == "role");
    }

    [Fact]
    public async Task Create_OnPostAsync_RejectsDuplicateName()
    {
        await SeedScopeAsync("existing.scope");

        var page = new CreateModel(_context)
        {
            PageContext = new PageContext(),
            Scope = new ViewModel { Name = "existing.scope" }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        Assert.True(page.ModelState.ContainsKey("Scope.Name"));
    }

    [Fact]
    public async Task Create_OnPostAsync_IgnoresEmptyClaimEntries()
    {
        var page = new CreateModel(_context)
        {
            PageContext = new PageContext(),
            Scope = new ViewModel
            {
                Name = "scope.blank",
                UserClaims = "name, , ,  , email"
            }
        };

        await page.OnPostAsync();

        var saved = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .FirstAsync(s => s.Name == "scope.blank");
        Assert.Equal(2, saved.UserClaims!.Count);
    }

    // ── Edit ──────────────────────────────────────────────────────

    [Fact]
    public async Task Edit_OnGetAsync_PopulatesViewModel()
    {
        var id = await SeedScopeAsync();
        var page = new EditModel(_context) { PageContext = new PageContext() };

        var result = await page.OnGetAsync(id);

        Assert.IsType<PageResult>(result);
        Assert.Equal("scope.test", page.Scope.Name);
        Assert.Contains("role", page.Scope.UserClaims);
    }

    [Fact]
    public async Task Edit_OnGetAsync_ReturnsNotFound_WhenIdMissing()
    {
        var page = new EditModel(_context) { PageContext = new PageContext() };
        Assert.IsType<NotFoundResult>(await page.OnGetAsync(null));
        Assert.IsType<NotFoundResult>(await page.OnGetAsync(999));
    }

    [Fact]
    public async Task Edit_OnPostAsync_UpdatesFieldsAndClaims()
    {
        var id = await SeedScopeAsync();
        var page = new EditModel(_context) { PageContext = new PageContext() };
        await page.OnGetAsync(id);

        page.Scope.DisplayName = "Updated";
        page.Scope.Enabled = false;
        page.Scope.UserClaims = "sub, email";

        var result = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var saved = await _context.ApiScopes
            .Include(s => s.UserClaims)
            .AsNoTracking()
            .FirstAsync(s => s.Id == id);
        Assert.Equal("Updated", saved.DisplayName);
        Assert.False(saved.Enabled);
        Assert.Equal(2, saved.UserClaims!.Count);
        Assert.Contains(saved.UserClaims, c => c.Type == "sub");
        Assert.Contains(saved.UserClaims, c => c.Type == "email");
        Assert.DoesNotContain(saved.UserClaims, c => c.Type == "role");
    }

    // ── Details ───────────────────────────────────────────────────

    [Fact]
    public async Task Details_OnGetAsync_LoadsScope()
    {
        var id = await SeedScopeAsync();
        var page = new DetailsModel(_context) { PageContext = new PageContext() };

        var result = await page.OnGetAsync(id);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(page.Scope);
        Assert.Equal("scope.test", page.Scope.Name);
    }

    [Fact]
    public async Task Details_OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        var page = new DetailsModel(_context) { PageContext = new PageContext() };
        Assert.IsType<NotFoundResult>(await page.OnGetAsync(999));
    }

    // ── Delete ────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_OnGetAsync_LoadsScope()
    {
        var id = await SeedScopeAsync();
        var page = new DeleteModel(_context) { PageContext = new PageContext() };

        var result = await page.OnGetAsync(id);

        Assert.IsType<PageResult>(result);
        Assert.Equal("scope.test", page.Scope.Name);
    }

    [Fact]
    public async Task Delete_OnPostAsync_RemovesScope()
    {
        var id = await SeedScopeAsync();
        var page = new DeleteModel(_context) { PageContext = new PageContext() };

        var result = await page.OnPostAsync(id);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.False(await _context.ApiScopes.AnyAsync(s => s.Id == id));
    }

    [Fact]
    public async Task Delete_OnPostAsync_ReturnsNotFound_WhenMissing()
    {
        var page = new DeleteModel(_context) { PageContext = new PageContext() };
        Assert.IsType<NotFoundResult>(await page.OnPostAsync(999));
    }
}
