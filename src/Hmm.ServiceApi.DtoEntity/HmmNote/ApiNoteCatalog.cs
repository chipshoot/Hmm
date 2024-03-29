﻿namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiNoteCatalog : ApiEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int SubsystemId { get; set; }

        public int RenderId { get; set; }

        public string Schema { get; set; }

        public bool IsDefault { get; set; }

        public string Description { get; set; }
    }
}