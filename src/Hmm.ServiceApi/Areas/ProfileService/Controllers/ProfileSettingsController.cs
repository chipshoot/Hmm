#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hmm.ServiceApi.Areas.ProfileService.Controllers
{
    /// <summary>
    /// Per-author settings sync for the cloudApi tier — the two verbs
    /// the Flutter <c>ApiSyncProvider</c> calls (Phase P3). See
    /// <c>docs/user-profile-settings-sync.md</c>.
    /// <list type="bullet">
    ///   <item><c>GET  /v1/profile/settings</c> — the caller's bundle, or 204 if none</item>
    ///   <item><c>PUT  /v1/profile/settings</c> — upsert (last-writer-wins)</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Self-scoped: the author is resolved from the JWT subject, never
    /// a route parameter — there is no cross-user access.
    /// </para>
    /// <para>
    /// The body is the client's <c>SyncableSettings</c> bundle, stored
    /// and returned VERBATIM. The server interprets exactly one field —
    /// the envelope <c>lastModified</c> — used for the
    /// last-writer-wins guard in <see cref="IUserSettingsManager"/>.
    /// That keeps the contract stable: the client can add preferences
    /// (bumping the bundle's <c>_v</c>) with zero server changes.
    /// </para>
    /// <para>
    /// Subscription gating is deferred to the same future
    /// <c>RequireActiveSubscriptionAttribute</c> the
    /// <c>NoteVaultController</c> / <c>MigrationController</c> will
    /// adopt; today every authenticated author can read and write.
    /// </para>
    /// </remarks>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/profile/settings")]
    [Produces("application/json")]
    public class ProfileSettingsController : Controller
    {
        private readonly IUserSettingsManager _manager;
        private readonly ICurrentUserAuthorProvider _authorProvider;
        private readonly ILogger<ProfileSettingsController> _logger;

        public ProfileSettingsController(
            IUserSettingsManager manager,
            ICurrentUserAuthorProvider authorProvider,
            ILogger<ProfileSettingsController> logger)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(authorProvider);
            ArgumentNullException.ThrowIfNull(logger);

            _manager = manager;
            _authorProvider = authorProvider;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Get()
        {
            var authorId = await ResolveAuthorIdAsync();
            if (authorId == null)
            {
                return Unauthorized();
            }

            var result = await _manager.GetByAuthorIdAsync(authorId.Value);
            if (!result.Success)
            {
                return Problem(
                    detail: FirstMessage(result),
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            if (result.Value == null)
            {
                // Cloud has nothing yet — the Dart pullSettings contract
                // treats 204 as "seed from local".
                return NoContent();
            }
            return Content(result.Value.SettingsJson ?? "{}", "application/json");
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Put(CancellationToken cancellationToken)
        {
            var authorId = await ResolveAuthorIdAsync();
            if (authorId == null)
            {
                return Unauthorized();
            }

            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync(cancellationToken);
            }
            if (string.IsNullOrWhiteSpace(body))
            {
                return BadRequest("Empty settings body.");
            }

            DateTime lastModified;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return BadRequest("Settings body must be a JSON object.");
                }
                lastModified = ExtractLastModified(doc.RootElement);
            }
            catch (JsonException)
            {
                return BadRequest("Settings body is not valid JSON.");
            }

            var result = await _manager.UpsertAsync(new AuthorSettings
            {
                AuthorId = authorId.Value,
                SettingsJson = body,
                LastModified = lastModified,
            });
            if (!result.Success || result.Value == null)
            {
                return Problem(
                    detail: FirstMessage(result),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            // Return the stored bundle. After the monotonicity guard this
            // may be the previously-stored (newer) one, which lets a
            // losing client notice it should pull.
            return Content(result.Value.SettingsJson ?? "{}", "application/json");
        }

        private async Task<int?> ResolveAuthorIdAsync()
        {
            var authorResult = await _authorProvider.GetCurrentUserAuthorAsync();
            if (!authorResult.Success || authorResult.Value == null)
            {
                return null;
            }
            return authorResult.Value.Id;
        }

        // The server reads exactly one field of the opaque bundle: the
        // envelope `lastModified`. Absent/unparseable => Unix epoch, so a
        // bundle with no real stamp loses to any stored one.
        private static DateTime ExtractLastModified(JsonElement root)
        {
            if (root.TryGetProperty("lastModified", out var lm) &&
                lm.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(
                    lm.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                    out var dto))
            {
                return dto.UtcDateTime;
            }
            return DateTime.UnixEpoch;
        }

        private static string FirstMessage<T>(ProcessingResult<T> result) =>
            result.Messages.Count > 0
                ? result.Messages[0].Message
                : "Settings operation failed";
    }
}
