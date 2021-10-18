using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Hmm.Automobile.Tests
{
    public class AutoTestFixtureBase : TestFixtureBase
    {
        protected readonly XNamespace XmlNamespace = @"http://schema.hmm.com/2020";
        private IApplication _app;

        protected const string NoteXmlTextBase =
            "<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{0}\"><Content>{1}</Content></Note>";

        protected IApplication Application => _app ??= GetApplication();

        protected override void InsertSeedRecords(
            List<Subsystem> systems = null,
            List<Author> authors = null,
            List<NoteRender> renders = null,
            List<NoteCatalog> catalogs = null)
        {
            systems ??= new List<Subsystem>();
            renders ??= new List<NoteRender>();
            catalogs ??= new List<NoteCatalog>();

            systems.Add(
                new Subsystem
                {
                    Name = "Automobile",
                    Description = "A Basic information manage system for the car"
                });

            renders.Add(
                new NoteRender
                {
                    Name = "GasLog",
                    Namespace = "Hmm.Renders",
                    Description = "Testing gal log render"
                });

            catalogs.Add(
                new NoteCatalog
                {
                    Name = AutomobileConstant.GasLogCatalogName,
                    Schema = File.ReadAllText("GasLog.xsd"),
                    Subsystem = new Subsystem { Name = "default subsystem" },
                    Render = renders[0],
                    Description = "Testing catalog"
                });
            catalogs.Add(
                new NoteCatalog
                {
                    Name = AutomobileConstant.AutoMobileInfoCatalogName,
                    Schema = File.ReadAllText("Automobile.xsd"),
                    Subsystem = new Subsystem { Name = "default subsystem" },
                    Render = renders[0],
                    Description = "Testing automobile note"
                });
            catalogs.Add(
                new NoteCatalog
                {
                    Name = AutomobileConstant.GasDiscountCatalogName,
                    Schema = File.ReadAllText("Discount.xsd"),
                    Subsystem = new Subsystem { Name = "default subsystem" },
                    Render = renders[0],
                    Description = "Testing discount note"
                });
            base.InsertSeedRecords(systems, authors, renders, catalogs);
        }

        private IApplication GetApplication()
        {
            var user = LookupRepo.GetEntities<Author>().FirstOrDefault();
            var system = new Subsystem
            {
                DefaultAuthor = user,
            };

            var fakeApplication = new Mock<IApplication>();
            fakeApplication.Setup(app => app.GetApplication()).Returns(system);
            fakeApplication.Setup(app => app.GetCatalog(It.IsAny<NoteCatalogType>())).Returns((NoteCatalogType type) =>
            {
                NoteCatalog catalog;
                switch (type)
                {
                    case NoteCatalogType.Automobile:
                        catalog = LookupRepo.GetEntities<NoteCatalog>()
                            .FirstOrDefault(c => c.Name == AutomobileConstant.AutoMobileInfoCatalogName);
                        if (catalog != null)
                        {
                            var schemaStr = File.ReadAllText("Automobile.xsd");
                            catalog.Schema = schemaStr;
                        }
                        break;

                    case NoteCatalogType.GasDiscount:
                        catalog = LookupRepo.GetEntities<NoteCatalog>()
                            .FirstOrDefault(c => c.Name == AutomobileConstant.GasDiscountCatalogName);
                        if (catalog != null)
                        {
                            var schemaStr = File.ReadAllText("Discount.xsd");
                            catalog.Schema = schemaStr;
                        }
                        break;

                    case NoteCatalogType.GasLog:
                        catalog = LookupRepo.GetEntities<NoteCatalog>()
                            .FirstOrDefault(c => c.Name == AutomobileConstant.GasLogCatalogName);
                        if (catalog != null)
                        {
                            var schemaStr = File.ReadAllText("GasLog.xsd");
                            catalog.Schema = schemaStr;
                        }
                        break;

                    default:
                        catalog = null;
                        break;
                }

                return catalog;
            });

            return fakeApplication.Object;
        }
    }
}