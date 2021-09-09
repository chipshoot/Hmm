using Hmm.Utility.Dal.DataEntity;
using System;

namespace Hmm.Core.DomainEntity
{
    public class HmmNote : VersionedEntity
    {
        public string Subject { get; set; }

        public string Content { get; set; }

        public NoteCatalog Catalog { get; set; }

        public Author Author { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreateDate { get; set; }

        public DateTime LastModifiedDate { get; set; }
    }
}