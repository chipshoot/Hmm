using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiSubsystemForUpdate : ApiEntity
    {
        public string Name { get; set; }

        public bool IsDefault { get; set; }

        public string Description { get; set; }
    }
}