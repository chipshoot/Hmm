using System;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiNote : ApiEntity
    {
        public string Subject { get; set; }

        public string Content { get; set; }

        public ApiAuthor Author { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime LastModifiedDate { get; set; }
    }
}