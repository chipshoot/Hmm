using Hmm.Idp.Pages.Admin.User;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using System.Security.Claims;

namespace Hmm.Idp.Tests;

/// <summary>
/// Tests for the User admin page models: Index (search/paging), Details (unlock / resend
/// verification handlers), Delete, ResetPassword, and Edit (role reconciliation).
/// </summary>
public class UserAdminModelTests
{
    private readonly Mock<IApplicationUserRepository> _mockRepo;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly PasswordPolicyService _passwordPolicy;

    public UserAdminModelTests()
    {
        _mockRepo = new Mock<IApplicationUserRepository>();
        _mockUserManager = TestHelpers.CreateMockUserManager();
        _mockRoleManager = TestHelpers.CreateMockRoleManager();
        _mockEmailService = new Mock<IEmailService>();
        _passwordPolicy = new PasswordPolicyService(new Microsoft.AspNetCore.Identity.PasswordOptions
        {
            RequiredLength = 12,
            RequiredUniqueChars = 6,
            RequireDigit = true,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireNonAlphanumeric = true
        });
    }

    private static ApplicationUser User(string id, string userName = "u", string? email = null)
        => new()
        {
            Id = id,
            UserName = userName,
            Email = email ?? $"{userName}@example.com",
            IsActive = true
        };

    // ── IndexModel ────────────────────────────────────────────────

    [Fact]
    public async Task Index_OnGet_PopulatesRowsAndPaging()
    {
        var users = new List<ApplicationUser>
        {
            User("id-1", "alice"),
            User("id-2", "bob"),
            User("id-3", "carol")
        };
        _mockRepo.Setup(r => r.SearchUsersAsync(null!, 1, 25))
            .ReturnsAsync((users, 57));
        _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string> { "User" });
        _mockUserManager.Setup(m => m.IsLockedOutAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(false);

        var page = new IndexModel(_mockRepo.Object, _mockUserManager.Object)
        {
            PageContext = new PageContext()
        };

        await page.OnGet();

        Assert.Equal(3, page.Users.Count);
        Assert.Equal(57, page.TotalUsers);
        // 57 users at 25/page = 3 pages
        Assert.Equal(3, page.TotalPages);
        Assert.All(page.Users, u => Assert.Equal("User", u.Roles));
    }

    [Fact]
    public async Task Index_OnGet_ForwardsQueryToRepo()
    {
        _mockRepo.Setup(r => r.SearchUsersAsync("alice", 2, 25))
            .ReturnsAsync((new List<ApplicationUser>(), 0))
            .Verifiable();
        _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());

        var page = new IndexModel(_mockRepo.Object, _mockUserManager.Object)
        {
            PageContext = new PageContext(),
            Query = "alice",
            Page = 2
        };

        await page.OnGet();

