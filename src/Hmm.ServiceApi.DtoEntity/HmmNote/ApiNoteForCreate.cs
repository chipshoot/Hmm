// Ignore Spelling: Dto

using System;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiNoteForCreate : ApiEntity
    {
        public string Subject { get; set; }

        public string Content { get; set; }

        public int AuthorId { get; set; }

        public int CatalogId { get; set; }

        public string Description { get; set; }
    }
}