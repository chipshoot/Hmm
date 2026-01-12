// Ignore Spelling: Repo

using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;

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

        public ProcessingResult ProcessingResult { get; } = new();

        public bool Register(IAutoEntityManager<AutomobileInfo> automobileMan,
            IAutoEntityManager<GasDiscount> discountMan,
            IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(automobileMan);
            ArgumentNullException.ThrowIfNull(discountMan);
            ArgumentNullException.ThrowIfNull(lookupRepo);

            string application = null;// GetApplication(lookupRepo);
            var success = false;
            if (application == null)
            {
                ProcessingResult.AddErrorMessage("Cannot get application for automobile");
                return false;
            }

            try
            {
                var addSeedRecords = bool.Parse(_configuration["Automobile:Seeding:AddSeedingEntity"] ?? "false");
                if (!addSeedRecords)
                {
                    return true;
                }

                // Insert seeding entity when registering application
                var dataFileName = _configuration["Automobile:Seeding:SeedingDataFile"];
                if (string.IsNullOrEmpty(dataFileName))
                {
                    return true;
                }
                var entities = GetSeedingEntities(dataFileName);
                if (entities != null)
                {
                    var automobileBases = entities.ToList();
                    foreach (var automobile in automobileBases.OfType<AutomobileInfo>())
                    {
                        var newCar = automobileMan.Create(automobile);
                        if (newCar == null)
                        {
                            ProcessingResult.PropagandaResult(automobileMan.ProcessResult);
                        }
                    }
                    foreach (var discount in automobileBases.OfType<GasDiscount>())
                    {
                        var newDiscount = discountMan.Create(discount);
                        if (newDiscount == null)
                        {
                            ProcessingResult.PropagandaResult(discountMan.ProcessResult);
                            success = false;
                            break;
                        }
                    }
                }

                // ReSharper restore PossibleNullReferenceException
                return success;
            }
            catch (Exception e)
            {
                ProcessingResult.WrapException(e);
                return false;
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

            entities.AddRange(root.AutomobileInfos);
            entities.AddRange(root.GasDiscounts);
            return entities;
        }

        public NoteCatalog GetCatalog(NoteCatalogType entityType, IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);

            // ReSharper disable PossibleNullReferenceException
            NoteCatalog catalog;
            switch (entityType)
            {
                case NoteCatalogType.Automobile:
                    catalog = _automobileCatalog ??= lookupRepo.GetEntities<NoteCatalog>()
                        .FirstOrDefault(c => c.Name == AutomobileConstant.AutoMobileInfoCatalogName);
                    break;

                case NoteCatalogType.GasDiscount:
                    catalog = _gasDiscountCatalog ??= lookupRepo.GetEntities<NoteCatalog>()
                        .FirstOrDefault(c => c.Name == AutomobileConstant.GasDiscountCatalogName);
                    break;

                case NoteCatalogType.GasLog:
                    catalog = _gasLogCatalog ??= _automobileCatalog = lookupRepo.GetEntities<NoteCatalog>()
                        .FirstOrDefault(c => c.Name == AutomobileConstant.GasLogCatalogName);
                    break;

                default:
                    ProcessingResult.AddErrorMessage($"CatalogType {entityType} is not supported");
                    catalog = null;
                    break;
            }

            if (catalog == null && !lookupRepo.ProcessResult.Success)
            {
                ProcessingResult.PropagandaResult(lookupRepo.ProcessResult);
            }

            // ReSharper restore PossibleNullReferenceException
            return catalog;
        }

        private string GetSchema(NoteCatalogType catalog)
        {
            var schemaFile = catalog switch
            {
                NoteCatalogType.Automobile => _configuration["Automobile:Schema:Automobile"],
                NoteCatalogType.GasDiscount => _configuration["Automobile:Schema:GasDiscount"],
                NoteCatalogType.GasLog => _configuration["Automobile:Schema:GasLog"],
                _ => ""
            };

            if (string.IsNullOrEmpty(schemaFile))
            {
                return string.Empty;
            }

            if (!File.Exists(schemaFile))
            {
                return "";
            }

            var xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(schemaFile);
                var sw = new StringWriter();
                var xw = new XmlTextWriter(sw);
                xmlDoc.WriteTo(xw);
                return sw.ToString();
            }
            catch (Exception e)
            {
                ProcessingResult.WrapException(e);
                return "";
            }
        }

        private class SeedingEntityRoot
        {
            public IEnumerable<AutomobileInfo> AutomobileInfos { get; set; }

            public IEnumerable<GasDiscount> GasDiscounts { get; set; }
        }
    }
}