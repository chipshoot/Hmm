//using Hmm.Core.DomainEntity;
//using Hmm.Utility.TestHelp;
//using Moq;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Xml.Linq;
//using Hmm.Core.DbEntity;
//using Hmm.Utility.Dal.Query;

//namespace Hmm.Automobile.Tests
//{
//    public class AutoTestFixtureBase : TestFixtureBase
//    {
//        protected readonly XNamespace XmlNamespace = @"http://schema.hmm.com/2020";
//        private IApplication _app;

//        protected const string NoteXmlTextBase =
//            "<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{0}\"><Content>{1}</Content></Note>";

//        protected IApplication Application => _app ??= GetApplication();

//        protected override void InsertSeedRecords(
//            List<AuthorDb> authors = null,
//            List<NoteCatalog> catalogs = null,
//            List<ContactDb> contracts = null)
//        {
//            authors ??= new List<AuthorDb>();
//            catalogs ??= new List<NoteCatalog>();

//            authors.Add(ApplicationRegister.DefaultAuthor);

//            catalogs.Add(
//                new NoteCatalog
//                {
//                    Name = AutomobileConstant.GasLogCatalogName,
//                    Schema = File.ReadAllText("GasLog.xsd"),
//                    Description = "Testing catalog"
//                });
//            catalogs.Add(
//                new NoteCatalog
//                {
//                    Name = AutomobileConstant.AutoMobileInfoCatalogName,
//                    Schema = File.ReadAllText("Automobile.xsd"),
//                    Description = "Testing automobile note"
//                });
//            catalogs.Add(
//                new NoteCatalog
//                {
//                    Name = AutomobileConstant.GasDiscountCatalogName,
//                    Schema = File.ReadAllText("Discount.xsd"),
//                    Description = "Testing discount note"
//                });
//            base.InsertSeedRecords(authors, catalogs);
//        }

//        private IApplication GetApplication()
//        {
//            var user = LookupRepository.GetEntities<AuthorDb>().FirstOrDefault();

//            var fakeApplication = new Mock<IApplication>();
//            fakeApplication.Setup(app => app.GetCatalog(It.IsAny<NoteCatalogType>(), It.IsAny<IEntityLookup>())).Returns((NoteCatalogType type, IEntityLookup lookupRepo) =>
//            {
//                NoteCatalog catalog;
//                switch (type)
//                {
//                    case NoteCatalogType.Automobile:
//                        catalog = LookupRepository.GetEntities<NoteCatalog>()
//                            .FirstOrDefault(c => c.Name == AutomobileConstant.AutoMobileInfoCatalogName);
//                        if (catalog != null)
//                        {
//                            var schemaStr = File.ReadAllText("Automobile.xsd");
//                            catalog.Schema = schemaStr;
//                        }
//                        break;

//                    case NoteCatalogType.GasDiscount:
//                        catalog = LookupRepository.GetEntities<NoteCatalog>()
//                            .FirstOrDefault(c => c.Name == AutomobileConstant.GasDiscountCatalogName);
//                        if (catalog != null)
//                        {
//                            var schemaStr = File.ReadAllText("Discount.xsd");
//                            catalog.Schema = schemaStr;
//                        }
//                        break;

//                    case NoteCatalogType.GasLog:
//                        catalog = LookupRepository.GetEntities<NoteCatalog>()
//                            .FirstOrDefault(c => c.Name == AutomobileConstant.GasLogCatalogName);
//                        if (catalog != null)
//                        {
//                            var schemaStr = File.ReadAllText("GasLog.xsd");
//                            catalog.Schema = schemaStr;
//                        }
//                        break;

//                    default:
//                        catalog = null;
//                        break;
//                }

//                return catalog;
//            });

//            return fakeApplication.Object;
//        }
//    }
//}