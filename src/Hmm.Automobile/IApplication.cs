using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Defines the contract for automobile module registration and catalog management.
    /// </summary>
    public interface IApplication
    {
        /// <summary>
        /// Registers the automobile module and optionally seeds initial data.
        /// </summary>
        /// <param name="lookupRepo">Repository for entity lookups.</param>
        /// <returns>A ProcessingResult indicating success or failure of the registration.</returns>
        Task<ProcessingResult<bool>> RegisterAsync(IEntityLookup lookupRepo);

        /// <summary>
        /// Asynchronously retrieves a NoteCatalog for the specified entity type.
        /// </summary>
        /// <param name="entityType">The type of entity for which to retrieve the catalog.</param>
        /// <param name="lookupRepo">Repository for entity lookups.</param>
        /// <returns>The NoteCatalog for the specified entity type, or null if not found.</returns>
        Task<NoteCatalog> GetCatalogAsync(NoteCatalogType entityType, IEntityLookup lookupRepo);
    }
}
