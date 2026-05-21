// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DbEntity
{
    /// <summary>
    /// Append-only audit row for bulk migration operations
    /// (Free → Paid upload, Paid → Local export, Replace, lapsed
    /// delete). Shape per <c>docs/multi-device-cloud-sync.md</c>
    /// §"MigrationLog".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>DeviceId</c> is a free-form string ("DeviceIdentifier" on
    /// the wire) until the <c>Devices</c> entity lands as part of
    /// the cloud-sync project. When that arrives we'll widen this
    /// column into a real FK and backfill from the existing rows.
    /// </para>
    /// <para>
    /// <c>RecordCounts</c> is the JSON shape from the design doc —
    /// <c>{ "notes": 8, "vaultFiles": 8, "vaultBytes": 14680064,
    /// "resolvedPhAssets": 5, "resolvedCloudFiles": 2,
    /// "unresolvedRefs": 1 }</c> — stored verbatim. The server
    /// computes the vault-side counts and merges in whatever extra
    /// per-domain counters the client provided.
    /// </para>
    /// </remarks>
    public class MigrationLogDao : Entity
    {
        [Column("authorid")]
        public int AuthorId { get; set; }

        [Column("device")]
        [StringLength(80)]
        public string? DeviceIdentifier { get; set; }

        [Column("kind")]
        public MigrationLogKind Kind { get; set; }

        /// <summary>
        /// Free-form JSON. May be null if a caller didn't bother
        /// to populate counts (the export endpoint always fills it
        /// in, but a future caller might not).
        /// </summary>
        [Column("recordcounts")]
        public string? RecordCounts { get; set; }

        [Column("at")]
        public System.DateTime At { get; set; }
    }
}
