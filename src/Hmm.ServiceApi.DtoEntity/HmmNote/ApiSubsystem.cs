using System;
using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiSubsystem : ApiEntity
    {
        public int Id { get; set; }

        public Guid DefaultAuthorId { get; set; }

        public string Name { get; set; }

        public IEnumerable<int> NoteCatalogIds { get; set; }

        public string Description { get; set; }
    }
}