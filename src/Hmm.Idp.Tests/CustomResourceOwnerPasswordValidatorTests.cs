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
/// Covers the OAuth ROPC validator's branches, including the new
/// <c>email_not_confirmed</c> grant-validation result.
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
    public async Task ValidateAsync_ActiveUserCorrectPasswordButEmailNotConfirmed_ReturnsEmailNotConfirmed()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = false };
        _userManager.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        _signInManager.Setup(m => m.CheckPasswordSignInAsync(user, "pw", true))
                      .ReturnsAsync(SignInResult.Success);
        var ctx = Ctx("alice", "pw");

        await Build().ValidateAsync(ctx);

        Assert.True(ctx.Result.IsError);
        Assert.Equal("email_not_confirmed", ctx.Result.ErrorDescription);
    }

    [Fact]
    public async Task ValidateAsync_ActiveConfirmedUserCorrectPassword_Succeeds()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = true };
        _userManager.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        _signInManager.Setup(m => m.CheckPasswordSignInAsync(user, "pw", true))
                      .ReturnsAsync(SignInResult.Success);
        var ctx = Ctx("alice", "pw");

        await Build().ValidateAsync(ctx);

        Assert.False(ctx.Result.IsError);
        Assert.Equal("u1", SubjectIdOf(ctx.Result));
    }

    [Fact]
    public async Task ValidateAsync_LockedOut_ReturnsAccountLocked()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = true };
        _userManager.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        _signInManager.Setup(m => m.CheckPasswordSignInAsync(user, "pw", true))
                      .ReturnsAsync(SignInResult.LockedOut);
        var ctx = Ctx("alice", "pw");

        await Build().ValidateAsync(ctx);

        Assert.True(ctx.Result.IsError);
        Assert.Equal("account_locked", ctx.Result.ErrorDescription);
    }

    [Fact]
    public async Task ValidateAsync_WrongPassword_InvalidGrant()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = true };
        _userManager.Setup(m => m.FindByNameAsync("alice")).ReturnsAsync(user);
        _signInManager.Setup(m => m.CheckPasswordSignInAsync(user, "pw", true))
                      .ReturnsAsync(SignInResult.Failed);
        var ctx = Ctx("alice", "pw");

        await Build().ValidateAsync(ctx);

        Assert.True(ctx.Result.IsError);
        Assert.Equal("invalid_username_or_password", ctx.Result.ErrorDescription);
    }

    [Fact]
    public async Task ValidateAsync_FallsBackToFindByEmailWhenUserNameMisses()
    {
        var user = new ApplicationUser { Id = "u1", UserName = "alice", IsActive = true, EmailConfirmed = true, Email = "alice@x.test" };
        _userManager.Setup(m => m.FindByNameAsync("alice@x.test")).ReturnsAsync((ApplicationUser?)null);
        _userManager.Setup(m => m.FindByEmailAsync("alice@x.test")).ReturnsAsync(user);
        _signInManager.Setup(m => m.CheckPasswordSignInAsync(user, "pw", true))
                      .ReturnsAsync(SignInResult.Success);
        var ctx = Ctx("alice@x.test", "pw");

        await Build().ValidateAsync(ctx);

        Assert.False(ctx.Result.IsError);
    }
}
