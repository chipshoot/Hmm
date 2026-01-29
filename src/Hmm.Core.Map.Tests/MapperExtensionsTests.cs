using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Moq;
using Xunit;

namespace Hmm.Core.Map.Tests
{
    /// <summary>
    /// Tests for MapperExtensions.MapWithNullCheck extension methods.
    /// Verifies null-safe mapping with ProcessingResult pattern.
    /// </summary>
    public class MapperExtensionsTests
    {
        private readonly Mock<IMapper> _mockMapper;

        public MapperExtensionsTests()
        {
            _mockMapper = new Mock<IMapper>();
        }

        [Fact]
        public void MapWithNullCheck_NullSource_ReturnsFailResult()
        {
            // Arrange
            AuthorDao nullSource = null;

            // Act
            var result = _mockMapper.Object.MapWithNullCheck<AuthorDao, Author>(nullSource);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("null", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
            Assert.Contains("AuthorDao", result.ErrorMessage);
        }

        [Fact]
        public void MapWithNullCheck_MappingReturnsNull_ReturnsFailResult()
        {
            // Arrange
            var source = new AuthorDao { Id = 1, AccountName = "test" };
            _mockMapper.Setup(m => m.Map<Author>(source)).Returns((Author)null);

            // Act
            var result = _mockMapper.Object.MapWithNullCheck<AuthorDao, Author>(source);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Cannot convert", result.ErrorMessage);
            Assert.Contains("AuthorDao", result.ErrorMessage);
            Assert.Contains("Author", result.ErrorMessage);
        }

        [Fact]
        public void MapWithNullCheck_SuccessfulMapping_ReturnsOkResult()
        {
            // Arrange
            var source = new AuthorDao { Id = 1, AccountName = "test" };
            var expected = new Author { Id = 1, AccountName = "test" };
            _mockMapper.Setup(m => m.Map<Author>(source)).Returns(expected);

            // Act
            var result = _mockMapper.Object.MapWithNullCheck<AuthorDao, Author>(source);

            // Assert
            Assert.True(result.Success);
            Assert.Same(expected, result.Value);
        }

        [Fact]
        public void MapWithNullCheck_WithCustomErrorMessage_UsesCustomMessage()
        {
            // Arrange
            var source = new AuthorDao { Id = 1, AccountName = "test" };
            _mockMapper.Setup(m => m.Map<Author>(source)).Returns((Author)null);
            var customMessage = "Custom mapping error for test";

            // Act
            var result = _mockMapper.Object.MapWithNullCheck<AuthorDao, Author>(source, customMessage);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(customMessage, result.ErrorMessage);
        }

        [Fact]
        public void MapWithNullCheck_NullSourceWithCustomMessage_UsesCustomMessage()
        {
            // Arrange
            AuthorDao nullSource = null;
            var customMessage = "Source cannot be null";

            // Act
            var result = _mockMapper.Object.MapWithNullCheck<AuthorDao, Author>(nullSource, customMessage);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(customMessage, result.ErrorMessage);
        }

        [Fact]
        public void MapWithNullCheck_DifferentTypes_WorksCorrectly()
        {
            // Arrange - Test with Tag types
            var source = new TagDao { Id = 1, Name = "TestTag", IsActivated = true };
            var expected = new Tag { Id = 1, Name = "TestTag", IsActivated = true };
            _mockMapper.Setup(m => m.Map<Tag>(source)).Returns(expected);

            // Act
            var result = _mockMapper.Object.MapWithNullCheck<TagDao, Tag>(source);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("TestTag", result.Value.Name);
        }

        [Fact]
        public void MapWithSourceNullCheck_NullSource_ReturnsFailResult()
        {
            // Arrange
            AuthorDao nullSource = null;

            // Act
            var result = _mockMapper.Object.MapWithSourceNullCheck<AuthorDao, Author>(nullSource);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("null", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void MapWithSourceNullCheck_MappingReturnsNull_ReturnsOkWithNull()
        {
            // Arrange - This is for cases where null result is acceptable (e.g., empty collections)
            var source = new AuthorDao { Id = 1, AccountName = "test" };
            _mockMapper.Setup(m => m.Map<Author>(source)).Returns((Author)null);

            // Act
            var result = _mockMapper.Object.MapWithSourceNullCheck<AuthorDao, Author>(source);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Value);
        }

        [Fact]
        public void MapWithSourceNullCheck_ValidSource_ReturnsOkResult()
        {
            // Arrange
            var source = new AuthorDao { Id = 1, AccountName = "test" };
            var expected = new Author { Id = 1, AccountName = "test" };
            _mockMapper.Setup(m => m.Map<Author>(source)).Returns(expected);

            // Act
            var result = _mockMapper.Object.MapWithSourceNullCheck<AuthorDao, Author>(source);

            // Assert
            Assert.True(result.Success);
            Assert.Same(expected, result.Value);
        }

        [Fact]
        public void MapWithNullCheck_ErrorMessageContainsTypeNames()
        {
            // Arrange
            var source = new NoteCatalogDao { Id = 1, Name = "Test" };
            _mockMapper.Setup(m => m.Map<NoteCatalog>(source)).Returns((NoteCatalog)null);

            // Act
            var result = _mockMapper.Object.MapWithNullCheck<NoteCatalogDao, NoteCatalog>(source);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("NoteCatalogDao", result.ErrorMessage);
            Assert.Contains("NoteCatalog", result.ErrorMessage);
        }
    }
}
