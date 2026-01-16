using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Validation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hmm.Automobile.Validator
{
    public class EntityValidatorBase<T> : ValidatorBase<T>
    {
        private readonly IEntityLookup _lookupRepo;

        protected EntityValidatorBase(IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);
            _lookupRepo = lookupRepo;
        }

        /// <summary>
        /// Validates that the author exists in the system (with cancellation token support).
        /// </summary>
        protected async Task<bool> HasValidAuthor(int authorId, CancellationToken cancellationToken)
        {
            if (authorId <= 0)
            {
                return false;
            }

            // If your lookup repo supports cancellation, pass it through
            var savedAuthorResult = await _lookupRepo.GetEntityAsync<Author>(authorId);
            return savedAuthorResult.Success && savedAuthorResult.Value != null;
        }

        /// <summary>
        /// Validates that distance has a positive value.
        /// </summary>
        protected static bool HasValidDistance(Dimension distance)
        {
            return distance.Value > 0;
        }

        /// <summary>
        /// Validates that volume has a positive value.
        /// </summary>
        protected static bool HasValidVolume(Volume volume)
        {
            return volume.Value > 0;
        }

        /// <summary>
        /// Validates that money amount is non-negative.
        /// </summary>
        protected static bool HasValidMoney(Money price)
        {
            return price?.Amount >= 0m;
        }
    }
}