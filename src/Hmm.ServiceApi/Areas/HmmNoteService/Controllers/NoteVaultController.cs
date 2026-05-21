#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hmm.Core;
using Hmm.Core.Vault;
using Hmm.ServiceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Controllers
{
    /// <summary>
    /// Per-note attachment vault. Five verbs: POST / GET / HEAD /
    /// DELETE on a single file, and a no-suffix GET to list a note's
    /// vault contents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Routes nest under <c>/v1/notes/{noteId}/vault/</c> on purpose —
    /// vault files belong to a specific note, so the existing
    /// "does this JWT own note N?" check (resolved through
    /// <see cref="IHmmNoteManager.GetNoteByIdAsync(int, bool)"/> +
    /// <see cref="ICurrentUserAuthorProvider"/>) extends naturally
    /// from the note endpoints to the vault. See
    /// <c>docs/attachments-design.md</c> §"API surface" for the
    /// design.
    /// </para>
    /// <para>
    /// The within-note filename in the URL is whatever the client
    /// chose (typically a UUID + extension). The server reconstructs
    /// the full vault relative path as
    /// <c>attachments/note-{noteId}/{filename}</c> and validates it
    /// via <see cref="VaultPathUtil.Validate(string)"/> before
    /// writing.
    /// </para>
    /// <para>
    /// Subscription gating ("Grace" → read+delete only; "Lapsed" →
    /// export-only) is deferred — the subscription model isn't in
    /// the codebase yet. Today every authenticated owner can
    /// read/write. Add a <c>RequireActiveSubscriptionAttribute</c>
    /// here when that model lands.
    /// </para>
    /// </remarks>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/notes/{noteId:int}/vault")]
    [Produces("application/json")]
    public class NoteVaultController : Controller
    {
        private readonly IVaultBlobStore _store;
        private readonly AttachmentSettings _settings;
        private readonly ICurrentUserAuthorProvider _authorProvider;
        private readonly IHmmNoteManager _noteManager;
        private readonly ILogger<NoteVaultController> _logger;

        public NoteVaultController(
            IVaultBlobStore store,
            IOptions<AttachmentSettings> settings,
            ICurrentUserAuthorProvider authorProvider,
            IHmmNoteManager noteManager,
            ILogger<NoteVaultController> logger)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(authorProvider);
            ArgumentNullException.ThrowIfNull(noteManager);
            ArgumentNullException.ThrowIfNull(logger);

            _store = store;
            _settings = settings.Value;
            _authorProvider = authorProvider;
            _noteManager = noteManager;
            _logger = logger;
        }

        /// <summary>
        /// Upload (or overwrite) the bytes at
        /// <c>attachments/note-{noteId}/{filename}</c>. Body is the
        /// raw file; <c>Content-Type</c> header is required and must
        /// be in the configured allow-list.
        /// </summary>
        [HttpPost("{*filename}")]
        [Consumes("image/jpeg", "image/png", "image/heic", "image/webp")]
        [ProducesResponseType(typeof(VaultRef), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status415UnsupportedMediaType)]
        public async Task<IActionResult> Put(
            int noteId,
            string filename,
            CancellationToken cancellationToken)
        {
            var ownership = await ResolveOwnerAsync(noteId);
            if (ownership.Error != null) return ownership.Error;
            var authorId = ownership.AuthorId!.Value;

            var contentType = Request.ContentType ?? string.Empty;
            // Trim parameters like "; charset=..." just in case.
            var semi = contentType.IndexOf(';');
            if (semi >= 0) contentType = contentType[..semi].Trim();

            if (!_settings.AllowedContentTypes.Contains(contentType))
            {
                return StatusCode(
                    StatusCodes.Status415UnsupportedMediaType,
                    ProblemDetailsHelper.UnsupportedMediaType(
                        $"Content-Type \"{contentType}\" is not allowed. "
                        + $"Allowed: {string.Join(", ", _settings.AllowedContentTypes)}.",
                        HttpContext));
            }

            // Pre-check Content-Length when supplied. Cheap fast-fail
            // for oversized uploads; the post-read check is the
            // authoritative one (some clients omit Content-Length).
            if (Request.ContentLength is long advertised
                && advertised > _settings.MaxBytes)
            {
                return StatusCode(
                    StatusCodes.Status413PayloadTooLarge,
                    ProblemDetailsHelper.PayloadTooLarge(
                        $"Upload exceeds max size of {_settings.MaxBytes} bytes.",
                        HttpContext));
            }

            // Buffer to memory bounded by MaxBytes + 1 so an oversized
            // upload bails the moment it crosses the threshold. v1
            // doesn't need true streaming (8 MB cap, ~100 MB headroom).
            using var ms = new MemoryStream();
            var cap = _settings.MaxBytes + 1;
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await Request.Body
                .ReadAsync(buffer, cancellationToken)
                .ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > cap)
                {
                    return StatusCode(
                        StatusCodes.Status413PayloadTooLarge,
                        ProblemDetailsHelper.PayloadTooLarge(
                            $"Upload exceeds max size of {_settings.MaxBytes} bytes.",
                            HttpContext));
                }
                await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                    .ConfigureAwait(false);
            }
            var bytes = ms.ToArray();
            if (bytes.Length == 0)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest(
                    "Empty body.", HttpContext));
            }

            string relativePath;
            try
            {
                relativePath = VaultPathUtil.Join(new[]
                {
                    "attachments",
                    $"note-{noteId}",
                    filename,
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest(
                    $"Invalid filename: {ex.Message}", HttpContext));
            }

            try
            {
                await _store.PutBytesAsync(
                    authorId, relativePath, bytes, contentType, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest(
                    $"Invalid vault path: {ex.Message}", HttpContext));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to write vault file {Path} for author {AuthorId} on note {NoteId}. TraceId: {TraceId}",
                    relativePath, authorId, noteId, HttpContext.TraceIdentifier);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ProblemDetailsHelper.InternalServerError(
                        "Failed to store the file.", HttpContext));
            }

            return Ok(new VaultRef
            {
                Path = relativePath,
                ContentType = contentType,
                ByteSize = bytes.Length,
            });
        }

        /// <summary>
        /// Stream the bytes at
        /// <c>attachments/note-{noteId}/{filename}</c>. 404 if the
        /// note isn't owned by the JWT subject or the file doesn't
        /// exist (don't leak existence between authors).
        /// </summary>
        [HttpGet("{*filename}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(
            int noteId,
            string filename,
            CancellationToken cancellationToken)
        {
            var ownership = await ResolveOwnerAsync(noteId);
            if (ownership.Error != null) return ownership.Error;
            var authorId = ownership.AuthorId!.Value;

            var relativePath = TryAssemblePath(noteId, filename, out var err);
            if (err != null) return err;

            byte[]? bytes;
            try
            {
                bytes = await _store.GetBytesAsync(
                    authorId, relativePath, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ArgumentException)
            {
                return NotFound(ProblemDetailsHelper.NotFound(
                    "Attachment not found.", HttpContext));
            }
            if (bytes == null)
            {
                return NotFound(ProblemDetailsHelper.NotFound(
                    "Attachment not found.", HttpContext));
            }

            // No persisted Content-Type yet (Phase 5 part 2 stores it
            // alongside the file in extended attributes). Infer from
            // the extension; the allow-list keeps this tractable.
            var contentType = GuessContentType(filename)
                ?? "application/octet-stream";
            return File(bytes, contentType);
        }

        /// <summary>
        /// HEAD existence + size check. 200 with no body if present,
        /// 404 otherwise. Mirrors S3 / OneDrive semantics.
        /// </summary>
        [HttpHead("{*filename}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Head(
            int noteId,
            string filename,
            CancellationToken cancellationToken)
        {
            var ownership = await ResolveOwnerAsync(noteId);
            if (ownership.Error != null) return ownership.Error;
            var authorId = ownership.AuthorId!.Value;

            var relativePath = TryAssemblePath(noteId, filename, out var err);
            if (err != null) return err;

            try
            {
                var exists = await _store.ExistsAsync(
                    authorId, relativePath, cancellationToken)
                    .ConfigureAwait(false);
                if (!exists)
                {
                    return NotFound(ProblemDetailsHelper.NotFound(
                        "Attachment not found.", HttpContext));
                }
            }
            catch (ArgumentException)
            {
                return NotFound(ProblemDetailsHelper.NotFound(
                    "Attachment not found.", HttpContext));
            }
            return Ok();
        }

        /// <summary>
        /// Delete a single vault file. Returns 204 if it existed,
        /// 204 if it didn't (idempotent — same outcome either way).
        /// </summary>
        [HttpDelete("{*filename}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            int noteId,
            string filename,
            CancellationToken cancellationToken)
        {
            var ownership = await ResolveOwnerAsync(noteId);
            if (ownership.Error != null) return ownership.Error;
            var authorId = ownership.AuthorId!.Value;

            var relativePath = TryAssemblePath(noteId, filename, out var err);
            if (err != null) return err;

            try
            {
                await _store.DeleteAsync(
                    authorId, relativePath, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ArgumentException)
            {
                // Invalid path = treated as not-found (idempotent).
            }
            return NoContent();
        }

        /// <summary>
        /// List every vault file under the note's per-note folder
        /// (<c>attachments/note-{noteId}/</c>). Used by the Replace
        /// flow and per-note GC.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(VaultEntry[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> List(
            int noteId,
            CancellationToken cancellationToken)
        {
            var ownership = await ResolveOwnerAsync(noteId);
            if (ownership.Error != null) return ownership.Error;
            var authorId = ownership.AuthorId!.Value;

            var prefix = $"attachments/note-{noteId}";
            try
            {
                VaultPathUtil.Validate(prefix);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ProblemDetailsHelper.BadRequest(
                    ex.Message, HttpContext));
            }

            var entries = await _store.ListAsync(
                authorId, prefix, cancellationToken)
                .ConfigureAwait(false);
            return Ok(entries);
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------

        /// <summary>
        /// Resolve the JWT subject's author + verify they own
        /// <paramref name="noteId"/>. Returns an error
        /// <see cref="IActionResult"/> on the unhappy path; never
        /// leaks "exists but you don't own it" — both shapes map to
        /// 404.
        /// </summary>
        private async Task<OwnershipResolution> ResolveOwnerAsync(int noteId)
        {
            var authorResult = await _authorProvider
                .GetCurrentUserAuthorAsync()
                .ConfigureAwait(false);
            if (!authorResult.Success || authorResult.Value == null)
            {
                return OwnershipResolution.Failure(Unauthorized());
            }
            var author = authorResult.Value;

            var noteResult = await _noteManager
                .GetNoteByIdAsync(noteId)
                .ConfigureAwait(false);
            if (!noteResult.Success || noteResult.Value == null)
            {
                return OwnershipResolution.Failure(NotFound(
                    ProblemDetailsHelper.NotFound(
                        $"Note {noteId} not found.", HttpContext)));
            }
            if (noteResult.Value.Author == null
                || noteResult.Value.Author.Id != author.Id)
            {
                // Cross-author access is reported as 404 to avoid
                // leaking which note ids exist on other accounts.
                return OwnershipResolution.Failure(NotFound(
                    ProblemDetailsHelper.NotFound(
                        $"Note {noteId} not found.", HttpContext)));
            }

            return OwnershipResolution.Ok(author.Id);
        }

        /// <remarks>
        /// Returns the assembled path on success and sets
        /// <paramref name="error"/> to <c>null</c>; on failure
        /// returns an empty string and sets <paramref name="error"/>
        /// to the BadRequest result. Caller MUST check
        /// <paramref name="error"/> before using the return value.
        /// </remarks>
        private string TryAssemblePath(
            int noteId,
            string filename,
            out IActionResult? error)
        {
            try
            {
                error = null;
                return VaultPathUtil.Join(new[]
                {
                    "attachments",
                    $"note-{noteId}",
                    filename,
                });
            }
            catch (ArgumentException ex)
            {
                error = BadRequest(ProblemDetailsHelper.BadRequest(
                    $"Invalid filename: {ex.Message}", HttpContext));
                return string.Empty;
            }
        }

        private static string? GuessContentType(string filename)
        {
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".heic" or ".heif" => "image/heic",
                ".webp" => "image/webp",
                _ => null,
            };
        }

        private readonly struct OwnershipResolution
        {
            private OwnershipResolution(int? authorId, IActionResult? error)
            {
                AuthorId = authorId;
                Error = error;
            }

            public int? AuthorId { get; }
            public IActionResult? Error { get; }

            public static OwnershipResolution Ok(int authorId)
                => new(authorId, null);
            public static OwnershipResolution Failure(IActionResult error)
                => new(null, error);
        }
    }
}
