using Hmm.Core.Map.DomainEntity;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Provides access to NoteCatalog instances for different automobile entity types.
    /// This interface is separated from IApplication to avoid circular dependencies
    /// in the DI container.
    /// </summary>
    public interface INoteCatalogProvider
    {
        /// <summary>
        /// Asynchronously retrieves a NoteCatalog for the specified entity type.
        /// </summary>
        /// <param name="entityType">The type of entity for which to retrieve the catalog.</param>
        /// <returns>The NoteCatalog for the specified entity type, or null if not found.</returns>
        Task<NoteCatalog> GetCatalogAsync(NoteCatalogType entityType);
    }
}
