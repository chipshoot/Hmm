using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile;

public interface IGasLogManager : IAutoEntityManager<GasLog>
{
    Task<ProcessingResult<PageList<GasLog>>> GetGasLogsAsync(int automobileId, ResourceCollectionParameters resourceCollectionParameters = null);

    Task<ProcessingResult<GasLog>> LogHistoryAsync(GasLog entity);
}
