using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hmm.ServiceApi.DtoEntity.HmmNote
{
    public class ApiContactForUpdate : ApiEntity
    {
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; }

        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; }

        public IEnumerable<ApiEmail> Emails { get; set; }

        public IEnumerable<ApiPhone> Phones { get; set; }

        public IEnumerable<ApiAddressInfo> Addresses { get; set; }

        public bool IsActivated { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }
    }
}