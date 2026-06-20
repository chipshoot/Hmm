// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DbEntity
{
    public class HmmNoteDao : VersionedEntity, IAuditable
    {
        [Column("subject")]
        [MaxLength(1000)]
        public string Subject { get; set; }

        [Column("content")]
        public string Content { get; set; }

        [ForeignKey("catalogid")]
        public NoteCatalogDao Catalog { get; set; }

        [ForeignKey("authorid")]
        public AuthorDao Author { get; set; }

        [Column("isdeleted")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Cross-device-stable identity for the note. Independent
        /// of <see cref="AbstractEntity{TIdentity}.Id"/>, which
        /// stays an internal FK target (cheap 4-byte joins on
        /// NoteTagRef / MigrationLog). The Uuid is the wire-level
        /// identity — clients pick it at create time so a note can
        /// exist on a device before any server sees it, and the
        /// same value follows the note across devices.
        /// </summary>
        /// <remarks>
        /// Nullable + unique. Existing rows pre-dating the Phase
        /// 15b migration stay null until their next manager call
        /// (Create/Update auto-assign). The unique index uses
        /// PG/SQLite's "multiple nulls allowed" semantics so the
        /// null rows don't block each other.
        /// </remarks>
        [Column("uuid")]
        [StringLength(36)]
        public string? Uuid { get; set; }

        [Column("createdate")]
        public DateTime CreateDate { get; set; }

        [Column("notedate")]
        public DateTime NoteDate { get; set; }

        [Column("latitude")]
        public double? Latitude { get; set; }

        [Column("longitude")]
        public double? Longitude { get; set; }

        [Column("locationlabel")]
        [MaxLength(500)]
        public string? LocationLabel { get; set; }

        [Column("lastmodifieddate")]
        public DateTime LastModifiedDate { get; set; }

        [Column("createdby")]
        [MaxLength(256)]
        public string? CreatedBy { get; set; }

        [Column("lastmodifiedby")]
        [MaxLength(256)]
        public string? LastModifiedBy { get; set; }

        /// <summary>
        /// Per-note attachments — JSON value matching
        /// <c>Hmm.Core.Vault/Schemas/NoteAttachments.schema.json</c>.
        /// NULL = no attachments. AutoMapper projects this column
        /// into <c>HmmNote.PrimaryImage</c> + <c>HmmNote.Images</c>
        /// via <c>NoteAttachmentsCodec</c>; the schema validation
        /// happens at the manager layer before persistence.
        /// </summary>
        [Column("attachments")]
        public string? Attachments { get; set; }

        public IList<NoteTagRefDao> Tags { get; set; } = new List<NoteTagRefDao>();
    }
}