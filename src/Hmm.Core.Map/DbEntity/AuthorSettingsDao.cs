// Ignore Spelling: Dao

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DbEntity
{
    /// <summary>
    /// One settings row per <see cref="AuthorDao"/> (enforced by a
    /// unique index on <c>authorid</c>). Mirrors the planned
    /// <c>Subscription</c> shape — a thin satellite table rather than
    /// columns bolted onto the hot <c>authors</c> row read on every
    /// note operation. Shape per
    /// <c>docs/user-profile-settings-sync.md</c>.
    /// </summary>
    /// <remarks>
    /// No <c>Version</c>/<c>ts</c> concurrency token: conflict
    /// resolution is the <see cref="LastModified"/> monotonicity guard
    /// in the manager (a stale write is a no-op), not row-version 409s.
    /// </remarks>
    public class AuthorSettingsDao : Entity
    {
        [Column("authorid")]
        public int AuthorId { get; set; }

        /// <summary>
        /// The client's settings bundle JSON, stored verbatim
        /// (opaque to the server).
        /// </summary>
        [Column("settingsjson")]
        public string SettingsJson { get; set; }

        [Column("lastmodified")]
        public DateTime LastModified { get; set; }

        [Column("updatedat")]
        public DateTime UpdatedAt { get; set; }
    }
}
