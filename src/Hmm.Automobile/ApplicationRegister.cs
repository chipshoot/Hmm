using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Hmm.Automobile
{
    public class ApplicationRegister : IApplication
    {
        private readonly ISubsystemManager _subsystemMan;
        private readonly IEntityLookup _lookupRepo;
        private readonly IConfiguration _configuration;
        private NoteCatalog _automobileCatalog;
        private NoteCatalog _gasDiscountCatalog;
        private NoteCatalog _gasLogCatalog;

        public ApplicationRegister(ISubsystemManager systemMan, IEntityLookup lookupRepo, IConfiguration configuration)
        {
            Guard.Against<ArgumentNullException>(systemMan == null, nameof(systemMan));
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));
            Guard.Against<ArgumentNullException>(configuration == null, nameof(configuration));
            _subsystemMan = systemMan;
            _lookupRepo = lookupRepo;
            _configuration = configuration;
        }

        public ProcessingResult ProcessingResult { get; } = new ProcessingResult();

        public bool Register()
        {
            var application = GetApplication();
            if (application == null)
            {
                ProcessingResult.AddErrorMessage("Cannot get application for automobile");
                return false;
            }

            if (!_subsystemMan.HasApplicationRegistered(application))
            {
                try
                {
                    var success = _subsystemMan.Register(application);
                    switch (success)
                    {
                        case false:
                            ProcessingResult.PropagandaResult(_subsystemMan.ProcessResult);
                            return false;

                        default:
                            return true;
                    }
                }
                catch (Exception e)
                {
                    ProcessingResult.WrapException(e);
                    return false;
                }
            }

            // Update the application if it already registered to system
            try
            {
                var updatedSys = _subsystemMan.Update(application);
                switch (updatedSys)
                {
                    case null:
                        ProcessingResult.PropagandaResult(_subsystemMan.ProcessResult);
                        return false;

                    default:
                        return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public Subsystem GetApplication()
        {
            // Get Default Author
            var author = new Author
            {
                Id = new Guid("3A62B298-6479-4B54-8806-D2BB2B4B3A64"),
                AccountName = "03D9D3DE-0C3C-4775-BEC3-6B698B696837",
                Description = "Automobile default author",
                Role = AuthorRoleType.Author,
                IsActivated = true
            };

            var defaultRender = _lookupRepo.GetEntities<NoteRender>().FirstOrDefault(r => r.IsDefault);
            if (defaultRender == null)
            {
                ProcessingResult.AddErrorMessage($"The application Automobile cannot registered to system, no default render found in data source");
                return null;
            }

            // Get NoteCatalogs
            var catalogs = new List<NoteCatalog>
            {
                new()
                {
                    Name = AutomobileConstant.AutoMobileInfoCatalogName,
                    Description = "automobile catalog",
                    Render = defaultRender,
                    Schema = GetSchema(NoteCatalogType.Automobile),
                    IsDefault = false
                },
                new()
                {
                    Name = AutomobileConstant.GasDiscountCatalogName,
                    Description = "gas discount catalog",
                    Render = defaultRender,
                    Schema = GetSchema(NoteCatalogType.GasDiscount),
                    IsDefault = false
                },
                new()
                {
                    Name = AutomobileConstant.GasLogCatalogName,
                    Description = "gas discount catalog",
                    Render = defaultRender,
                    Schema = GetSchema(NoteCatalogType.GasLog),
                    IsDefault = false
                }
            };
            var app = new Subsystem
            {
                Name = "Automobile",
                Description = "Manage basic information or my car",
                DefaultAuthor = author,
                NoteCatalogs = catalogs
            };

            return app;
        }

        public NoteCatalog GetCatalog(NoteCatalogType entityType)
        {
            NoteCatalog catalog;
            switch (entityType)
            {
                case NoteCatalogType.Automobile:
                    catalog = _automobileCatalog ??= _lookupRepo.GetEntities<NoteCatalog>()
                          .FirstOrDefault(c => c.Name == AutomobileConstant.AutoMobileInfoCatalogName);
                    break;

                case NoteCatalogType.GasDiscount:
                    catalog = _gasDiscountCatalog ??= _lookupRepo.GetEntities<NoteCatalog>()
                        .FirstOrDefault(c => c.Name == AutomobileConstant.GasDiscountCatalogName);
                    break;

                case NoteCatalogType.GasLog:
                    catalog = _gasLogCatalog ??= _automobileCatalog = _lookupRepo.GetEntities<NoteCatalog>()
                        .FirstOrDefault(c => c.Name == AutomobileConstant.GasLogCatalogName);
                    break;

                default:
                    ProcessingResult.AddErrorMessage($"CatalogType {entityType} is not supported");
                    catalog = null;
                    break;
            }

            if (catalog == null && !_lookupRepo.ProcessResult.Success)
            {
                ProcessingResult.PropagandaResult(_lookupRepo.ProcessResult);
            }

            return catalog;
        }

        private string GetSchema(NoteCatalogType catalog)
        {
            var schemaFile = catalog switch
            {
                NoteCatalogType.Automobile => _configuration["Schema:Automobile:Automobile"],
                NoteCatalogType.GasDiscount => _configuration["Schema:Automobile:GasDiscount"],
                NoteCatalogType.GasLog => _configuration["Schema:Automobile:GasLog"],
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
    }
}