using Hmm.Idp.Pages.Admin.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using CreateModel = Hmm.Idp.Pages.Admin.Roles.CreateModel;
using DeleteModel = Hmm.Idp.Pages.Admin.Roles.DeleteModel;
using EditModel = Hmm.Idp.Pages.Admin.Roles.EditModel;
using ViewModel = Hmm.Idp.Pages.Admin.Roles.ViewModel;

namespace Hmm.Idp.Tests;

/// <summary>
/// Tests for the new Roles admin page models (Create/Edit/Delete/Details).
/// Uses mocked <see cref="RoleManager{ApplicationRole}"/> and
/// <see cref="UserManager{ApplicationUser}"/> — the page models never touch EF directly.
/// </summary>
public class RolesModelTests
{
    private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;

    public RolesModelTests()
    {
        _mockRoleManager = TestHelpers.CreateMockRoleManager();
        _mockUserManager = TestHelpers.CreateMockUserManager();
    }

    // ── Create ────────────────────────────────────────────────────

    [Fact]
    public async Task Create_OnPostAsync_CreatesRole_WhenNameAvailable()
    {
        _mockRoleManager.Setup(m => m.RoleExistsAsync("Editor")).ReturnsAsync(false);
        _mockRoleManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);

        var page = new CreateModel(_mockRoleManager.Object)
        {
            PageContext = new PageContext(),
            Role = new ViewModel { Name = "Editor", Description = "Editors" }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _mockRoleManager.Verify(m => m.CreateAsync(It.Is<ApplicationRole>(
            r => r.Name == "Editor" && r.Description == "Editors")), Times.Once);
    }

