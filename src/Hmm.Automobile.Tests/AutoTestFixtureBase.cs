using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Moq;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using DomainNoteContentFormatType = Hmm.Core.Map.DomainEntity.NoteContentFormatType;

namespace Hmm.Automobile.Tests
{
    public class AutoTestFixtureBase : CoreTestFixtureBase
    {
        protected readonly XNamespace XmlNamespace = @"http://schema.hmm.com/2020";
        private IApplication _app;
        private bool _automobileCatalogsAdded;
        private static Author _testDefaultAuthor;

        protected const string NoteXmlTextBase =
            "<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{0}\"><Content>{1}</Content></Note>";

        protected IApplication Application => _app ??= GetApplication();

        /// <summary>
        /// Gets the default author used for automobile tests.
        /// This is a test-only property that provides a consistent author for testing.
        /// </summary>
        protected static Author TestDefaultAuthor => _testDefaultAuthor ??= CreateTestDefaultAuthor();

        /// <summary>
        /// Creates a mock IAuthorProvider that returns TestDefaultAuthor.
        /// </summary>
        protected static IAuthorProvider CreateMockAuthorProvider()
        {
            var mock = new Mock<IAuthorProvider>();
            mock.Setup(p => p.GetAuthorAsync())
                .ReturnsAsync(ProcessingResult<Author>.Ok(TestDefaultAuthor));
            mock.Setup(p => p.CachedAuthor).Returns(TestDefaultAuthor);
            return mock.Object;
        }

        /// <summary>
        /// Creates a mock IDefaultAuthorProvider that returns TestDefaultAuthor.
        /// </summary>
        protected static IDefaultAuthorProvider CreateMockDefaultAuthorProvider()
        {
            var mock = new Mock<IDefaultAuthorProvider>();
            mock.Setup(p => p.GetAuthorAsync())
                .ReturnsAsync(ProcessingResult<Author>.Ok(TestDefaultAuthor));
            mock.Setup(p => p.GetDefaultAuthorAsync())
                .ReturnsAsync(ProcessingResult<Author>.Ok(TestDefaultAuthor));
            mock.Setup(p => p.CachedAuthor).Returns(TestDefaultAuthor);
            return mock.Object;
        }

        private static Author CreateTestDefaultAuthor()
        {
            var author = new Author
            {
                AccountName = "automobile-test-author",
                Description = "Automobile test default author",
                Role = AuthorRoleType.Author,
                IsActivated = true
            };

            // Set ID via reflection for testing
            var idProperty = typeof(Author).GetProperty("Id");
            idProperty?.SetValue(author, 1);

            return author;
        }

        protected void InsertSeedRecords()
        {
            // Add automobile-specific catalogs and author lookup
            AddAutomobileCatalogs();
            SetupDomainEntityLookups();
        }

        private void SetupDomainEntityLookups()
        {
            var lookupMock = Mock.Get(LookupRepository);
            var defaultAuthor = TestDefaultAuthor;

            // Setup GetEntityAsync<Author> for validation
            lookupMock.Setup(lk => lk.GetEntityAsync<Author>(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    // Return default author if ID matches
                    if (id == defaultAuthor.Id || id == 1)
                    {
                        return ProcessingResult<Author>.Ok(defaultAuthor);
                    }
                    return ProcessingResult<Author>.NotFound($"Author with ID {id} not found");
                });
        }

        private void AddAutomobileCatalogs()
        {
            if (_automobileCatalogsAdded)
            {
                return;
            }

            var lookupMock = Mock.Get(LookupRepository);

            var automobileCatalog = new NoteCatalog
            {
                Id = 200,
                Name = AutomobileConstant.AutoMobileInfoCatalogName,
                Type = DomainNoteContentFormatType.Xml,
                Description = "Automobile info catalog",
                Schema = File.Exists("Automobile.xsd") ? File.ReadAllText("Automobile.xsd") : ""
            };

            var gasLogCatalog = new NoteCatalog
            {
                Id = 201,
                Name = AutomobileConstant.GasLogCatalogName,
                Type = DomainNoteContentFormatType.Xml,
                Description = "Gas log catalog",
                Schema = File.Exists("GasLog.xsd") ? File.ReadAllText("GasLog.xsd") : ""
            };

            var gasDiscountCatalog = new NoteCatalog
            {
                Id = 202,
                Name = AutomobileConstant.GasDiscountCatalogName,
                Type = DomainNoteContentFormatType.Xml,
                Description = "Gas discount catalog",
                Schema = File.Exists("Discount.xsd") ? File.ReadAllText("Discount.xsd") : ""
            };

            var gasStationCatalog = new NoteCatalog
            {
                Id = 203,
                Name = AutomobileConstant.GasStationCatalogName,
                Type = DomainNoteContentFormatType.Xml,
                Description = "Gas station catalog",
                Schema = ""
            };

            var catalogs = new[] { automobileCatalog, gasLogCatalog, gasDiscountCatalog, gasStationCatalog };

            // Setup GetEntitiesAsync for NoteCatalog domain entity
            lookupMock.Setup(lk => lk.GetEntitiesAsync(
                    It.IsAny<Expression<System.Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync((Expression<System.Func<NoteCatalog, bool>> query, ResourceCollectionParameters para) =>
                {
                    var filtered = catalogs.AsQueryable().Where(query).ToList();
                    return ProcessingResult<PageList<NoteCatalog>>.Ok(
                        PageList<NoteCatalog>.Create(filtered.AsQueryable(), 1, 20));
                });

            _automobileCatalogsAdded = true;
        }

        private IApplication GetApplication()
        {
            var fakeApplication = new Mock<IApplication>();

            fakeApplication.Setup(app => app.GetCatalogAsync(It.IsAny<NoteCatalogType>(), It.IsAny<IEntityLookup>()))
                .ReturnsAsync((NoteCatalogType type, IEntityLookup lookupRepo) => GetCatalogForType(type));

            fakeApplication.Setup(app => app.RegisterAsync(It.IsAny<IEntityLookup>()))
                .ReturnsAsync(ProcessingResult<bool>.Ok(true));

            return fakeApplication.Object;
        }

        private static NoteCatalog GetCatalogForType(NoteCatalogType type)
        {
            return type switch
            {
                NoteCatalogType.Automobile => new NoteCatalog
                {
                    Id = 200,
                    Name = AutomobileConstant.AutoMobileInfoCatalogName,
                    Schema = File.Exists("Automobile.xsd") ? File.ReadAllText("Automobile.xsd") : "",
                    Type = DomainNoteContentFormatType.Xml,
                    Description = "Automobile info catalog"
                },
                NoteCatalogType.GasDiscount => new NoteCatalog
                {
                    Id = 202,
                    Name = AutomobileConstant.GasDiscountCatalogName,
                    Schema = File.Exists("Discount.xsd") ? File.ReadAllText("Discount.xsd") : "",
                    Type = DomainNoteContentFormatType.Xml,
                    Description = "Gas discount catalog"
                },
                NoteCatalogType.GasLog => new NoteCatalog
                {
                    Id = 201,
                    Name = AutomobileConstant.GasLogCatalogName,
                    Schema = File.Exists("GasLog.xsd") ? File.ReadAllText("GasLog.xsd") : "",
                    Type = DomainNoteContentFormatType.Xml,
                    Description = "Gas log catalog"
                },
                NoteCatalogType.GasStation => new NoteCatalog
                {
                    Id = 203,
                    Name = AutomobileConstant.GasStationCatalogName,
                    Schema = "",
                    Type = DomainNoteContentFormatType.Xml,
                    Description = "Gas station catalog"
                },
                _ => null
            };
        }
    }
}