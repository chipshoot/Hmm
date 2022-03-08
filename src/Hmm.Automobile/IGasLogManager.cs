using Hmm.Automobile.DomainEntity;
using System.Threading.Tasks;

namespace Hmm.Automobile;

public interface IGasLogManager : IAutoEntityManager<GasLog>
{
    GasLog LogHistory(GasLog entity);

    Task<GasLog> LogHistoryAsync(GasLog entity);
}