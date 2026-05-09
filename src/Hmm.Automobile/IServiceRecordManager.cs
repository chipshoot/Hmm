using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public interface IServiceRecordManager : IAutoEntityManager<ServiceRecord>
    {
        /// <summary>
        /// Lists service-history records for a specific automobile.
        /// </summary>
        Task<ProcessingResult<PageList<ServiceRecord>>> GetByAutomobileAsync(
            int automobileId,
            ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Returns the most-recent service record for a vehicle, or null if none.
        /// </summary>
        Task<ProcessingResult<ServiceRecord>> GetMostRecentForAutomobileAsync(int automobileId);
    }
}
