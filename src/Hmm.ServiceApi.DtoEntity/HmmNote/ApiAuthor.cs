using System;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiAuthor : ApiEntity
    {
        public Guid Id { get; set; }

        public string AccountName { get; set; }

        public string Role { get; set; }

        public bool IsActivated { get; set; }

        public string Description { get; set; }
    }
}