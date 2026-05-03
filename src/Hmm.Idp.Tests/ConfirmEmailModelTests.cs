using System.Text;
using Hmm.Idp.Pages.Account;
using Hmm.Idp.Pages.Admin.User;
using Hmm.Idp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Hmm.Idp.Tests;

/// <summary>
/// Covers the email-verification landing page (<see cref="ConfirmEmailModel"/>):
/// the four failure branches and the happy path. The test surface is just the
/// repository + logger — the page model has no HttpContext dependency on its
/// confirmation flow.
/// </summary>
public class ConfirmEmailModelTests
{
    private readonly Mock<IApplicationUserRepository> _repo = new();
    private readonly Mock<ILogger<ConfirmEmailModel>> _logger = new();
    private readonly IOptions<EmailSettings> _emailSettings =
        Options.Create(new EmailSettings { PostVerificationUrl = "https://homemademessage.com" });

    private ConfirmEmailModel Build() => new(_repo.Object, _emailSettings, _logger.Object);

    private static string EncodeToken(string raw) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));

    [Theory]
    [InlineData(null, "token")]
    [InlineData("", "token")]
    [InlineData("user-1", null)]
    [InlineData("user-1", "")]
    public async Task OnGetAsync_MissingArguments_ShowsGenericFailure(string? userId, string? token)
    {
        var model = Build();

        var result = await model.OnGetAsync(userId!, token!);

        Assert.IsType<PageResult>(result);
        Assert.False(model.Success);
        Assert.Contains("missing required information", model.Message);
        _repo.Verify(r => r.FindBySubjectIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_UnknownUser_ShowsGenericInvalidLink()
    {
        _repo.Setup(r => r.FindBySubjectIdAsync("unknown")).ReturnsAsync((ApplicationUser?)null);
        var model = Build();

        var result = await model.OnGetAsync("unknown", EncodeToken("anything"));

        Assert.IsType<PageResult>(result);
        Assert.False(model.Success);
        Assert.Contains("no longer valid", model.Message);
        _repo.Verify(r => r.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_MalformedBase64UrlToken_ShowsMalformedMessage()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "u@x.test" };
        _repo.Setup(r => r.FindBySubjectIdAsync("user-1")).ReturnsAsync(user);
        var model = Build();

        // "!!!" is not valid base64url and should make the decoder throw FormatException.
        var result = await model.OnGetAsync("user-1", "!!!");

        Assert.IsType<PageResult>(result);
        Assert.False(model.Success);
        Assert.Contains("malformed", model.Message);
        _repo.Verify(r => r.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnGetAsync_RepositoryReturnsFailure_ShowsExpiredMessage()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "u@x.test" };
        _repo.Setup(r => r.FindBySubjectIdAsync("user-1")).ReturnsAsync(user);
        _repo.Setup(r => r.ConfirmEmailAsync(user, "raw-token"))
             .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));
        var model = Build();

        var result = await model.OnGetAsync("user-1", EncodeToken("raw-token"));

        Assert.IsType<PageResult>(result);
        Assert.False(model.Success);
        Assert.Contains("invalid or has expired", model.Message);
    }

    [Fact]
    public async Task OnGetAsync_ValidTokenAndUser_MarksConfirmedAndShowsSuccess()
    {
        var user = new ApplicationUser { Id = "user-1", Email = "u@x.test" };
        _repo.Setup(r => r.FindBySubjectIdAsync("user-1")).ReturnsAsync(user);
        _repo.Setup(r => r.ConfirmEmailAsync(user, "raw-token"))
             .ReturnsAsync(IdentityResult.Success);
        var model = Build();

        var result = await model.OnGetAsync("user-1", EncodeToken("raw-token"));

        Assert.IsType<PageResult>(result);
        Assert.True(model.Success);
        Assert.Contains("verified", model.Message);
        _repo.Verify(r => r.ConfirmEmailAsync(user, "raw-token"), Times.Once);
    }
}
