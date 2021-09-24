using Hmm.Core.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Validation;
using System;

namespace Hmm.Automobile.Validator
{
    public class EntityValidatorBase<T> : ValidatorBase<T>
    {
        private readonly IEntityLookup _lookupRepo;

        protected EntityValidatorBase(IEntityLookup lookupRepo)
        {
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));
            _lookupRepo = lookupRepo;
        }

        protected bool HasValidAuthor(Guid authorId)
        {
            if (authorId == Guid.Empty)
            {
                return false;
            }

            var savedAuthor = _lookupRepo.GetEntity<Author>(authorId);
            return savedAuthor != null;
        }

        protected static bool HasValidDistance(Dimension distance)
        {
            return distance.Value > 0;
        }

        protected static bool HasValidVolume(Volume volume)
        {
            return volume.Value > 0;
        }

        protected static bool HasValidMoney(Money price)
        {
            return price.Amount >= 0m;
        }
    }
}