using System.Linq;
using AutoMapper;
using Hmm.Core.DefaultManager;
using Hmm.Core.Map;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests
{
    public class NoteCatalogManagerTests : CoreTestFixtureBase
    {
        private readonly NoteCatalogManager _catalogManager;

        public NoteCatalogManagerTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<HmmMappingProfile>();
            });
            var mapper = config.CreateMapper();
            _catalogManager = new NoteCatalogManager(CatalogRepository, mapper);
        }

        [Fact]
        public async Task Can_Add_Catalog_To_DataSource()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Schema = "test schema"
            };

            // Act
            var newCatalog = await _catalogManager.CreateAsync(catalog);

            // Assert
            Assert.True(_catalogManager.ProcessResult.Success);
            Assert.NotNull(newCatalog);
            Assert.True(newCatalog.Id >= 1, "newNote.Id >=1");
            Assert.Equal("Test Catalog", newCatalog.Name);
        }

        [Fact]
        public async Task Cannot_Add_Invalid_Catalog_To_DataSource()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = GetRandomString(255),
                Schema = "test schema"
            };

            // Act
            var newCatalog = await _catalogManager.CreateAsync(catalog);

            // Assert
            Assert.False(_catalogManager.ProcessResult.Success);
            Assert.Equal("Name : 'Name' must be between 1 and 200 characters. You entered 255 characters.", _catalogManager.ProcessResult.MessageList.First().Message);
            Assert.Null(newCatalog);
        }

        [Fact]
        public async Task Can_Update_Catalog()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Schema = "test schema"
            };
            await _catalogManager.CreateAsync(catalog);
            var savedCatalog = await _catalogManager.GetEntityByIdAsync(catalog.Id);
            Assert.NotNull(savedCatalog);

            // Act
            savedCatalog.Schema = "changed test schema";
            var updatedCatalog = await _catalogManager.UpdateAsync(savedCatalog);

            // Assert
            Assert.True(_catalogManager.ProcessResult.Success);
            Assert.NotNull(updatedCatalog);
            Assert.Equal("changed test schema", updatedCatalog.Schema);
        }

        [Fact]
        public async Task Cannot_Update_Invalid_Catalog()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Schema = "test schema"
            };
            var savedCatalog = await _catalogManager.CreateAsync(catalog);

            // Act
            savedCatalog.Name = GetRandomString(255);
            var updatedCatalog = await _catalogManager.UpdateAsync(savedCatalog);

            // Assert
            Assert.False(_catalogManager.ProcessResult.Success);
            Assert.Equal("Name : 'Name' must be between 1 and 200 characters. You entered 255 characters.", _catalogManager.ProcessResult.MessageList.First().Message);
            Assert.Null(updatedCatalog);
        }

        [Fact]
        public async Task Cannot_Update_Not_Exists_Catalog()
        {
            // Arrange - no id
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Schema = "test schema"
            };

            // Act
            var updatedCatalog = await _catalogManager.UpdateAsync(catalog);

            // Assert
            Assert.False(_catalogManager.ProcessResult.Success);
            Assert.Null(updatedCatalog);

            // Arrange - id does not exists
            _catalogManager.ProcessResult.Rest();
            catalog = new NoteCatalog
            {
                Id = 1000,
                Name = "Test Catalog",
                Schema = "test schema"
            };

            // Act
            updatedCatalog = await _catalogManager.UpdateAsync(catalog);

            // Assert
            Assert.False(_catalogManager.ProcessResult.Success);
            Assert.Null(updatedCatalog);
        }

        [Fact]
        public async Task Can_Search_Catalog_By_Id()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Schema = "test schema"
            };
            var newCatalog = await _catalogManager.CreateAsync(catalog);

            // Act
            var savedNote = await _catalogManager.GetEntityByIdAsync(newCatalog.Id);

            // Assert
            Assert.True(_catalogManager.ProcessResult.Success);
            Assert.NotNull(savedNote);
            Assert.Equal(savedNote.Name, catalog.Name);
        }
    }
}