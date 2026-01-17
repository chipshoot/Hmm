using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public interface IApplication
    {
        Task<ProcessingResult<bool>> RegisterAsync(
            IAutoEntityManager<AutomobileInfo> automobileMan,
            IAutoEntityManager<GasDiscount> discountMan,
            IEntityLookup lookupRepo);

        Task<NoteCatalog> GetCatalogAsync(NoteCatalogType entityType, IEntityLookup lookupRepo);

        NoteCatalog GetCatalog(NoteCatalogType entityType, IEntityLookup lookupRepo);
    }
}
