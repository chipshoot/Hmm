// Ignore Spelling: Repo

using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public class ApplicationRegister : IApplication
    {
        private static Author _appAuthor;
        private readonly IConfiguration _configuration;
        private NoteCatalog _automobileCatalog;
        private NoteCatalog _gasDiscountCatalog;
        private NoteCatalog _gasLogCatalog;

        public ApplicationRegister(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            _configuration = configuration;
        }

        public static Author DefaultAuthor
        {
            get
            {
                return _appAuthor ??= new Author
                {
                    AccountName = "03D9D3DE-0C3C-4775-BEC3-6B698B696837",
                    Description = "Automobile default author",
                    Role = AuthorRoleType.Author,
                    IsActivated = true
                };
            }
        }

        public async Task<ProcessingResult<bool>> RegisterAsync(
            IAutoEntityManager<AutomobileInfo> automobileMan,
            IAutoEntityManager<GasDiscount> discountMan,
            IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(automobileMan);
            ArgumentNullException.ThrowIfNull(discountMan);
            ArgumentNullException.ThrowIfNull(lookupRepo);

            try
            {
                var addSeedRecords = bool.Parse(_configuration["Automobile:Seeding:AddSeedingEntity"] ?? "false");
                if (!addSeedRecords)
                {
                    return ProcessingResult<bool>.Ok(true);
                }

                // Insert seeding entity when registering application
                var dataFileName = _configuration["Automobile:Seeding:SeedingDataFile"];
                if (string.IsNullOrEmpty(dataFileName))
                {
                    return ProcessingResult<bool>.Ok(true);
                }

                var entities = GetSeedingEntities(dataFileName);
                if (entities == null)
                {
                    return ProcessingResult<bool>.Ok(true);
                }

                var automobileBases = entities.ToList();
                var errors = new List<string>();

                foreach (var automobile in automobileBases.OfType<AutomobileInfo>())
                {
                    var result = await automobileMan.CreateAsync(automobile);
                    if (!result.Success)
                    {
                        errors.Add($"Failed to create automobile: {result.ErrorMessage}");
                    }
                }

                foreach (var discount in automobileBases.OfType<GasDiscount>())
                {
                    var result = await discountMan.CreateAsync(discount);
                    if (!result.Success)
                    {
                        errors.Add($"Failed to create discount: {result.ErrorMessage}");
                        break;
                    }
                }

                if (errors.Count > 0)
                {
                    return ProcessingResult<bool>.Ok(false, string.Join("; ", errors));
                }

                return ProcessingResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ProcessingResult<bool>.FromException(ex);
            }
        }

        public static IEnumerable<AutomobileBase> GetSeedingEntities(string dataFileName)
        {
            var entities = new List<AutomobileBase>();

            if (string.IsNullOrEmpty(dataFileName) || !File.Exists(dataFileName))
            {
                return entities;
            }

            var jsonText = File.ReadAllText(dataFileName);
            var root = JsonSerializer.Deserialize<SeedingEntityRoot>(jsonText);
            if (root == null)
            {
                return entities;
            }

            if (root.AutomobileInfos != null)
            {
                entities.AddRange(root.AutomobileInfos);
            }

            if (root.GasDiscounts != null)
            {
                entities.AddRange(root.GasDiscounts);
            }

            return entities;
        }

        public async Task<NoteCatalog> GetCatalogAsync(NoteCatalogType entityType, IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);

            var catalogName = entityType switch
            {
                NoteCatalogType.Automobile => AutomobileConstant.AutoMobileInfoCatalogName,
                NoteCatalogType.GasDiscount => AutomobileConstant.GasDiscountCatalogName,
                NoteCatalogType.GasLog => AutomobileConstant.GasLogCatalogName,
                _ => null
            };

            if (string.IsNullOrEmpty(catalogName))
            {
                return null;
            }

            // Check cached catalogs first
            var cachedCatalog = entityType switch
            {
                NoteCatalogType.Automobile => _automobileCatalog,
                NoteCatalogType.GasDiscount => _gasDiscountCatalog,
                NoteCatalogType.GasLog => _gasLogCatalog,
                _ => null
            };

            if (cachedCatalog != null)
            {
                return cachedCatalog;
            }

            // Fetch from repository
            var catalogsResult = await lookupRepo.GetEntitiesAsync<NoteCatalog>(c => c.Name == catalogName);
            if (!catalogsResult.Success || catalogsResult.Value == null)
            {
                return null;
            }

            var catalog = catalogsResult.Value.FirstOrDefault();
            if (catalog != null)
            {
                // Cache the result
                switch (entityType)
                {
                    case NoteCatalogType.Automobile:
                        _automobileCatalog = catalog;
                        break;
                    case NoteCatalogType.GasDiscount:
                        _gasDiscountCatalog = catalog;
                        break;
                    case NoteCatalogType.GasLog:
                        _gasLogCatalog = catalog;
                        break;
                }
            }

            return catalog;
        }

        private class SeedingEntityRoot
        {
            public IEnumerable<AutomobileInfo> AutomobileInfos { get; set; }

            public IEnumerable<GasDiscount> GasDiscounts { get; set; }
        }
    }
}
