using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Provides access to NoteCatalog instances for automobile entity types.
    /// Uses thread-safe caching to minimize database queries.
    ///
    /// This class is separated from ApplicationRegister to avoid circular dependencies:
    /// - Serializers depend on INoteCatalogProvider (this class)
    /// - ApplicationRegister depends on ISeedingService which depends on managers
    /// - Managers depend on serializers
    ///
    /// By separating catalog lookup from application registration, we break the cycle.
    /// </summary>
    public class NoteCatalogProvider : INoteCatalogProvider
    {
        private readonly IEntityLookup _lookupRepo;
        private readonly ILogger<NoteCatalogProvider> _logger;
        private readonly ConcurrentDictionary<NoteCatalogType, NoteCatalog> _catalogCache = new();

        /// <summary>
        /// Initializes a new instance of the NoteCatalogProvider class.
        /// </summary>
        /// <param name="lookupRepo">Repository for entity lookups.</param>
        /// <param name="logger">Logger instance for diagnostics.</param>
        public NoteCatalogProvider(
            IEntityLookup lookupRepo,
            ILogger<NoteCatalogProvider> logger = null)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);

            _lookupRepo = lookupRepo;
            _logger = logger;
        }

        /// <summary>
        /// Asynchronously retrieves a NoteCatalog for the specified entity type.
        /// Catalogs are cached after first retrieval to minimize database queries.
        /// </summary>
        /// <param name="entityType">The type of entity for which to retrieve the catalog.</param>
        /// <returns>The NoteCatalog for the specified entity type, or null if not found.</returns>
        public async Task<NoteCatalog> GetCatalogAsync(NoteCatalogType entityType)
        {
            var catalogName = GetCatalogName(entityType);
            if (string.IsNullOrEmpty(catalogName))
            {
                _logger?.LogWarning("Unknown catalog type requested: {EntityType}", entityType);
                return null;
            }

            // Try to get from cache first (thread-safe)
            if (_catalogCache.TryGetValue(entityType, out var cachedCatalog))
            {
                return cachedCatalog;
            }

            // Fetch from repository
            _logger?.LogDebug("Fetching catalog from database: {CatalogName}", catalogName);
            var catalogsResult = await _lookupRepo.GetEntitiesAsync<NoteCatalog>(c => c.Name == catalogName);
            if (!catalogsResult.Success || catalogsResult.Value == null)
            {
                _logger?.LogWarning("Failed to retrieve catalog: {CatalogName}", catalogName);
                return null;
            }

            var catalog = catalogsResult.Value.FirstOrDefault();
            if (catalog != null)
            {
                // Thread-safe cache update
                _catalogCache.TryAdd(entityType, catalog);
                _logger?.LogDebug("Catalog cached: {CatalogName}", catalogName);
            }

            return catalog;
        }

        /// <summary>
        /// Gets the catalog name for the specified entity type.
        /// </summary>
        private static string GetCatalogName(NoteCatalogType entityType)
        {
            return entityType switch
            {
                NoteCatalogType.Automobile => AutomobileConstant.AutoMobileInfoCatalogName,
                NoteCatalogType.GasDiscount => AutomobileConstant.GasDiscountCatalogName,
                NoteCatalogType.GasLog => AutomobileConstant.GasLogCatalogName,
                NoteCatalogType.GasStation => AutomobileConstant.GasStationCatalogName,
                _ => null
            };
        }
    }
}
