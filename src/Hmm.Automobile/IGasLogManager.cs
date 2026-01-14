using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;
using System.Threading.Tasks;

namespace Hmm.Automobile;

public interface IGasLogManager : IAutoEntityManager<GasLog>
{
    PageList<GasLog> GetGasLogs(int automobileId, ResourceCollectionParameters resourceCollectionParameters = null);

    Task<PageList<GasLog>> GetGasLogsAsync(int automobileId, ResourceCollectionParameters resourceCollectionParameters = null);

    GasLog LogHistory(GasLog entity);

    Task<GasLog> LogHistoryAsync(GasLog entity);
}