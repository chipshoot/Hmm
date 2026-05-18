using Hmm.Core.Vault;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DomainEntity
{
    public class HmmNote : VersionedEntity, IAuditable
    {
        public string Subject { get; set; }

        public string Content { get; set; }

        public NoteCatalog Catalog { get; set; }

        public Author Author { get; set; }

        public bool IsDeleted { get; set; } = false;

        public List<Tag> Tags { get; set; }=new List<Tag>();

        public DateTime CreateDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

        public string? CreatedBy { get; set; }

        public string? LastModifiedBy { get; set; }

        /// <summary>
        /// Headline image, or <c>null</c> when none is set. Read /
        /// written through the <c>Notes.attachments</c> JSON column
        /// via <c>NoteAttachmentsCodec</c>; AutoMapper handles the
        /// projection (see <c>HmmMappingProfile</c>).
        /// </summary>
        public VaultRef? PrimaryImage { get; set; }

        /// <summary>
        /// Gallery — zero or more additional refs. Disjoint with
        /// <see cref="PrimaryImage"/>.
        /// </summary>
        public IList<VaultRef> Images { get; set; } = new List<VaultRef>();
    }
}