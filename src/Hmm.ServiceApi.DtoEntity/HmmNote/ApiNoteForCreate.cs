using System;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiNoteForCreate : ApiEntity
    {
        public string Subject { get; set; }

        public string Content { get; set; }

        public Guid AuthorId { get; set; }

        public int NoteCatalogId { get; set; }

        public string Description { get; set; }
    }
}