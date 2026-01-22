using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    /// <summary>
    /// Unit tests for RepositoryBase.PropertyCheckingAsync method.
    /// Tests verify the Open/Closed Principle implementation for default entity resolution.
    /// </summary>
    public class RepositoryBaseTests
    {
        private readonly Mock<IHmmDataContext> _mockDataContext;
        private readonly Mock<IEntityLookup> _mockLookupRepository;
        private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
        private readonly Mock<ILogger> _mockLogger;
        private readonly TestableRepositoryBase _repository;

        public RepositoryBaseTests()
        {
            _mockDataContext = new Mock<IHmmDataContext>();
            _mockLookupRepository = new Mock<IEntityLookup>();
            _mockDateTimeProvider = new Mock<IDateTimeProvider>();
            _mockLogger = new Mock<ILogger>();

            _repository = new TestableRepositoryBase(
                _mockDataContext.Object,
                _mockLookupRepository.Object,
                _mockDateTimeProvider.Object,
                _mockLogger.Object);
        }

        #region Null Property Tests

        [Fact]
        public async Task PropertyCheckingAsync_WhenPropertyIsNull_ReturnsDefaultEntity()
        {
            // Arrange
            var defaultCatalog = CreateNoteCatalogDao(1, "Default", isDefault: true);
            _mockDataContext
                .Setup(x => x.GetDefaultEntityAsync<NoteCatalogDao>())
                .ReturnsAsync(defaultCatalog);

            // Act
            var result = await _repository.TestPropertyCheckingAsync<NoteCatalogDao>(null);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(defaultCatalog.Id, result.Value.Id);
            Assert.True(result.Value.IsDefault);
            _mockDataContext.Verify(x => x.GetDefaultEntityAsync<NoteCatalogDao>(), Times.Once);
        }

        [Fact]
        public async Task PropertyCheckingAsync_WhenPropertyIsNull_AndNoDefaultExists_ReturnsNotFound()
        {
            // Arrange
            _mockDataContext
                .Setup(x => x.GetDefaultEntityAsync<NoteCatalogDao>())
                .ReturnsAsync((NoteCatalogDao)null);

            // Act
            var result = await _repository.TestPropertyCheckingAsync<NoteCatalogDao>(null);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
            Assert.Contains("No default", result.ErrorMessage);
            Assert.Contains("NoteCatalogDao", result.ErrorMessage);
        }

        #endregion

        #region Invalid Id Tests

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task PropertyCheckingAsync_WhenPropertyIdIsZeroOrNegative_ReturnsDefaultEntity(int invalidId)
        {
            // Arrange
            var invalidCatalog = CreateNoteCatalogDao(invalidId, "Invalid", isDefault: false);
            var defaultCatalog = CreateNoteCatalogDao(1, "Default", isDefault: true);

            _mockDataContext
                .Setup(x => x.GetDefaultEntityAsync<NoteCatalogDao>())
                .ReturnsAsync(defaultCatalog);

            // Act
            var result = await _repository.TestPropertyCheckingAsync(invalidCatalog);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(defaultCatalog.Id, result.Value.Id);
            Assert.True(result.Value.IsDefault);

            // Verify lookup was NOT called since Id <= 0
            _mockLookupRepository.Verify(x => x.GetEntityAsync<NoteCatalogDao>(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region Valid Property Tests

        [Fact]
        public async Task PropertyCheckingAsync_WhenPropertyExistsInLookup_ReturnsOriginalProperty()
        {
            // Arrange
            var existingCatalog = CreateNoteCatalogDao(5, "Existing", isDefault: false);

            _mockLookupRepository
                .Setup(x => x.GetEntityAsync<NoteCatalogDao>(5))
                .ReturnsAsync(ProcessingResult<NoteCatalogDao>.Ok(existingCatalog));

            // Act
            var result = await _repository.TestPropertyCheckingAsync(existingCatalog);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(existingCatalog.Id, result.Value.Id);
            Assert.Equal("Existing", result.Value.Name);

            // Verify GetDefaultEntityAsync was NOT called
            _mockDataContext.Verify(x => x.GetDefaultEntityAsync<NoteCatalogDao>(), Times.Never);
        }

        [Fact]
        public async Task PropertyCheckingAsync_WhenPropertyNotFoundInLookup_ReturnsDefaultEntity()
        {
            // Arrange
            var nonExistentCatalog = CreateNoteCatalogDao(999, "NonExistent", isDefault: false);
            var defaultCatalog = CreateNoteCatalogDao(1, "Default", isDefault: true);

            _mockLookupRepository
                .Setup(x => x.GetEntityAsync<NoteCatalogDao>(999))
                .ReturnsAsync(ProcessingResult<NoteCatalogDao>.NotFound("Entity not found"));

            _mockDataContext
                .Setup(x => x.GetDefaultEntityAsync<NoteCatalogDao>())
                .ReturnsAsync(defaultCatalog);

            // Act
            var result = await _repository.TestPropertyCheckingAsync(nonExistentCatalog);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(defaultCatalog.Id, result.Value.Id);
            Assert.True(result.Value.IsDefault);
        }

        [Fact]
        public async Task PropertyCheckingAsync_WhenLookupFails_ReturnsDefaultEntity()
        {
            // Arrange
            var catalogWithFailedLookup = CreateNoteCatalogDao(10, "FailedLookup", isDefault: false);
            var defaultCatalog = CreateNoteCatalogDao(1, "Default", isDefault: true);

            _mockLookupRepository
                .Setup(x => x.GetEntityAsync<NoteCatalogDao>(10))
                .ReturnsAsync(ProcessingResult<NoteCatalogDao>.Fail("Database error"));

            _mockDataContext
                .Setup(x => x.GetDefaultEntityAsync<NoteCatalogDao>())
                .ReturnsAsync(defaultCatalog);

            // Act
            var result = await _repository.TestPropertyCheckingAsync(catalogWithFailedLookup);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(defaultCatalog.Id, result.Value.Id);
        }

        #endregion

        #region Exception Handling Tests

        [Fact]
        public async Task PropertyCheckingAsync_WhenLookupThrowsException_ReturnsFromException()
        {
            // Arrange
            var catalog = CreateNoteCatalogDao(5, "Test", isDefault: false);
            var expectedException = new InvalidOperationException("Database connection failed");

            _mockLookupRepository
                .Setup(x => x.GetEntityAsync<NoteCatalogDao>(5))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _repository.TestPropertyCheckingAsync(catalog);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Database connection failed", result.ErrorMessage);
        }

        [Fact]
        public async Task PropertyCheckingAsync_WhenGetDefaultThrowsException_ReturnsFromException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Cannot access database");

            _mockDataContext
                .Setup(x => x.GetDefaultEntityAsync<NoteCatalogDao>())
                .ThrowsAsync(expectedException);

            // Act
            var result = await _repository.TestPropertyCheckingAsync<NoteCatalogDao>(null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot access database", result.ErrorMessage);
        }

        #endregion

        #region Open/Closed Principle Tests

        [Fact]
        public async Task PropertyCheckingAsync_WorksWithAnyHasDefaultEntityType_WithoutModification()
        {
            // Arrange - Using a different entity type that extends HasDefaultEntity
            // This test verifies the Open/Closed Principle - new entity types work without modification
            var testEntity = new TestHasDefaultEntity { Id = 0, IsDefault = false };
            var defaultEntity = new TestHasDefaultEntity { Id = 1, IsDefault = true, TestProperty = "Default" };

            _mockDataContext
                .Setup(x => x.GetDefaultEntityAsync<TestHasDefaultEntity>())
                .ReturnsAsync(defaultEntity);

            // Act
            var result = await _repository.TestPropertyCheckingAsync(testEntity);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(1, result.Value.Id);
            Assert.True(result.Value.IsDefault);
            Assert.Equal("Default", ((TestHasDefaultEntity)result.Value).TestProperty);
        }

        #endregion

        #region Helper Methods

        private static NoteCatalogDao CreateNoteCatalogDao(int id, string name, bool isDefault)
        {
            return new NoteCatalogDao
            {
                Id = id,
                Name = name,
                IsDefault = isDefault,
                Schema = "<root/>",
                Description = $"Test catalog: {name}"
            };
        }

        #endregion

        #region Test Helpers

        /// <summary>
        /// Testable subclass of RepositoryBase that exposes the protected PropertyCheckingAsync method.
        /// </summary>
        private class TestableRepositoryBase : RepositoryBase
        {
            public TestableRepositoryBase(
                IHmmDataContext dataContext,
                IEntityLookup lookupRepository,
                IDateTimeProvider dateTimeProvider,
                ILogger logger = null)
                : base(dataContext, lookupRepository, dateTimeProvider, logger)
            {
            }

            public Task<ProcessingResult<TP>> TestPropertyCheckingAsync<TP>(TP property) where TP : HasDefaultEntity
            {
                return PropertyCheckingAsync(property);
            }
        }

        /// <summary>
        /// Test entity class that extends HasDefaultEntity for Open/Closed Principle testing.
        /// This demonstrates that new entity types work without modifying RepositoryBase.
        /// </summary>
        private class TestHasDefaultEntity : HasDefaultEntity
        {
            public string TestProperty { get; set; }
        }

        #endregion
    }
}
