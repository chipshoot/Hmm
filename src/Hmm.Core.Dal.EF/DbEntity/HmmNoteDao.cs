// Ignore Spelling: Dao

using Hmm.Utility.Dal.DataEntity;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hmm.Core.Dal.EF.DbEntity
{
    public class HmmNoteDao : VersionedEntity
    {
        [Column("subject")]
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
    }
}