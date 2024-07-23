// Ignore Spelling: Dao

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DbEntity
{
    public class HmmNoteDao : VersionedEntity
    {
        public HmmNoteDao()
        {
            Tags = new List<NoteTagRefDao>().AsQueryable();
        }

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

        public IQueryable<NoteTagRefDao> Tags { get; init; }
    }
}