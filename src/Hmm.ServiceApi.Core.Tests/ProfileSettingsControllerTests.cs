using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.ProfileService.Controllers;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Hmm.ServiceApi.Core.Tests;

/// <summary>
/// Controller coverage for Phase P2 (<c>/v1/profile/settings</c>):
/// 204-when-absent, 200 round-trip, the raw-bundle PUT path (author
/// scoping + envelope <c>lastModified</c> extraction), bad-body
/// rejection, and auth scoping. The last-writer-wins logic itself is
/// covered at the manager level (Phase P1).
/// </summary>
public class ProfileSettingsControllerTests
{
    private const int AuthorId = 7;

    private readonly Mock<IUserSettingsManager> _manager = new();
    private readonly Mock<ICurrentUserAuthorProvider> _authorProvider = new();

    private ProfileSettingsController BuildController(string? body = null)
    {
        var controller = new ProfileSettingsController(
            _manager.Object,
            _authorProvider.Object,
            NullLogger<ProfileSettingsController>.Instance);

        var httpContext = new DefaultHttpContext();
        if (body != null)
        {
            httpContext.Request.Body =
                new System.IO.MemoryStream(Encoding.UTF8.GetBytes(body));
            httpContext.Request.ContentType = "application/json";
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private void SetupOwner() =>
        _authorProvider.Setup(p => p.GetCurrentUserAuthorAsync())
            .ReturnsAsync(ProcessingResult<Author>.Ok(
                new Author { Id = AuthorId, AccountName = "owner" }));

    private void SetupUnauthenticated() =>
        _authorProvider.Setup(p => p.GetCurrentUserAuthorAsync())
            .ReturnsAsync(ProcessingResult<Author>.Fail(
                "no user", ErrorCategory.Unauthorized));

    // ---- GET ----

    [Fact]
    public async Task Get_returns_204_when_no_settings_stored()
    {
        SetupOwner();
        _manager.Setup(m => m.GetByAuthorIdAsync(AuthorId))
            .ReturnsAsync(ProcessingResult<AuthorSettings>.EmptyOk("none"));

        var result = await BuildController().Get();

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Get_returns_200_with_stored_bundle_verbatim()
    {
        const string bundle = "{\"gasLog\":{},\"lastModified\":\"2026-05-29T18:04:11.000Z\",\"_v\":1}";
        SetupOwner();
        _manager.Setup(m => m.GetByAuthorIdAsync(AuthorId))
            .ReturnsAsync(ProcessingResult<AuthorSettings>.Ok(
                new AuthorSettings { AuthorId = AuthorId, SettingsJson = bundle }));

        var result = await BuildController().Get();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(bundle, content.Content);
        Assert.Equal("application/json", content.ContentType);
    }

    [Fact]
    public async Task Get_returns_401_when_unauthenticated()
    {
        SetupUnauthenticated();
        var result = await BuildController().Get();
        Assert.IsType<UnauthorizedResult>(result);
    }

    // ---- PUT ----

    [Fact]
    public async Task Put_stores_body_verbatim_with_scoped_author_and_parsed_stamp()
    {
        const string bundle = "{\"gasLog\":{},\"lastModified\":\"2026-05-29T18:04:11.000Z\",\"_v\":1}";
        var expectedStamp = DateTimeOffset
            .Parse("2026-05-29T18:04:11.000Z").UtcDateTime;

        SetupOwner();
        AuthorSettings? captured = null;
        _manager.Setup(m => m.UpsertAsync(It.IsAny<AuthorSettings>()))
            .ReturnsAsync((AuthorSettings s) =>
            {
                captured = s;
                return ProcessingResult<AuthorSettings>.Ok(s);
            });

        var result = await BuildController(bundle).Put(CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(bundle, content.Content);
        Assert.NotNull(captured);
        Assert.Equal(AuthorId, captured!.AuthorId);   // scoped to the token, not the body
        Assert.Equal(bundle, captured.SettingsJson);   // stored verbatim
        Assert.Equal(expectedStamp, captured.LastModified);
    }

    [Fact]
    public async Task Put_without_lastModified_defaults_to_epoch()
    {
        const string bundle = "{\"gasLog\":{},\"_v\":1}";
        SetupOwner();
        AuthorSettings? captured = null;
        _manager.Setup(m => m.UpsertAsync(It.IsAny<AuthorSettings>()))
            .ReturnsAsync((AuthorSettings s) =>
            {
                captured = s;
                return ProcessingResult<AuthorSettings>.Ok(s);
            });

        await BuildController(bundle).Put(CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(DateTime.UnixEpoch, captured!.LastModified);
    }

    [Fact]
    public async Task Put_returns_stored_bundle_which_may_differ_on_stale_write()
    {
        // The manager applies the guard and echoes the STORED bundle;
        // the controller just relays it. Simulate a stale write whose
        // stored value is the newer one.
        const string staleBody = "{\"gasLog\":{},\"lastModified\":\"2026-05-01T00:00:00.000Z\"}";
        const string storedNewer = "{\"gasLog\":{},\"lastModified\":\"2026-05-29T18:04:11.000Z\"}";
        SetupOwner();
        _manager.Setup(m => m.UpsertAsync(It.IsAny<AuthorSettings>()))
            .ReturnsAsync(ProcessingResult<AuthorSettings>.Ok(
                new AuthorSettings { AuthorId = AuthorId, SettingsJson = storedNewer }));

        var result = await BuildController(staleBody).Put(CancellationToken.None);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(storedNewer, content.Content);
    }

    [Fact]
    public async Task Put_rejects_empty_body()
    {
        SetupOwner();
        var result = await BuildController("").Put(CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Put_rejects_invalid_json()
    {
        SetupOwner();
        var result = await BuildController("not json").Put(CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Put_rejects_non_object_json()
    {
        SetupOwner();
        var result = await BuildController("[1,2,3]").Put(CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Put_returns_401_when_unauthenticated()
    {
        SetupUnauthenticated();
        var result = await BuildController("{}").Put(CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }
}