        _mockRepo.Verify();
    }

    [Theory]
    [InlineData(0, 25, 0)]      // empty
    [InlineData(25, 25, 1)]     // exactly one page
    [InlineData(26, 25, 2)]     // just over — rounds up
    [InlineData(100, 25, 4)]    // exact multiple
    public async Task Index_TotalPages_IsCeilingDivision(int total, int pageSize, int expected)
    {
        _mockRepo.Setup(r => r.SearchUsersAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((new List<ApplicationUser>(), total));
        _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());

        var page = new IndexModel(_mockRepo.Object, _mockUserManager.Object)
        {
            PageContext = new PageContext()
        };
        // IndexModel hardcodes 25
        Assert.Equal(25, pageSize);

        await page.OnGet();

        Assert.Equal(expected, page.TotalPages);
    }

    // ── DetailsModel ──────────────────────────────────────────────

    [Fact]
    public async Task Details_OnGetAsync_PopulatesModel()
    {
        var user = User("id-1", "alice");
        user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(10);
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("id-1")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User", "Editor" });
        _mockUserManager.Setup(m => m.GetClaimsAsync(user))
            .ReturnsAsync(new List<Claim> { new("name", "Alice") });
        _mockUserManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);

        var page = new DetailsModel(_mockRepo.Object, _mockUserManager.Object, _mockEmailService.Object)
        {
            PageContext = new PageContext()
        };

        var result = await page.OnGetAsync("id-1");

        Assert.IsType<PageResult>(result);
        Assert.Equal(user, page.User);
        Assert.Equal(2, page.Roles.Count);
        Assert.Single(page.Claims);
        Assert.True(page.IsLockedOut);
    }

    [Fact]
    public async Task Details_OnGetAsync_ReturnsNotFound_WhenMissing()
    {
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("missing"))
            .ReturnsAsync((ApplicationUser?)null);

        var page = new DetailsModel(_mockRepo.Object, _mockUserManager.Object, _mockEmailService.Object)
        {
            PageContext = new PageContext()
        };

        var result = await page.OnGetAsync("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_OnPostUnlockAsync_UnlocksUser()
    {
        var user = User("id-1");
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("id-1")).ReturnsAsync(user);
        _mockRepo.Setup(r => r.UnlockUserAsync(user)).Returns(Task.CompletedTask).Verifiable();

        var page = new DetailsModel(_mockRepo.Object, _mockUserManager.Object, _mockEmailService.Object)
        {
            PageContext = new PageContext(),
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };

        var result = await page.OnPostUnlockAsync("id-1");

        Assert.IsType<RedirectToPageResult>(result);
        _mockRepo.Verify();
    }

    [Fact]
    public async Task Details_OnPostResendVerificationAsync_SendsEmail_WhenUserHasEmail()
    {
        var user = User("id-1", "alice", "alice@example.com");
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("id-1")).ReturnsAsync(user);
        _mockEmailService.Setup(e => e.SendVerificationEmailAsync(
            "alice@example.com", "id-1", It.IsAny<string>()))
            .ReturnsAsync(true).Verifiable();

        var page = new DetailsModel(_mockRepo.Object, _mockUserManager.Object, _mockEmailService.Object)
        {
            PageContext = new PageContext(),
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };

        var result = await page.OnPostResendVerificationAsync("id-1");

        Assert.IsType<RedirectToPageResult>(result);
        _mockEmailService.Verify();
    }

    [Fact]
    public async Task Details_OnPostResendVerificationAsync_DoesNotSend_WhenUserHasNoEmail()
    {
        var user = User("id-1");
        user.Email = null;
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("id-1")).ReturnsAsync(user);

        var page = new DetailsModel(_mockRepo.Object, _mockUserManager.Object, _mockEmailService.Object)
        {
            PageContext = new PageContext(),
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };

        var result = await page.OnPostResendVerificationAsync("id-1");

        Assert.IsType<RedirectToPageResult>(result);
        _mockEmailService.Verify(e => e.SendVerificationEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── DeleteModel ───────────────────────────────────────────────

    [Fact]
    public async Task Delete_OnGetAsync_LoadsUser()
    {
        var user = User("id-1");
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("id-1")).ReturnsAsync(user);

        var page = new DeleteModel(_mockRepo.Object) { PageContext = new PageContext() };
        var result = await page.OnGetAsync("id-1");

        Assert.IsType<PageResult>(result);
        Assert.Equal(user, page.User);
    }

    [Fact]
    public async Task Delete_OnPostAsync_DeletesAndRedirects()
    {
        _mockRepo.Setup(r => r.DeleteUserAsync("id-1")).ReturnsAsync(true);

        var page = new DeleteModel(_mockRepo.Object) { PageContext = new PageContext() };
        var result = await page.OnPostAsync("id-1");

        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task Delete_OnPostAsync_ReturnsPage_WhenDeleteFails()
    {
        _mockRepo.Setup(r => r.DeleteUserAsync("id-1")).ReturnsAsync(false);
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("id-1")).ReturnsAsync(User("id-1"));

        var page = new DeleteModel(_mockRepo.Object) { PageContext = new PageContext() };
        var result = await page.OnPostAsync("id-1");

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
    }

    // ── ResetPasswordModel ────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_OnPostAsync_SetsNewPassword()
    {
        var user = User("id-1", "alice");
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("id-1")).ReturnsAsync(user);
        _mockRepo.Setup(r => r.SetPasswordAsync(user, "NewPassword123!")).ReturnsAsync(true);

        var page = new ResetPasswordModel(_mockRepo.Object, _passwordPolicy)
        {
            PageContext = new PageContext(),
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()),
            Input = new ResetPasswordModel.InputModel
            {
                SubjectId = "id-1",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _mockRepo.Verify(r => r.SetPasswordAsync(user, "NewPassword123!"), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_OnPostAsync_FailsPolicyCheck_ForWeakPassword()
    {
        var user = User("id-1");
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("id-1")).ReturnsAsync(user);

        var page = new ResetPasswordModel(_mockRepo.Object, _passwordPolicy)
        {
            PageContext = new PageContext(),
            Input = new ResetPasswordModel.InputModel
            {
                SubjectId = "id-1",
                NewPassword = "weakpassword12",  // no uppercase, no special
                ConfirmPassword = "weakpassword12"
            }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        _mockRepo.Verify(r => r.SetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPassword_OnPostAsync_ReturnsNotFound_WhenUserMissing()
    {
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("missing"))
            .ReturnsAsync((ApplicationUser?)null);

        var page = new ResetPasswordModel(_mockRepo.Object, _passwordPolicy)
        {
            PageContext = new PageContext(),
            Input = new ResetPasswordModel.InputModel
            {
                SubjectId = "missing",
                NewPassword = "StrongP@ss1234",
                ConfirmPassword = "StrongP@ss1234"
            }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<NotFoundResult>(result);
    }

    // ── EditModel (role reconciliation) ───────────────────────────

    /// <summary>
    /// Verifies that <see cref="EditModel.OnPostAsync"/> correctly diffs the user's current
    /// roles against the selected roles — adding missing ones, removing stale ones, and
    /// leaving unchanged roles alone. Requires mocking <see cref="RoleManager{T}.Roles"/> with
    /// a real EF InMemory DbSet so the EditModel can enumerate available roles.
    /// </summary>
    [Fact]
    public async Task Edit_OnPostAsync_ReconcilesRoleMembership()
    {
        // Arrange: user currently in ["User", "Editor"], we want ["User", "Admin"]
        var user = User("id-1", "alice", "alice@example.com");
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("id-1")).ReturnsAsync(user);

        _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.GetClaimsAsync(user))
            .ReturnsAsync(new List<Claim>());
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User", "Editor" });
        _mockUserManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success).Verifiable();
        _mockUserManager.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success).Verifiable();

        // Edit page enumerates RoleManager.Roles — mock it with an in-memory DbSet
        using var dbContext = TestHelpers.CreateApplicationDbContext();
        dbContext.Roles.Add(new ApplicationRole { Id = "r1", Name = "User" });
        dbContext.Roles.Add(new ApplicationRole { Id = "r2", Name = "Editor" });
        dbContext.Roles.Add(new ApplicationRole { Id = "r3", Name = "Admin" });
        await dbContext.SaveChangesAsync();
        _mockRoleManager.Setup(m => m.Roles).Returns(() => dbContext.Roles);

        var page = new EditModel(_mockRepo.Object, _mockUserManager.Object, _mockRoleManager.Object)
        {
            PageContext = new PageContext(),
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()),
            Input = new EditModel.InputModel
            {
                SubjectId = "id-1",
                Username = "alice",
                Email = "alice@example.com",
                IsActive = true,
                SelectedRoles = new List<string> { "User", "Admin" }
            }
        };

        // Act
        var result = await page.OnPostAsync();

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        // "Editor" was removed, "Admin" was added, "User" left alone
        _mockUserManager.Verify(m => m.RemoveFromRolesAsync(
            user,
            It.Is<IEnumerable<string>>(roles => roles.Count() == 1 && roles.Contains("Editor"))),
            Times.Once);
        _mockUserManager.Verify(m => m.AddToRolesAsync(
            user,
            It.Is<IEnumerable<string>>(roles => roles.Count() == 1 && roles.Contains("Admin"))),
            Times.Once);
    }

    [Fact]
    public async Task Edit_OnPostAsync_AddsNewPassword_WhenProvided()
    {
        var user = User("id-1");
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("id-1")).ReturnsAsync(user);
        _mockRepo.Setup(r => r.SetPasswordAsync(user, "NewPass123!")).ReturnsAsync(true).Verifiable();

        _mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(new List<Claim>());
        _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

        using var dbContext = TestHelpers.CreateApplicationDbContext();
        _mockRoleManager.Setup(m => m.Roles).Returns(() => dbContext.Roles);

        var page = new EditModel(_mockRepo.Object, _mockUserManager.Object, _mockRoleManager.Object)
        {
            PageContext = new PageContext(),
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()),
            Input = new EditModel.InputModel
            {
                SubjectId = "id-1",
                Username = "u",
                Password = "NewPass123!",
                SelectedRoles = new List<string>()
            }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        _mockRepo.Verify();
    }

    [Fact]
    public async Task Edit_OnPostAsync_ReturnsNotFound_WhenUserMissing()
    {
        _mockRepo.Setup(r => r.FindBySubjectIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);

        var page = new EditModel(_mockRepo.Object, _mockUserManager.Object, _mockRoleManager.Object)
        {
            PageContext = new PageContext(),
            Input = new EditModel.InputModel
            {
                SubjectId = "missing",
                Username = "u"
            }
        };

        var result = await page.OnPostAsync();

        Assert.IsType<NotFoundResult>(result);
    }
}
