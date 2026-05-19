#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Core;
using Hmm.Core.Map.Migration;
using Hmm.Core.Vault;
using Hmm.ServiceApi.DtoEntity.Migration;
using Hmm.ServiceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hmm.ServiceApi.Areas.MigrationService.Controllers
{
    /// <summary>
    /// Cross-note bulk migration endpoints. Four verbs:
    /// <list type="bullet">
    ///   <item><c>POST /v1/migration/upload</c> — Free → Paid push</item>
    ///   <item><c>POST /v1/migration/replace</c> — wipe-then-upload</item>
    ///   <item><c>GET /v1/migration/export</c> — zip download (records + vault)</item>
    ///   <item><c>GET /v1/migration/log</c> — recent audit rows</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Wire shape for upload + replace: <c>multipart/form-data</c>
    /// with one <c>manifest</c> text field (the
    /// <see cref="ApiMigrationEnvelope"/> JSON) plus one
    /// <c>IFormFile</c> per vault blob. Each blob's
    /// <c>FormFile.Name</c> (or <c>FileName</c>) is the vault
    /// relative path — e.g. <c>attachments/note-7/abc.jpg</c>.
    /// </para>
    /// <para>
    /// Export returns <c>application/zip</c>; the zip contains
    /// <c>records.json</c> at the root plus every vault file at its
    /// full relative path. Mirrors the live vault layout so the
    /// client can drop it straight into a local vault root.
    /// </para>
    /// <para>
    /// Subscription gating (Grace → read+delete, Lapsed → export
    /// only) is deferred to the same future
    /// <c>RequireActiveSubscriptionAttribute</c> the
    /// <c>NoteVaultController</c> will adopt. Today every
    /// authenticated author can run all four endpoints.
    /// </para>
    /// </remarks>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/migration")]
    [Produces("application/json")]
    public class MigrationController : Controller
    {
        private readonly IMigrationManager _manager;
        private readonly ICurrentUserAuthorProvider _authorProvider;
        private readonly IMapper _mapper;
        private readonly AttachmentSettings _settings;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(
            IMigrationManager manager,
            ICurrentUserAuthorProvider authorProvider,
            IMapper mapper,
            IOptions<AttachmentSettings> settings,
            ILogger<MigrationController> logger)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(authorProvider);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(logger);

            _manager = manager;
            _authorProvider = authorProvider;
            _mapper = mapper;
            _settings = settings.Value;
            _logger = logger;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(long.MaxValue)]
        [ProducesResponseType(typeof(ApiMigrationUploadResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Upload(CancellationToken cancellationToken)
            => await IngestAsync(replace: false, cancellationToken);

        [HttpPost("replace")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(long.MaxValue)]
        [ProducesResponseType(typeof(ApiMigrationUploadResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Replace(CancellationToken cancellationToken)
            => await IngestAsync(replace: true, cancellationToken);

        [HttpGet("export")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Export(CancellationToken cancellationToken)
        {
            var authorResult = await _authorProvider.GetCurrentUserAuthorAsync();
            if (!authorResult.Success || authorResult.Value == null)
            {
                return Unauthorized();
            }
            var authorId = authorResult.Value.Id;

            // Buffer to memory first. The zip is small enough at v1
            // limits (worst case: 1k notes ≈ a few MB JSON +
            // attachment cap × file count ≈ tens of MB). Switch to
            // a streaming Response.Body write if/when the cap goes
            // up.
            using var ms = new MemoryStream();
            var deviceIdentifier = Request.Headers["X-Device-Identifier"].ToString();
            var result = await _manager.ExportAsync(
                authorId, ms,
                string.IsNullOrWhiteSpace(deviceIdentifier) ? null : deviceIdentifier,
                cancellationToken);
            if (!result.Success || result.Value == null)
            {
                _logger.LogError(
                    "Export failed for author {AuthorId}: {Error}",
                    authorId, result.ErrorMessage);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError(
                        "Export failed.", HttpContext));
            }

            return File(
                ms.ToArray(),
                "application/zip",
                fileDownloadName: $"hmm-export-{authorId}.zip");
        }

        [HttpGet("log")]
        [ProducesResponseType(typeof(IReadOnlyList<ApiMigrationLog>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetLog(
            [FromQuery] int take = 20,
            CancellationToken cancellationToken = default)
        {
            var authorResult = await _authorProvider.GetCurrentUserAuthorAsync();
            if (!authorResult.Success || authorResult.Value == null)
            {
                return Unauthorized();
            }
            var logResult = await _manager.GetLogAsync(
                authorResult.Value.Id, take, cancellationToken);
            if (!logResult.Success || logResult.Value == null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError(
                        "Failed to read log.", HttpContext));
            }
            var apiLogs = logResult.Value
                .Select(l => _mapper.Map<ApiMigrationLog>(l))
                .ToList();
            return Ok(apiLogs);
        }

        // ============================================================
        // Helpers
        // ============================================================

        private async Task<IActionResult> IngestAsync(
            bool replace, CancellationToken cancellationToken)
        {
            var authorResult = await _authorProvider.GetCurrentUserAuthorAsync();
            if (!authorResult.Success || authorResult.Value == null)
            {
                return Unauthorized();
            }
            var authorId = authorResult.Value.Id;

            if (!Request.HasFormContentType)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest(
                    "Expected multipart/form-data with a \"manifest\" field.",
                    HttpContext));
            }

            var form = await Request.ReadFormAsync(cancellationToken);
            var manifestRaw = form["manifest"].ToString();
            if (string.IsNullOrWhiteSpace(manifestRaw))
            {
                return BadRequest(ProblemDetailsHelper.BadRequest(
                    "Missing \"manifest\" form field.", HttpContext));
            }

            ApiMigrationEnvelope? apiEnvelope;
            try
            {
                apiEnvelope = JsonSerializer.Deserialize<ApiMigrationEnvelope>(
                    manifestRaw,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
            }
            catch (JsonException ex)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest(
                    $"Malformed manifest JSON: {ex.Message}", HttpContext));
            }
            if (apiEnvelope == null)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest(
                    "Empty manifest.", HttpContext));
            }

            // Map DTO → manager input.
            var envelope = _mapper.Map<MigrationEnvelope>(apiEnvelope);

            // Vault blobs: every form file is treated as a vault
            // file. The form field NAME carries the vault relative
            // path so the client doesn't have to fight the "file
            // name" sanitisation some HTTP libs do.
            var blobs = new List<MigrationVaultBlob>();
            foreach (var file in form.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = string.IsNullOrWhiteSpace(file.Name)
                    ? file.FileName
                    : file.Name;
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    return BadRequest(ProblemDetailsHelper.BadRequest(
                        "File part missing both Name and FileName.",
                        HttpContext));
                }
                if (file.Length > _settings.MaxBytes)
                {
                    return BadRequest(ProblemDetailsHelper.BadRequest(
                        $"Blob {relativePath} exceeds max size {_settings.MaxBytes}.",
                        HttpContext));
                }
                using var fs = file.OpenReadStream();
                using var bs = new MemoryStream();
                await fs.CopyToAsync(bs, cancellationToken);
                blobs.Add(new MigrationVaultBlob
                {
                    RelativePath = relativePath,
                    ContentType = file.ContentType ?? string.Empty,
                    Bytes = bs.ToArray(),
                });
            }

            var managerResult = replace
                ? await _manager.ReplaceAsync(authorId, envelope, blobs, cancellationToken)
                : await _manager.UploadAsync(authorId, envelope, blobs, cancellationToken);

            if (!managerResult.Success || managerResult.Value == null)
            {
                _logger.LogError(
                    "Migration {Kind} failed for author {AuthorId}: {Error}",
                    replace ? "replace" : "upload",
                    authorId, managerResult.ErrorMessage);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError(
                        "Migration failed.", HttpContext));
            }

            var apiResult = _mapper.Map<ApiMigrationUploadResult>(managerResult.Value);
            return Ok(apiResult);
        }
    }
}
