using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Map.Migration;
using Hmm.Core.Vault;
using Hmm.ServiceApi.Areas.MigrationService.Controllers;
using Hmm.ServiceApi.DtoEntity.Migration;
using Hmm.ServiceApi.DtoEntity.Profiles;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Hmm.ServiceApi.Core.Tests;

/// <summary>
/// HTTP-wiring + DTO-mapping coverage for the new
/// <see cref="MigrationController"/>. The manager is mocked — the
/// actual bulk logic is exercised in
/// <c>Hmm.Core.Tests.MigrationManagerTests</c>.
/// </summary>
public class MigrationControllerTests
{
    private const int AuthorId = 7;

    private readonly Mock<IMigrationManager> _manager = new();
    private readonly Mock<ICurrentUserAuthorProvider> _authorProvider = new();
    private readonly IMapper _mapper;
    private readonly AttachmentSettings _settings = new()
    {
        MaxBytes = 8 * 1024 * 1024,
        AllowedContentTypes = new List<string>
        {
            "image/jpeg", "image/png", "image/heic", "image/webp",
        },
    };

    public MigrationControllerTests()
    {
        var cfg = new MapperConfiguration(
            c => c.AddProfile<ApiMappingProfile>(),
            NullLoggerFactory.Instance);
        _mapper = cfg.CreateMapper();
    }

