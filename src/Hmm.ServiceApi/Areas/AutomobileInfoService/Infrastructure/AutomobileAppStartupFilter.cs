using Hmm.Automobile;
using Hmm.Core;
using Hmm.Core.Dal.EF;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure
{
    public class AutomobileAppStartupFilter : IStartupFilter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutomobileAppStartupFilter> _logger;

        public AutomobileAppStartupFilter(IServiceProvider serviceProvider, ILogger<AutomobileAppStartupFilter> logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            using var scope = _serviceProvider.CreateScope();

            // SQLite: ensure database tables exist before seeding
            EnsureDatabaseCreated(scope.ServiceProvider);

            // Ensure required NoteCatalogs exist before registration
            EnsureNoteCatalogsExist(scope.ServiceProvider);

            var app = scope.ServiceProvider.GetService<IApplication>();
            var lookupRepo = scope.ServiceProvider.GetService<IEntityLookup>();

            if (app != null && lookupRepo != null)
            {
                var result = app.RegisterAsync(lookupRepo).GetAwaiter().GetResult();
                if (!result.Success)
                {
                    _logger?.LogWarning("Automobile application registration failed: {Error}", result.ErrorMessage);
                }
            }

            return next;
        }

        private void EnsureDatabaseCreated(IServiceProvider serviceProvider)
        {
            try
            {
                var dbContext = serviceProvider.GetService<HmmDataContext>();
                if (dbContext?.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
                {
                    dbContext.Database.EnsureCreated();
                    dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
                    _logger?.LogInformation("SQLite database created/verified with WAL mode");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error ensuring database is created");
            }
        }

        private void EnsureNoteCatalogsExist(IServiceProvider serviceProvider)
        {
            var catalogManager = serviceProvider.GetService<INoteCatalogManager>();
            if (catalogManager == null)
            {
                _logger?.LogWarning("INoteCatalogManager not available, skipping catalog seeding");
                return;
            }

            var requiredCatalogs = new[]
            {
                AutomobileConstant.AutoMobileInfoCatalogName,
                AutomobileConstant.GasDiscountCatalogName,
                AutomobileConstant.GasLogCatalogName,
                AutomobileConstant.GasStationCatalogName
            };

            foreach (var catalogName in requiredCatalogs)
            {
                try
                {
                    var existingResult = catalogManager.GetEntitiesAsync(c => c.Name == catalogName).GetAwaiter().GetResult();
                    if (existingResult.Success && existingResult.Value != null && existingResult.Value.Any())
                    {
                        continue;
                    }

                    _logger?.LogInformation("Creating missing NoteCatalog: {CatalogName}", catalogName);
                    var catalog = new NoteCatalog
                    {
                        Name = catalogName,
                        Type = NoteContentFormatType.Json,
                        Schema = string.Empty,
                        IsDefault = false
                    };

                    var createResult = catalogManager.CreateAsync(catalog).GetAwaiter().GetResult();
                    if (!createResult.Success)
                    {
                        _logger?.LogWarning("Failed to create NoteCatalog {CatalogName}: {Error}", catalogName, createResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error ensuring NoteCatalog {CatalogName} exists", catalogName);
                }
            }
        }
    }
}
