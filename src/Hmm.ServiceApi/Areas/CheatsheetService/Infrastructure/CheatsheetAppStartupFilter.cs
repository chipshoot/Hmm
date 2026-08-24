using System;
using System.Linq;
using Hmm.Cheatsheet;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure
{
    /// <summary>
    /// Creates the cheatsheet NoteCatalog row if it is missing.
    ///
    /// This is DATA, not schema: cheatsheets ride the existing Notes and
    /// NoteCatalogs tables, so the feature needs no EF migration. Schema
    /// creation itself is still owned by AutomobileAppStartupFilter, which runs
    /// EnsureCreated()/Migrate() per provider.
    ///
    /// The catalog schema is "{}" - an empty JSON schema that validates
    /// everything. A restrictive schema here would reject exactly the
    /// forward-compatible card content the serializer works to preserve.
    /// </summary>
    public class CheatsheetAppStartupFilter : IStartupFilter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CheatsheetAppStartupFilter> _logger;

        public CheatsheetAppStartupFilter(
            IServiceProvider serviceProvider,
            ILogger<CheatsheetAppStartupFilter> logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            using var scope = _serviceProvider.CreateScope();
            EnsureCatalogExists(scope.ServiceProvider);
            return next;
        }

        private void EnsureCatalogExists(IServiceProvider serviceProvider)
        {
            var catalogManager = serviceProvider.GetService<INoteCatalogManager>();
            if (catalogManager == null)
            {
                _logger?.LogWarning("INoteCatalogManager not available, skipping cheatsheet catalog seeding");
                return;
            }

            try
            {
                var existingResult = catalogManager
                    .GetEntitiesAsync(c => c.Name == CheatsheetConstant.CheatsheetCatalogName)
                    .GetAwaiter()
                    .GetResult();

                // A failed lookup is not the same as "no catalog yet". Treating
                // them alike meant a DB that was not ready at boot fell straight
                // through to CreateAsync, and the real error was discarded.
                if (!existingResult.Success)
                {
                    _logger?.LogError(
                        "Cannot determine whether NoteCatalog {CatalogName} exists, skipping seeding: {Error}",
                        CheatsheetConstant.CheatsheetCatalogName,
                        existingResult.ErrorMessage);
                    return;
                }

                if (existingResult.Value != null && existingResult.Value.Any())
                {
                    return;
                }

                _logger?.LogInformation(
                    "Creating missing NoteCatalog: {CatalogName}",
                    CheatsheetConstant.CheatsheetCatalogName);

                var catalog = new NoteCatalog
                {
                    Name = CheatsheetConstant.CheatsheetCatalogName,
                    Type = NoteContentFormatType.Json,
                    Schema = "{}",
                    IsDefault = false
                };

                var createResult = catalogManager.CreateAsync(catalog).GetAwaiter().GetResult();
                if (!createResult.Success)
                {
                    _logger?.LogWarning(
                        "Failed to create NoteCatalog {CatalogName}: {Error}",
                        CheatsheetConstant.CheatsheetCatalogName,
                        createResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error ensuring NoteCatalog {CatalogName} exists",
                    CheatsheetConstant.CheatsheetCatalogName);
            }
        }
    }
}
