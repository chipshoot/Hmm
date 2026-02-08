using System;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    /// <summary>
    /// Represents a note in API responses.
    /// </summary>
    public class ApiNote : ApiEntity
    {
        public int Id { get; set; }

        public string Subject { get; set; }

        public string Content { get; set; }

        public int AuthorId { get; set; }

        public int CatalogId { get; set; }

        public string Description { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime LastModifiedDate { get; set; }

        public string CreatedBy { get; set; }

        public string LastModifiedBy { get; set; }
    }
}