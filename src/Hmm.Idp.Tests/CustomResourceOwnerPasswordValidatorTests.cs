using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Hmm.Idp.Pages.Admin.User;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Hmm.Idp.Tests;

/// <summary>
/// Covers the OAuth ROPC validator's branches.
///
/// The validator deliberately uses <see cref="UserManager{T}.CheckPasswordAsync"/>
/// rather than <see cref="SignInManager{T}.CheckPasswordSignInAsync"/> so that
/// SignInOptions like RequireConfirmedEmail don't make it impossible to
/// distinguish wrong-password from right-password-but-unconfirmed. These
/// tests exercise that distinction.
/// </summary>
public class CustomResourceOwnerPasswordValidatorTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager = TestHelpers.CreateMockUserManager();
    private readonly Mock<SignInManager<ApplicationUser>> _signInManager;

    public CustomResourceOwnerPasswordValidatorTests()
    {
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManager = new Mock<SignInManager<ApplicationUser>>(
            _userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<ILogger<SignInManager<ApplicationUser>>>().Object,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<ApplicationUser>>().Object);
    }

    private CustomResourceOwnerPasswordValidator Build() =>
        new(_userManager.Object, _signInManager.Object);

    private static ResourceOwnerPasswordValidationContext Ctx(string user, string pwd) =>
        new()
        {
            UserName = user,
            Password = pwd,
        };

    private static string? SubjectIdOf(GrantValidationResult result) =>
        result.Subject?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

    [Fact]
    public async Task ValidateAsync_UnknownUser_InvalidGrantInvalidUsernameOrPassword()
    {
        _userManager.Setup(m => m.FindByNameAsync("nobody")).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.FindByEmailAsync("nobody")).ReturnsAsync((ApplicationUser?)null);
        var ctx = Ctx("nobody", "anything");

        await Build().ValidateAsync(ctx);

        Assert.True(ctx.Result.IsError);
        Assert.Equal("invalid_username_or_password", ctx.Result.ErrorDescription);
    }

    [Fact]
    public async Task ValidateAsync_InactiveUser_InvalidGrant()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = false, EmailConfirmed = true };
        _userManager.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        var ctx = Ctx("alice", "pw");

        await Build().ValidateAsync(ctx);

        Assert.True(ctx.Result.IsError);
        Assert.Equal("invalid_username_or_password", ctx.Result.ErrorDescription);
    }

    [Fact]
    public async Task ValidateAsync_AlreadyLockedOut_ReturnsAccountLockedBeforePasswordCheck()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = true };
        _userManager.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);
        var ctx = Ctx("alice", "pw");

        await Build().ValidateAsync(ctx);

        Assert.True(ctx.Result.IsError);
        Assert.Equal("account_locked", ctx.Result.ErrorDescription);
        // We bailed early — never even ran the password check.
        _userManager.Verify(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ValidateAsync_ActiveUserCorrectPasswordButEmailNotConfirmed_ReturnsEmailNotConfirmed()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = false };
        _userManager.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "pw")).ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        var ctx = Ctx("alice", "pw");

        await Build().ValidateAsync(ctx);

        Assert.True(ctx.Result.IsError);
        Assert.Equal("email_not_confirmed", ctx.Result.ErrorDescription);
        // Successful password should still reset the failed-attempt counter.
        _userManager.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_ActiveConfirmedUserCorrectPassword_Succeeds()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = true };
        _userManager.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "pw")).ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        var ctx = Ctx("alice", "pw");

        await Build().ValidateAsync(ctx);

        Assert.False(ctx.Result.IsError);
        Assert.Equal("u1", SubjectIdOf(ctx.Result));
    }

    [Fact]
    public async Task ValidateAsync_WrongPassword_TracksFailureAndReturnsInvalidGrant()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = true };
        _userManager.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        _userManager.SetupSequence(m => m.IsLockedOutAsync(user))
                    .ReturnsAsync(false)   // pre-check
                    .ReturnsAsync(false);  // post-AccessFailed (still under threshold)
        _userManager.Setup(m => m.CheckPasswordAsync(user, "pw")).ReturnsAsync(false);
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);
        var ctx = Ctx("alice", "pw");

        await Build().ValidateAsync(ctx);

        Assert.True(ctx.Result.IsError);
        Assert.Equal("invalid_username_or_password", ctx.Result.ErrorDescription);
        _userManager.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WrongPasswordCrossingLockoutThreshold_ReturnsAccountLocked()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = true };
        _userManager.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        _userManager.SetupSequence(m => m.IsLockedOutAsync(user))
                    .ReturnsAsync(false)   // pre-check (still allowed in)
                    .ReturnsAsync(true);   // post-AccessFailed (crossed threshold)
        _userManager.Setup(m => m.CheckPasswordAsync(user, "pw")).ReturnsAsync(false);
        _userManager.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);
        var ctx = Ctx("alice", "pw");

        await Build().ValidateAsync(ctx);

        Assert.True(ctx.Result.IsError);
        Assert.Equal("account_locked", ctx.Result.ErrorDescription);
    }

    [Fact]
    public async Task ValidateAsync_FallsBackToFindByEmailWhenUserNameMisses()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = true, Email = "alice@x.test" };
        _userManager.Setup(m => m.FindByNameAsync("alice@x.test")).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.FindByEmailAsync("alice@x.test")).ReturnsAsync(user);
        _userManager.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        _userManager.Setup(m => m.CheckPasswordAsync(user, "pw")).ReturnsAsync(true);
        _userManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        var ctx = Ctx("alice@x.test", "pw");

        await Build().ValidateAsync(ctx);

        Assert.False(ctx.Result.IsError);
    }
}
