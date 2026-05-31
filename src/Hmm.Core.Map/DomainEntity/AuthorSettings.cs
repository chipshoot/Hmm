using System;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DomainEntity
{
    /// <summary>
    /// Per-author user-preferences blob for the cloudApi tier. One row
    /// per <see cref="Author"/>. See
    /// <c>docs/user-profile-settings-sync.md</c>.
    /// </summary>
    /// <remarks>
    /// The server treats <see cref="SettingsJson"/> as OPAQUE — it's
    /// the client's <c>SyncableSettings</c> bundle, stored verbatim.
    /// The only field the server interprets is
    /// <see cref="LastModified"/> (lifted from the bundle envelope by
    /// the controller), used as the last-writer-wins / monotonicity
    /// stamp. That keeps the contract stable: a client can add a new
    /// preference (bumping the bundle's <c>_v</c>) with zero server
    /// migrations.
    /// </remarks>
    public class AuthorSettings : Entity
    {
        public int AuthorId { get; set; }

        /// <summary>
        /// The full settings bundle JSON, opaque to the server.
        /// </summary>
        public string SettingsJson { get; set; }

        /// <summary>
        /// UTC timestamp lifted from the bundle envelope; the LWW
        /// stamp. A PUT whose <see cref="LastModified"/> is not newer
        /// than the stored row is a no-op (the stored bundle wins).
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Server clock at the last successful write — audit only.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
