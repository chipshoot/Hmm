using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Map.Migration;
using Hmm.Utility.Misc;

namespace Hmm.Core
{
    /// <summary>
    /// Bulk migration operations across all of an author's notes +
    /// vault — Free → Paid upload, Paid → Local export, and
    /// "Replace cloud." See <c>docs/multi-device-cloud-sync.md</c>
    /// §"Migration" and <c>docs/attachments-design.md</c>
    /// §"Migration alignment" for the design.
    /// </summary>
    /// <remarks>
    /// Composition seam — the manager:
    /// <list type="bullet">
    ///   <item>persists notes via the underlying note repository
    ///   (not <c>HmmNoteManager</c>) so it can batch a whole
    ///   envelope into a single unit of work.</item>
    ///   <item>delegates vault byte storage to
    ///   <c>IVaultBlobStore</c>.</item>
    ///   <item>writes a <c>MigrationLog</c> row at the end of
    ///   every operation (audit trail for support tickets).</item>
    /// </list>
    /// </remarks>
    public interface IMigrationManager
    {
        /// <summary>
        /// Persist notes + vault bytes from a local-mode device.
        /// Records that fail validation are surfaced in
        /// <see cref="MigrationUploadResult.Errors"/>; the rest
        /// still land. Writes a <c>UploadFromLocal</c>
        /// <c>MigrationLog</c> row.
        /// </summary>
        Task<ProcessingResult<MigrationUploadResult>> UploadAsync(
            int authorId,
            MigrationEnvelope envelope,
            IReadOnlyList<MigrationVaultBlob> blobs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Wipe every note + every vault file the author owns,
        /// then run <see cref="UploadAsync"/>. Writes a
        /// <c>CloudReplaced</c> <c>MigrationLog</c> row instead of
        /// <c>UploadFromLocal</c>.
        /// </summary>
        Task<ProcessingResult<MigrationUploadResult>> ReplaceAsync(
            int authorId,
            MigrationEnvelope envelope,
            IReadOnlyList<MigrationVaultBlob> blobs,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stream the author's complete record set + vault as a zip
        /// into <paramref name="zipStream"/>. Layout inside the
        /// zip: <c>records.json</c> at the root + each vault file
        /// at its full <c>attachments/note-N/...</c> relative path.
        /// Returns the server-computed counts; also writes an
        /// <c>ExportToLocal</c> <c>MigrationLog</c> row.
        /// </summary>
        Task<ProcessingResult<MigrationUploadResult>> ExportAsync(
            int authorId,
            Stream zipStream,
            string? deviceIdentifier = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Last <paramref name="take"/> log entries for the author,
        /// newest first.
        /// </summary>
        Task<ProcessingResult<IReadOnlyList<MigrationLog>>> GetLogAsync(
            int authorId,
            int take = 20,
            CancellationToken cancellationToken = default);
    }
}
