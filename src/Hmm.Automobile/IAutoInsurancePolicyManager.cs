using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public interface IAutoInsurancePolicyManager : IAutoEntityManager<AutoInsurancePolicy>
    {
        /// <summary>
        /// Lists insurance policies attached to a specific automobile.
        /// </summary>
        Task<ProcessingResult<PageList<AutoInsurancePolicy>>> GetByAutomobileAsync(
            int automobileId,
            ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Returns the currently-active policy for a vehicle, or null if none.
        /// "Active" = IsActive AND EffectiveDate &lt;= now &lt; ExpiryDate, newest EffectiveDate wins.
        /// </summary>
        Task<ProcessingResult<AutoInsurancePolicy>> GetActiveForAutomobileAsync(int automobileId);
    }
}
