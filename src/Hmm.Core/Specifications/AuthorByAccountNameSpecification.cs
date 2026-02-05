using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Specification;

namespace Hmm.Core.Specifications
{
    /// <summary>
    /// Specification that matches an author by their account name.
    /// </summary>
    public class AuthorByAccountNameSpecification : Specification<Author>
    {
        public AuthorByAccountNameSpecification(string accountName)
            : base(a => a.AccountName == accountName)
        {
        }
    }
}
