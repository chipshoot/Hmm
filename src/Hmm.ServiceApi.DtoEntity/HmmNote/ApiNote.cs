using System;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiNote : ApiEntity
    {
        public int Id { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }

        public Guid AuthorId { get; set; }

        public int CatalogId { get; set; }

        public string Description { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime LastModifiedDate { get; set; }
    }
}