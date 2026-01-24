using System.Linq;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests
{
    public class NoteCatalogManagerTests : CoreTestFixtureBase
    {
        private readonly NoteCatalogManager _catalogManager;
        private readonly NoteCatalogManager _catalogManagerWithRealValidator;
        private readonly Mock<IHmmValidator<NoteCatalog>> _mockValidator;

        public NoteCatalogManagerTests()
        {
            _mockValidator = new Mock<IHmmValidator<NoteCatalog>>();
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<NoteCatalog>()))
                .ReturnsAsync(ProcessingResult<NoteCatalog>.Ok(It.IsAny<NoteCatalog>()));
            _catalogManager = new NoteCatalogManager(CatalogRepository, Mapper, LookupRepository, _mockValidator.Object);
            _catalogManagerWithRealValidator = new NoteCatalogManager(CatalogRepository, Mapper, LookupRepository, new NoteCatalogValidator(CatalogRepository));
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
            var newCatalogResult = await _catalogManager.CreateAsync(catalog);

            // Assert
            Assert.True(newCatalogResult.Success);
            Assert.NotNull(newCatalogResult.Value);
            Assert.True(newCatalogResult.Value.Id >= 1, "newNote.Id >=1");
            Assert.Equal("Test Catalog", newCatalogResult.Value.Name);
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
            var newCatalogResult = await _catalogManagerWithRealValidator.CreateAsync(catalog);

            // Assert
            Assert.False(newCatalogResult.Success);
            Assert.Contains("'Name' must be between 1 and 200 characters", newCatalogResult.Messages.First().Message);
            Assert.Null(newCatalogResult.Value);
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
            var createResult = await _catalogManager.CreateAsync(catalog);
            Assert.True(createResult.Success, "Catalog should be created successfully");
            var savedCatalogResult = await _catalogManager.GetEntityByIdAsync(createResult.Value.Id);
            Assert.NotNull(savedCatalogResult.Value);

            // Act
            savedCatalogResult.Value.Schema = "changed test schema";
            var updatedCatalogResult = await _catalogManager.UpdateAsync(savedCatalogResult.Value);

            // Assert
            Assert.True(updatedCatalogResult.Success);
            Assert.NotNull(updatedCatalogResult.Value);
            Assert.Equal("changed test schema", updatedCatalogResult.Value.Schema);
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
            var savedCatalogResult = await _catalogManager.CreateAsync(catalog);
            Assert.True(savedCatalogResult.Success, "Catalog should be created successfully");

            // Act
            var catalogToUpdate = savedCatalogResult.Value;
            catalogToUpdate.Name = GetRandomString(255);
            var updatedCatalogResult = await _catalogManagerWithRealValidator.UpdateAsync(catalogToUpdate);

            // Assert
            Assert.False(updatedCatalogResult.Success);
            Assert.Contains("'Name' must be between 1 and 200 characters", updatedCatalogResult.Messages.First().Message);
            Assert.Null(updatedCatalogResult.Value);
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
            var updatedCatalogResult = await _catalogManager.UpdateAsync(catalog);

            // Assert
            Assert.False(updatedCatalogResult.Success);
            Assert.Null(updatedCatalogResult.Value);

            // Arrange - id does not exists
            catalog = new NoteCatalog
            {
                Id = 1000,
                Name = "Test Catalog",
                Schema = "test schema"
            };

            // Act
            updatedCatalogResult = await _catalogManager.UpdateAsync(catalog);

            // Assert
            Assert.False(updatedCatalogResult.Success);
            Assert.Null(updatedCatalogResult.Value);
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
            var newCatalogResult = await _catalogManager.CreateAsync(catalog);

            // Act
            var savedNoteResult = await _catalogManager.GetEntityByIdAsync(newCatalogResult.Value.Id);

            // Assert
            Assert.True(savedNoteResult.Success);
            Assert.NotNull(savedNoteResult.Value);
            Assert.Equal(savedNoteResult.Value.Name, catalog.Name);
        }
    }
}