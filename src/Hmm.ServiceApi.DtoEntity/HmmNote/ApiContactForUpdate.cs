using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiContactForUpdate : ApiEntity
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public IEnumerable<ApiEmail> Emails { get; set; }

        public IEnumerable<ApiPhone> Phones { get; set; }

        public IEnumerable<ApiAddressInfo> Addresses { get; set; }

        public bool IsActivated { get; set; }

        public string Description { get; set; }
    }
}