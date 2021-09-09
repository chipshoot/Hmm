using Hmm.Utility.Dal.Query;

namespace Hmm.Core.DefaultManager.Validation
{
    public class ExistsUserValidator : ExistsElementValidator<User>
    {
        public ExistsUserValidator(IEntityLookup lookupRepo) : base(lookupRepo)
        {
            Include(new UserValidator2());
        }
    }
}