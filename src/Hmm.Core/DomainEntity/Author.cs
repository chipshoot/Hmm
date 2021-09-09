using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Core.DomainEntity
{
    public class Author : GuidEntity
    {
        public string AccountName { get; set; }

        public AuthorRoleType Role { get; set; }

        public bool IsActivated { get; set; }
    }
}