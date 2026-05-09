using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public interface IAutoScheduledServiceManager : IAutoEntityManager<AutoScheduledService>
    {
        /// <summary>
        /// Lists scheduled-service entries for a specific automobile.
        /// </summary>
        Task<ProcessingResult<PageList<AutoScheduledService>>> GetByAutomobileAsync(
            int automobileId,
            ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Returns the soonest-due active schedule for a vehicle, or null if none.
        /// </summary>
        Task<ProcessingResult<AutoScheduledService>> GetSoonestDueForAutomobileAsync(int automobileId);
    }
}
