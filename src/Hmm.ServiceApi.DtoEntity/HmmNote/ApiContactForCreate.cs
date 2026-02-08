using System.Collections.Generic;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    /// <summary>
    /// Data required to create a new contact.
    /// </summary>
    public class ApiContactForCreate : ApiEntity
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