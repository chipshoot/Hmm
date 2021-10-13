using System.Collections.Generic;
using Hmm.Core.DomainEntity;

namespace Hmm.Core
{
    public interface INoteCatalogManager : IEntityManager<NoteCatalog>
    {
        bool BatchCreate(IEnumerable<NoteCatalog> catalogs);
    }
}