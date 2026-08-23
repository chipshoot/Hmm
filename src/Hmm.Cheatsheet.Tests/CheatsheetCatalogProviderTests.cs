using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetCatalogProviderTests
    {
        private static Mock<IEntityLookup> LookupReturning(params NoteCatalog[] catalogs)
        {
            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(
                    new PageList<NoteCatalog>(catalogs, catalogs.Length, 1, 10)));
            return lookup;
        }

        [Fact]
        public async Task GetCatalogAsync_ReturnsTheCheatsheetCatalog()
        {
            // Arrange
            var catalog = new NoteCatalog { Id = 7, Name = CheatsheetConstant.CheatsheetCatalogName };
            var provider = new CheatsheetCatalogProvider(LookupReturning(catalog).Object);

            // Act
            var result = await provider.GetCatalogAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(7, result.Id);
        }

        [Fact]
        public async Task GetCatalogAsync_HitsTheLookupOnlyOnce()
        {
            // Arrange
            var catalog = new NoteCatalog { Id = 7, Name = CheatsheetConstant.CheatsheetCatalogName };
            var lookup = LookupReturning(catalog);
            var provider = new CheatsheetCatalogProvider(lookup.Object);

            // Act
            await provider.GetCatalogAsync();
            await provider.GetCatalogAsync();

            // Assert
            lookup.Verify(
                l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()),
                Times.Once);
        }

        [Fact]
        public async Task GetCatalogAsync_ReturnsNull_WhenCatalogMissing()
        {
            // Arrange
            var provider = new CheatsheetCatalogProvider(LookupReturning(Array.Empty<NoteCatalog>()).Object);

            // Act
            var result = await provider.GetCatalogAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCatalogAsync_ReturnsNull_WhenLookupFails()
        {
            // Arrange
            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Fail("boom"));
            var provider = new CheatsheetCatalogProvider(lookup.Object);

            // Act
            var result = await provider.GetCatalogAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Constructor_Throws_WhenLookupIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new CheatsheetCatalogProvider(null));
        }
    }
}
