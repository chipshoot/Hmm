using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;

namespace Hmm.Automobile
{
    public interface IApplication
    {
        bool Register(IAutoEntityManager<AutomobileInfo> automobileMan,
            IAutoEntityManager<GasDiscount> discountMan,
            IEntityLookup lookupRepo);

        NoteCatalog GetCatalog(NoteCatalogType entityType, IEntityLookup lookupRepo);

        ProcessingResult ProcessingResult { get; }
    }
}