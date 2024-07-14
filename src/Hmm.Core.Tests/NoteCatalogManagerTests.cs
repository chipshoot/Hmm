//using Hmm.Core.DefaultManager;
//using Hmm.Core.DomainEntity;
//using Hmm.Utility.TestHelp;
//using System.Linq;
//using Xunit;

//namespace Hmm.Core.Tests
//{
//    public class NoteCatalogManagerTests : TestFixtureBase
//    {
//        private INoteCatalogManager _manager;
//        private MockCatalogValidator _testValidator;

//        public NoteCatalogManagerTests()
//        {
//            SetupTestEnv();
//        }

//        [Fact]
//        public void Can_Add_Catalog_To_DataSource()
//        {
//            // Arrange
//            var catalog = new NoteCatalog
//            {
//                Name = "Test Catalog",
//                Schema = "test schema"
//            };

//            // Act
//            var newCatalog = _manager.Create(catalog);

//            // Assert
//            Assert.True(_manager.ProcessResult.Success);
//            Assert.NotNull(newCatalog);
//            Assert.True(newCatalog.Id >= 1, "newNote.Id >=1");
//            Assert.Equal("Test Catalog", newCatalog.Name);
//        }

//        [Fact]
//        public void Can_Update_Catalog()
//        {
//            // Arrange
//            var catalog = new NoteCatalog
//            {
//                Name = "Test Catalog",
//                Schema = "test schema"
//            };
//            _manager.Create(catalog);
//            var savedCatalog = _manager.GetEntities().FirstOrDefault(c => c.Id == 1);
//            Assert.NotNull(savedCatalog);

//            // Act
//            savedCatalog.Schema = "changed test schema";
//            var updatedCatalog = _manager.Update(savedCatalog);

//            // Assert
//            Assert.True(_manager.ProcessResult.Success);
//            Assert.NotNull(updatedCatalog);
//            Assert.Equal("changed test schema", updatedCatalog.Schema);
//        }

//        [Fact]
//        public void Cannot_Update_Invalid_Catalog()
//        {
//            // Arrange
//            var catalog = new NoteCatalog
//            {
//                Name = "Test Catalog",
//                Schema = "test schema"
//            };
//            var savedCatalog = _manager.Create(catalog);
//            _testValidator.GetInvalidResult = true;

//            // Act
//            savedCatalog.Name = "updated test catalog";
//            var updatedCatalog = _manager.Update(savedCatalog);

//            // Assert
//            Assert.False(_manager.ProcessResult.Success);
//            Assert.Null(updatedCatalog);
//        }

//        [Fact]
//        public void Cannot_Update_Not_Exists_Catalog()
//        {
//            // Arrange - no id
//            var catalog = new NoteCatalog
//            {
//                Name = "Test Catalog",
//                Schema = "test schema"
//            };

//            // Act
//            var updatedCatalog = _manager.Update(catalog);

//            // Assert
//            Assert.False(_manager.ProcessResult.Success);
//            Assert.Null(updatedCatalog);

//            // Arrange - id does not exists
//            _manager.ProcessResult.Rest();
//            catalog = new NoteCatalog
//            {
//                Id = 100,
//                Name = "Test Catalog",
//                Schema = "test schema"
//            };

//            // Act
//            updatedCatalog = _manager.Update(catalog);

//            // Assert
//            Assert.False(_manager.ProcessResult.Success);
//            Assert.Null(updatedCatalog);
//        }

//        [Fact]
//        public void Can_Search_Catalog_By_Id()
//        {
//            // Arrange
//            var catalog = new NoteCatalog
//            {
//                Name = "Test Catalog",
//                Schema = "test schema"
//            };
//            var newCatalog = _manager.Create(catalog);

//            // Act
//            var savedNote = _manager.GetEntities().FirstOrDefault(c => c.Id == newCatalog.Id);

//            // Assert
//            Assert.True(_manager.ProcessResult.Success);
//            Assert.NotNull(savedNote);
//            Assert.Equal(savedNote.Name, catalog.Name);
//        }

//        private void SetupTestEnv()
//        {
//            InsertSeedRecords();
//            _testValidator = FakeCatalogValidator;
//            _manager = new NoteCatalogManager(CatalogRepository, _testValidator, LookupRepository);
//        }
//    }
//}