    [Fact]
    public async Task Create_OnPostAsync_RejectsDuplicateName()
    {
        _mockRoleManager.Setup(m => m.RoleExistsAsync("Editor")).ReturnsAsync(true);

        var page = new CreateModel(_mockRoleManager.Object)
        {
            PageContext = new PageContext(),
            Role = new ViewModel { Name = "Editor" }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(page.ModelState.ContainsKey("Role.Name"));
        _mockRoleManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact]
    public async Task Create_OnPostAsync_PropagatesIdentityErrors()
    {
        _mockRoleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _mockRoleManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Something went wrong" }));

        var page = new CreateModel(_mockRoleManager.Object)
        {
            PageContext = new PageContext(),
            Role = new ViewModel { Name = "Editor" }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
    }

    // ── Edit ──────────────────────────────────────────────────────

    [Fact]
    public async Task Edit_OnGetAsync_PopulatesViewModel()
    {
        var role = new ApplicationRole { Id = "role-1", Name = "Editor", Description = "Editors" };
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);

        var page = new EditModel(_mockRoleManager.Object) { PageContext = new PageContext() };
        var result = await page.OnGetAsync("role-1");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Editor", page.Role.Name);
        Assert.Equal("Editors", page.Role.Description);
    }

    [Fact]
    public async Task Edit_OnPostAsync_SavesChanges()
    {
        var role = new ApplicationRole { Id = "role-1", Name = "Editor", Description = "Old" };
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockRoleManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);

        var page = new EditModel(_mockRoleManager.Object)
        {
            PageContext = new PageContext(),
            Role = new ViewModel { Id = "role-1", Name = "Editor", Description = "New" }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _mockRoleManager.Verify(m => m.UpdateAsync(It.Is<ApplicationRole>(
            r => r.Description == "New")), Times.Once);
    }

    [Fact]
    public async Task Edit_OnPostAsync_ForbidsRenamingAdministrator()
    {
        var role = new ApplicationRole { Id = "admin-id", Name = "Administrator", Description = "" };
        _mockRoleManager.Setup(m => m.FindByIdAsync("admin-id")).ReturnsAsync(role);

        var page = new EditModel(_mockRoleManager.Object)
        {
            PageContext = new PageContext(),
            Role = new ViewModel { Id = "admin-id", Name = "SuperAdmin" }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.True(page.ModelState.ContainsKey("Role.Name"));
        _mockRoleManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact]
    public async Task Edit_OnPostAsync_AllowsUpdatingAdministratorDescription()
    {
        var role = new ApplicationRole { Id = "admin-id", Name = "Administrator", Description = "" };
        _mockRoleManager.Setup(m => m.FindByIdAsync("admin-id")).ReturnsAsync(role);
        _mockRoleManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);

        var page = new EditModel(_mockRoleManager.Object)
        {
            PageContext = new PageContext(),
            Role = new ViewModel { Id = "admin-id", Name = "Administrator", Description = "System administrators" }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task Edit_OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        _mockRoleManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationRole?)null);
        var page = new EditModel(_mockRoleManager.Object) { PageContext = new PageContext() };

        var result = await page.OnGetAsync("not-found");

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Delete ────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_OnGetAsync_LoadsNonAdminRole()
    {
        var role = new ApplicationRole { Id = "role-1", Name = "Editor" };
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockUserManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(new List<ApplicationUser>());

        var page = new DeleteModel(_mockRoleManager.Object, _mockUserManager.Object)
        {
            PageContext = new PageContext()
        };

        var result = await page.OnGetAsync("role-1");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Editor", page.Role.Name);
    }

    [Fact]
    public async Task Delete_OnGetAsync_RefusesAdministrator()
    {
        var role = new ApplicationRole { Id = "admin-id", Name = "Administrator" };
        _mockRoleManager.Setup(m => m.FindByIdAsync("admin-id")).ReturnsAsync(role);

        var page = new DeleteModel(_mockRoleManager.Object, _mockUserManager.Object)
        {
            PageContext = new PageContext()
        };

        var result = await page.OnGetAsync("admin-id");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_OnPostAsync_DeletesNonAdminRole()
    {
        var role = new ApplicationRole { Id = "role-1", Name = "Editor" };
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockRoleManager.Setup(m => m.DeleteAsync(role)).ReturnsAsync(IdentityResult.Success);

        var page = new DeleteModel(_mockRoleManager.Object, _mockUserManager.Object)
        {
            PageContext = new PageContext()
        };

        var result = await page.OnPostAsync("role-1");

        Assert.IsType<RedirectToPageResult>(result);
        _mockRoleManager.Verify(m => m.DeleteAsync(role), Times.Once);
    }

    [Fact]
    public async Task Delete_OnPostAsync_RefusesAdministrator()
    {
        var role = new ApplicationRole { Id = "admin-id", Name = "Administrator" };
        _mockRoleManager.Setup(m => m.FindByIdAsync("admin-id")).ReturnsAsync(role);

        var page = new DeleteModel(_mockRoleManager.Object, _mockUserManager.Object)
        {
            PageContext = new PageContext()
        };

        var result = await page.OnPostAsync("admin-id");

        Assert.IsType<BadRequestObjectResult>(result);
        _mockRoleManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationRole>()), Times.Never);
    }

    [Fact]
    public async Task Delete_OnPostAsync_IsCaseInsensitiveForAdministrator()
    {
        var role = new ApplicationRole { Id = "admin-id", Name = "ADMINISTRATOR" };
        _mockRoleManager.Setup(m => m.FindByIdAsync("admin-id")).ReturnsAsync(role);

        var page = new DeleteModel(_mockRoleManager.Object, _mockUserManager.Object)
        {
            PageContext = new PageContext()
        };

        var result = await page.OnPostAsync("admin-id");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_OnPostAsync_PropagatesIdentityErrors()
    {
        var role = new ApplicationRole { Id = "role-1", Name = "Editor" };
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockRoleManager.Setup(m => m.DeleteAsync(role))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "nope" }));

        var page = new DeleteModel(_mockRoleManager.Object, _mockUserManager.Object)
        {
            PageContext = new PageContext()
        };

        var result = await page.OnPostAsync("role-1");

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
    }
}
