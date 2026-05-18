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

        [Column("createdate")]
        public DateTime CreateDate { get; set; }

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