    private MigrationController BuildController(
        IFormCollection? form = null,
        string? requestMethod = null)
    {
        var controller = new MigrationController(
            _manager.Object,
            _authorProvider.Object,
            _mapper,
            Options.Create(_settings),
            Mock.Of<ILogger<MigrationController>>());

        var httpContext = new DefaultHttpContext();
        if (form != null)
        {
            httpContext.Request.Form = form;
            // Required so HasFormContentType returns true.
            httpContext.Request.ContentType = "multipart/form-data; boundary=test";
        }
        if (requestMethod != null)
        {
            httpContext.Request.Method = requestMethod;
        }
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };
        return controller;
    }

    private void SetupOwner()
    {
        _authorProvider.Setup(p => p.GetCurrentUserAuthorAsync())
            .ReturnsAsync(ProcessingResult<Author>.Ok(
                new Author { Id = AuthorId, AccountName = "owner" }));
    }

    private void SetupUnauthenticated()
    {
        _authorProvider.Setup(p => p.GetCurrentUserAuthorAsync())
            .ReturnsAsync(ProcessingResult<Author>.Fail(
                "no user", ErrorCategory.Unauthorized));
    }

    private static IFormCollection FormWithManifest(string manifestJson)
    {
        var fields = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["manifest"] = manifestJson,
        };
        return new FormCollection(fields);
    }

    // ============================================================
    // Upload
    // ============================================================

    [Fact]
    public async Task Upload_returns_401_when_unauthenticated()
    {
        SetupUnauthenticated();
        var controller = BuildController(FormWithManifest("{}"));

        var result = await controller.Upload(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Upload_returns_400_when_manifest_missing()
    {
        SetupOwner();
        var controller = BuildController(new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));

        var result = await controller.Upload(CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(bad.Value);
        Assert.Contains("manifest", problem.Detail,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upload_returns_400_when_manifest_is_not_json()
    {
        SetupOwner();
        var controller = BuildController(FormWithManifest("not json"));

        var result = await controller.Upload(CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<ProblemDetails>(bad.Value);
    }

    [Fact]
    public async Task Upload_returns_400_when_request_is_not_multipart()
    {
        SetupOwner();
        var controller = BuildController(); // no form, no content-type

        var result = await controller.Upload(CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_calls_manager_and_returns_mapped_result()
    {
        SetupOwner();
        _manager.Setup(m => m.UploadAsync(
                AuthorId,
                It.IsAny<MigrationEnvelope>(),
                It.IsAny<IReadOnlyList<MigrationVaultBlob>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessingResult<MigrationUploadResult>.Ok(
                new MigrationUploadResult
                {
                    NotesPersisted = 3,
                    VaultFilesPersisted = 2,
                    VaultBytes = 1024,
                }));
        var manifest = JsonSerializer.Serialize(new ApiMigrationEnvelope
        {
            DeviceIdentifier = "test",
            Notes = new List<ApiMigrationNoteRecord>(),
        });
        var controller = BuildController(FormWithManifest(manifest));

        var result = await controller.Upload(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ApiMigrationUploadResult>(ok.Value);
        Assert.Equal(3, body.NotesPersisted);
        Assert.Equal(2, body.VaultFilesPersisted);
        Assert.Equal(1024, body.VaultBytes);
    }

    // ============================================================
    // Replace — same wire shape, different manager call
    // ============================================================

    [Fact]
    public async Task Replace_dispatches_to_manager_ReplaceAsync_not_UploadAsync()
    {
        SetupOwner();
        _manager.Setup(m => m.ReplaceAsync(
                AuthorId,
                It.IsAny<MigrationEnvelope>(),
                It.IsAny<IReadOnlyList<MigrationVaultBlob>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessingResult<MigrationUploadResult>.Ok(
                new MigrationUploadResult { NotesPersisted = 1 }));
        var manifest = JsonSerializer.Serialize(new ApiMigrationEnvelope
        {
            Notes = new List<ApiMigrationNoteRecord>(),
        });
        var controller = BuildController(FormWithManifest(manifest));

        await controller.Replace(CancellationToken.None);

        _manager.Verify(m => m.ReplaceAsync(
            AuthorId,
            It.IsAny<MigrationEnvelope>(),
            It.IsAny<IReadOnlyList<MigrationVaultBlob>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _manager.Verify(m => m.UploadAsync(
            It.IsAny<int>(),
            It.IsAny<MigrationEnvelope>(),
            It.IsAny<IReadOnlyList<MigrationVaultBlob>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ============================================================
    // Export
    // ============================================================

    [Fact]
    public async Task Export_returns_zip_file_result()
    {
        SetupOwner();
        _manager.Setup(m => m.ExportAsync(
                AuthorId,
                It.IsAny<Stream>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns((int _, Stream s, string? __, CancellationToken ct) =>
            {
                // Write a minimal zip stub directly into the stream
                // the controller passes. Reproduces the manager's
                // expected behaviour without needing the real impl.
                using (var archive = new ZipArchive(s, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var entry = archive.CreateEntry("records.json");
                    using var es = entry.Open();
                    es.Write(Encoding.UTF8.GetBytes("[]"));
                }
                return Task.FromResult(ProcessingResult<MigrationUploadResult>.Ok(
                    new MigrationUploadResult { NotesPersisted = 0 }));
            });
        var controller = BuildController();

        var result = await controller.Export(CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", file.ContentType);
        Assert.Contains($"hmm-export-{AuthorId}.zip", file.FileDownloadName);
        // Zip header bytes — verifies the controller faithfully
        // forwarded what the manager wrote.
        Assert.Equal(0x50, file.FileContents[0]); // 'P'
        Assert.Equal(0x4B, file.FileContents[1]); // 'K'
    }

    [Fact]
    public async Task Export_returns_401_when_unauthenticated()
    {
        SetupUnauthenticated();
        var controller = BuildController();

        var result = await controller.Export(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    // ============================================================
    // Log
    // ============================================================

    [Fact]
    public async Task GetLog_returns_recent_entries_mapped_to_dto()
    {
        SetupOwner();
        var at = new DateTime(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc);
        _manager.Setup(m => m.GetLogAsync(AuthorId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcessingResult<IReadOnlyList<MigrationLog>>.Ok(
                new List<MigrationLog>
                {
                    new()
                    {
                        Id = 1, AuthorId = AuthorId,
                        Kind = Hmm.Core.Map.DbEntity.MigrationLogKind.UploadFromLocal,
                        At = at,
                        RecordCounts = "{\"notes\":3}",
                    },
                }));
        var controller = BuildController();

        var result = await controller.GetLog(take: 5, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsAssignableFrom<IEnumerable<ApiMigrationLog>>(ok.Value);
        var only = Assert.Single(body);
        Assert.Equal(AuthorId, only.AuthorId);
        Assert.Equal("UploadFromLocal", only.Kind);
        Assert.Equal(at, only.At);
    }
}
