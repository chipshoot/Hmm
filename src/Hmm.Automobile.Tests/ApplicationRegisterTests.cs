using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    /// <summary>
    /// Tests for ApplicationRegister functionality.
    /// Seeding-specific tests have been moved to AutomobileSeedingServiceTests.
    /// </summary>
    public class ApplicationRegisterTests : AutoTestFixtureBase
    {
        private readonly IConfiguration _configuration;
        private readonly Mock<ISeedingService> _seedingServiceMock;
        private readonly Mock<ILogger<ApplicationRegister>> _loggerMock;

        public ApplicationRegisterTests()
        {
            InsertSeedRecords();
            
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Automobile:Seeding:AddSeedingEntity"] = "false"
                });
            _configuration = configBuilder.Build();

            _seedingServiceMock = new Mock<ISeedingService>();
            _loggerMock = new Mock<ILogger<ApplicationRegister>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationRegister(null, _seedingServiceMock.Object, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullSeedingService_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationRegister(_configuration, null, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var register = new ApplicationRegister(_configuration, _seedingServiceMock.Object, _loggerMock.Object);

            // Assert
            Assert.NotNull(register);
        }

        [Fact]
        public void Constructor_WithNullLogger_CreatesInstance()
        {
            // Arrange & Act
            var register = new ApplicationRegister(_configuration, _seedingServiceMock.Object);

            // Assert
            Assert.NotNull(register);
        }

        #endregion

        #region DefaultAuthor Tests

        [Fact]
        public void DefaultAuthor_ReturnsValidAuthor()
        {
            // Act
            var author = ApplicationRegister.DefaultAuthor;

            // Assert
            Assert.NotNull(author);
            Assert.Equal("03D9D3DE-0C3C-4775-BEC3-6B698B696837", author.AccountName);
            Assert.Equal("Automobile default author", author.Description);
            Assert.Equal(AuthorRoleType.Author, author.Role);
            Assert.True(author.IsActivated);
        }

        [Fact]
        public void DefaultAuthor_ReturnsSameInstance()
        {
            // Act
            var author1 = ApplicationRegister.DefaultAuthor;
            var author2 = ApplicationRegister.DefaultAuthor;

            // Assert
            Assert.Same(author1, author2);
        }

        #endregion

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_WithNullLookupRepo_ThrowsArgumentNullException()
        {
            // Arrange
            var register = new ApplicationRegister(_configuration, _seedingServiceMock.Object, _loggerMock.Object);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => register.RegisterAsync(automobileManager, discountManager, null));
        }

        [Fact]
        public async Task RegisterAsync_WhenSeedingDisabled_ReturnsSuccessWithoutCallingSeedingService()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Automobile:Seeding:AddSeedingEntity"] = "false"
                })
                .Build();
            
            var register = new ApplicationRegister(config, _seedingServiceMock.Object, _loggerMock.Object);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act
            var result = await register.RegisterAsync(automobileManager, discountManager, LookupRepository);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Value);
            Assert.Contains("Seeding disabled", result.Messages[0].Message);
            
            // Verify seeding service was not called
            _seedingServiceMock.Verify(
                x => x.SeedDataAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WhenSeedingEnabledButNoDataFile_ReturnsSuccessWithoutCallingSeedingService()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Automobile:Seeding:AddSeedingEntity"] = "true",
                    ["Automobile:Seeding:SeedingDataFile"] = ""
                })
                .Build();
            
            var register = new ApplicationRegister(config, _seedingServiceMock.Object, _loggerMock.Object);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act
            var result = await register.RegisterAsync(automobileManager, discountManager, LookupRepository);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Value);
            Assert.Contains("No seeding data file configured", result.Messages[0].Message);
            
            // Verify seeding service was not called
            _seedingServiceMock.Verify(
                x => x.SeedDataAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WhenSeedingEnabledWithFile_CallsSeedingService()
        {
            // Arrange
            var dataFile = "test-seeding-data.json";
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Automobile:Seeding:AddSeedingEntity"] = "true",
                    ["Automobile:Seeding:SeedingDataFile"] = dataFile
                })
                .Build();

            _seedingServiceMock
                .Setup(x => x.SeedDataAsync(dataFile))
                .ReturnsAsync(ProcessingResult<int>.Ok(5, "Successfully seeded 5 entities"));

            var register = new ApplicationRegister(config, _seedingServiceMock.Object, _loggerMock.Object);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act
            var result = await register.RegisterAsync(automobileManager, discountManager, LookupRepository);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Value);
            Assert.Contains("5 entities seeded", result.Messages[0].Message);
            
            // Verify seeding service was called with correct file path
            _seedingServiceMock.Verify(
                x => x.SeedDataAsync(dataFile),
                Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WhenSeedingFails_ReturnsFailure()
        {
            // Arrange
            var dataFile = "invalid-file.json";
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Automobile:Seeding:AddSeedingEntity"] = "true",
                    ["Automobile:Seeding:SeedingDataFile"] = dataFile
                })
                .Build();

            _seedingServiceMock
                .Setup(x => x.SeedDataAsync(dataFile))
                .ReturnsAsync(ProcessingResult<int>.NotFound("Seeding data file not found"));

            var register = new ApplicationRegister(config, _seedingServiceMock.Object, _loggerMock.Object);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act
            var result = await register.RegisterAsync(automobileManager, discountManager, LookupRepository);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Seeding failed", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WhenSeedingSucceedsWithWarnings_PreservesWarnings()
        {
            // Arrange
            var dataFile = "test-data.json";
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Automobile:Seeding:AddSeedingEntity"] = "true",
                    ["Automobile:Seeding:SeedingDataFile"] = dataFile
                })
                .Build();

            var seedResult = ProcessingResult<int>.Ok(3, "Seeded 3 entities")
                .WithWarning("Failed to create automobile 'TestCar': Validation error");

            _seedingServiceMock
                .Setup(x => x.SeedDataAsync(dataFile))
                .ReturnsAsync(seedResult);

            var register = new ApplicationRegister(config, _seedingServiceMock.Object, _loggerMock.Object);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act
            var result = await register.RegisterAsync(automobileManager, discountManager, LookupRepository);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.HasWarning);
            Assert.Contains(result.Messages, m => m.Message.Contains("Failed to create automobile"));
        }

        #endregion

        #region GetCatalog/GetCatalogAsync Tests

        [Theory]
        [InlineData(NoteCatalogType.Automobile, AutomobileConstant.AutoMobileInfoCatalogName)]
        [InlineData(NoteCatalogType.GasDiscount, AutomobileConstant.GasDiscountCatalogName)]
        [InlineData(NoteCatalogType.GasLog, AutomobileConstant.GasLogCatalogName)]
        [InlineData(NoteCatalogType.GasStation, AutomobileConstant.GasStationCatalogName)]
        public async Task GetCatalogAsync_WithValidType_ReturnsCorrectCatalog(
            NoteCatalogType catalogType,
            string expectedName)
        {
            // Arrange
            var register = new ApplicationRegister(_configuration, _seedingServiceMock.Object, _loggerMock.Object);

            // Act
            var catalog = await register.GetCatalogAsync(catalogType, LookupRepository);

            // Assert
            Assert.NotNull(catalog);
            Assert.Equal(expectedName, catalog.Name);
        }

        [Fact]
        public async Task GetCatalogAsync_WithNullLookupRepo_ThrowsArgumentNullException()
        {
            // Arrange
            var register = new ApplicationRegister(_configuration, _seedingServiceMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => register.GetCatalogAsync(NoteCatalogType.Automobile, null));
        }

        [Fact]
        public async Task GetCatalogAsync_CachesCatalog()
        {
            // Arrange
            var register = new ApplicationRegister(_configuration, _seedingServiceMock.Object, _loggerMock.Object);

            // Act
            var catalog1 = await register.GetCatalogAsync(NoteCatalogType.Automobile, LookupRepository);
            var catalog2 = await register.GetCatalogAsync(NoteCatalogType.Automobile, LookupRepository);

            // Assert
            Assert.NotNull(catalog1);
            Assert.NotNull(catalog2);
            Assert.Equal(catalog1.Name, catalog2.Name);
        }

        [Fact]
        public async Task GetCatalogAsync_WithInvalidType_ReturnsNull()
        {
            // Arrange
            var register = new ApplicationRegister(_configuration, _seedingServiceMock.Object, _loggerMock.Object);

            // Act
            var catalog = await register.GetCatalogAsync((NoteCatalogType)999, LookupRepository);

            // Assert
            Assert.Null(catalog);
        }

        [Theory]
        [InlineData(NoteCatalogType.Automobile, AutomobileConstant.AutoMobileInfoCatalogName)]
        [InlineData(NoteCatalogType.GasDiscount, AutomobileConstant.GasDiscountCatalogName)]
        [InlineData(NoteCatalogType.GasLog, AutomobileConstant.GasLogCatalogName)]
        [InlineData(NoteCatalogType.GasStation, AutomobileConstant.GasStationCatalogName)]
        public void GetCatalog_WithValidType_ReturnsCorrectCatalog(
            NoteCatalogType catalogType,
            string expectedName)
        {
            // Arrange
            var register = new ApplicationRegister(_configuration, _seedingServiceMock.Object, _loggerMock.Object);

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            var catalog = register.GetCatalog(catalogType, LookupRepository);
#pragma warning restore CS0618

            // Assert
            Assert.NotNull(catalog);
            Assert.Equal(expectedName, catalog.Name);
        }

        [Fact]
        public void GetCatalog_WithInvalidType_ReturnsNull()
        {
            // Arrange
            var register = new ApplicationRegister(_configuration, _seedingServiceMock.Object, _loggerMock.Object);

            // Act
#pragma warning disable CS0618 // Type or member is obsolete
            var catalog = register.GetCatalog((NoteCatalogType)999, LookupRepository);
#pragma warning restore CS0618

            // Assert
            Assert.Null(catalog);
        }

        [Fact]
        public void GetCatalog_IsMarkedAsObsolete()
        {
            // This test verifies that GetCatalog method is marked with Obsolete attribute
            var method = typeof(ApplicationRegister).GetMethod("GetCatalog");
            var obsoleteAttribute = method.GetCustomAttributes(typeof(ObsoleteAttribute), false);

            Assert.NotEmpty(obsoleteAttribute);
        }

        #endregion
    }
}
