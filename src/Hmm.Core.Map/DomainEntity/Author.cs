using System.ComponentModel.DataAnnotations;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.Map.DomainEntity
{
    public class Author : Entity
    {
        [StringLength(256)]
        public string AccountName { get; set; }

        public Contact ContactInfo { get; set; }

        public AuthorRoleType Role { get; set; }

        public bool IsActivated { get; set; }
    }
}