using System;
using System.Linq;
using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Microsoft.Extensions.Logging;

namespace Hmm.Cheatsheet
{
    /// <summary>
    /// Caches the cheatsheet NoteCatalog after the first successful lookup so
    /// every serialize/deserialize does not re-query the database.
    /// </summary>
    public class CheatsheetCatalogProvider : ICheatsheetCatalogProvider
    {
        private readonly IEntityLookup _lookupRepo;
        private readonly ILogger<CheatsheetCatalogProvider> _logger;
        private NoteCatalog _cachedCatalog;

        public CheatsheetCatalogProvider(
            IEntityLookup lookupRepo,
            ILogger<CheatsheetCatalogProvider> logger = null)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);

            _lookupRepo = lookupRepo;
            _logger = logger;
        }

        public async Task<NoteCatalog> GetCatalogAsync()
        {
            if (_cachedCatalog != null)
            {
                return _cachedCatalog;
            }

            var catalogsResult = await _lookupRepo.GetEntitiesAsync<NoteCatalog>(
                c => c.Name == CheatsheetConstant.CheatsheetCatalogName);

            if (!catalogsResult.Success || catalogsResult.Value == null)
            {
                _logger?.LogWarning(
                    "Failed to retrieve catalog {CatalogName}: {Error}",
                    CheatsheetConstant.CheatsheetCatalogName,
                    catalogsResult.ErrorMessage);
                return null;
            }

            var catalog = catalogsResult.Value.FirstOrDefault();
            if (catalog == null)
            {
                _logger?.LogWarning(
                    "Catalog {CatalogName} does not exist yet",
                    CheatsheetConstant.CheatsheetCatalogName);
                return null;
            }

            _cachedCatalog = catalog;
            return catalog;
        }
    }
}
