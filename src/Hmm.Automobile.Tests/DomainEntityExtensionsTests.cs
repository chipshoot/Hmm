using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Misc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    /// <summary>
    /// Tests for DomainEntityExtensions to verify GetSubject() and GetCatalogIdAsync()
    /// extension methods work correctly. These tests verify issue #57 is resolved -
    /// the extension methods are functional and not commented out.
    /// </summary>
    public class DomainEntityExtensionsTests : AutoTestFixtureBase
    {
        public DomainEntityExtensionsTests()
        {
            InsertSeedRecords();
        }

        #region GetSubject Tests

        [Fact]
        public void GetSubject_AutomobileInfo_ReturnsCorrectSubject()
        {
            // Arrange
            var automobile = new AutomobileInfo
            {
                Id = 1,
                AuthorId = TestDefaultAuthor.Id,
                Maker = "Toyota",
                Brand = "Camry"
            };

            // Act
            var subject = automobile.GetSubject();

            // Assert
            Assert.Equal(AutomobileConstant.AutoMobileRecordSubject, subject);
        }

        [Fact]
        public void GetSubject_GasDiscount_ReturnsCorrectSubject()
        {
            // Arrange
            var discount = new GasDiscount
            {
                Id = 1,
                AuthorId = TestDefaultAuthor.Id,
                Program = "Petro-Points",
                Amount = new Money(0.10m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var subject = discount.GetSubject();

            // Assert
            Assert.Equal(AutomobileConstant.GasDiscountRecordSubject, subject);
        }

        [Fact]
        public void GetSubject_GasLog_ReturnsCorrectSubject()
        {
            // Arrange
            var gasLog = new GasLog
            {
                Id = 1,
                AuthorId = TestDefaultAuthor.Id,
                Date = DateTime.UtcNow,
                AutomobileId = 5,
                Distance = Dimension.FromKilometer(350),
                Odometer = Dimension.FromKilometer(50000),
                Fuel = Volume.FromLiter(45.5),
                TotalPrice = new Money(85.00m, CurrencyCodeType.Cad)
            };

            // Act
            var subject = gasLog.GetSubject();

            // Assert
            // Note: GasLog.GetSubject() from extension returns the constant, not the dynamic subject
            Assert.Equal(AutomobileConstant.GasLogRecordSubject, subject);
        }

        [Fact]
        public void GetSubject_GasStation_ReturnsCorrectSubject()
        {
            // Arrange
            var station = new GasStation
            {
                Id = 1,
                AuthorId = TestDefaultAuthor.Id,
                Name = "Costco Gas",
                Address = "123 Main St",
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var subject = station.GetSubject();

            // Assert
            Assert.Equal(AutomobileConstant.GasStationRecordSubject, subject);
        }

        #endregion

        #region GetCatalogIdAsync Tests

        [Fact]
        public async Task GetCatalogIdAsync_AutomobileInfo_ReturnsCatalogId()
        {
            // Arrange
            var automobile = new AutomobileInfo
            {
                Id = 1,
                AuthorId = TestDefaultAuthor.Id,
                Maker = "Toyota"
            };

            // Act
            var catalogId = await automobile.GetCatalogIdAsync(LookupRepository);

            // Assert
            Assert.Equal(200, catalogId); // AutomobileInfo catalog ID from AutoTestFixtureBase
        }

        [Fact]
        public async Task GetCatalogIdAsync_GasLog_ReturnsCatalogId()
        {
            // Arrange
            var gasLog = new GasLog
            {
                Id = 1,
                AuthorId = TestDefaultAuthor.Id,
                Date = DateTime.UtcNow
            };

            // Act
            var catalogId = await gasLog.GetCatalogIdAsync(LookupRepository);

            // Assert
            Assert.Equal(201, catalogId); // GasLog catalog ID from AutoTestFixtureBase
        }

        [Fact]
        public async Task GetCatalogIdAsync_GasDiscount_ReturnsCatalogId()
        {
            // Arrange
            var discount = new GasDiscount
            {
                Id = 1,
                AuthorId = TestDefaultAuthor.Id,
                Program = "Test Discount"
            };

            // Act
            var catalogId = await discount.GetCatalogIdAsync(LookupRepository);

            // Assert
            Assert.Equal(202, catalogId); // GasDiscount catalog ID from AutoTestFixtureBase
        }

        [Fact]
        public async Task GetCatalogIdAsync_GasStation_ReturnsCatalogId()
        {
            // Arrange
            var station = new GasStation
            {
                Id = 1,
                AuthorId = TestDefaultAuthor.Id,
                Name = "Test Station",
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var catalogId = await station.GetCatalogIdAsync(LookupRepository);

            // Assert
            Assert.Equal(203, catalogId); // GasStation catalog ID from AutoTestFixtureBase
        }

        [Fact]
        public async Task GetCatalogIdAsync_NullLookup_ThrowsArgumentNullException()
        {
            // Arrange
            var automobile = new AutomobileInfo { Id = 1 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                automobile.GetCatalogIdAsync(null));
        }

        [Fact]
        public async Task GetCatalogIdAsync_CatalogNotFound_ReturnsZero()
        {
            // Arrange
            var automobile = new AutomobileInfo { Id = 1 };
            var mockLookup = new Mock<IEntityLookup>();

            // Setup to return empty list (catalog not found)
            mockLookup.Setup(lk => lk.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(
                    PageList<NoteCatalog>.Create(
                        new System.Collections.Generic.List<NoteCatalog>().AsQueryable(), 1, 20)));

            // Act
            var catalogId = await automobile.GetCatalogIdAsync(mockLookup.Object);

            // Assert
            Assert.Equal(0, catalogId);
        }

        [Fact]
        public async Task GetCatalogIdAsync_LookupFails_ReturnsZero()
        {
            // Arrange
            var automobile = new AutomobileInfo { Id = 1 };
            var mockLookup = new Mock<IEntityLookup>();

            // Setup to return failure
            mockLookup.Setup(lk => lk.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Fail("Database error"));

            // Act
            var catalogId = await automobile.GetCatalogIdAsync(mockLookup.Object);

            // Assert
            Assert.Equal(0, catalogId);
        }

        #endregion
    }
}
