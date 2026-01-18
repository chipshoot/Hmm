using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class ApplicationRegisterTests : AutoTestFixtureBase
    {
        private readonly IConfiguration _configuration;

        public ApplicationRegisterTests()
        {
            InsertSeedRecords();
            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Automobile:Seeding:AddSeedingEntity"] = "false"
                });
            _configuration = configBuilder.Build();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => new ApplicationRegister(null));
        }

        [Fact]
        public void Constructor_WithValidConfiguration_CreatesInstance()
        {
            // Arrange & Act
            var register = new ApplicationRegister(_configuration);

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
        public async Task RegisterAsync_WithNullAutomobileManager_ThrowsArgumentNullException()
        {
            // Arrange
            var register = new ApplicationRegister(_configuration);
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act & Assert
            await Assert.ThrowsAsync<System.ArgumentNullException>(
                () => register.RegisterAsync(null, discountManager, LookupRepository));
        }

        [Fact]
        public async Task RegisterAsync_WithNullDiscountManager_ThrowsArgumentNullException()
        {
            // Arrange
            var register = new ApplicationRegister(_configuration);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;

            // Act & Assert
            await Assert.ThrowsAsync<System.ArgumentNullException>(
                () => register.RegisterAsync(automobileManager, null, LookupRepository));
        }

        [Fact]
        public async Task RegisterAsync_WithNullLookupRepo_ThrowsArgumentNullException()
        {
            // Arrange
            var register = new ApplicationRegister(_configuration);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act & Assert
            await Assert.ThrowsAsync<System.ArgumentNullException>(
                () => register.RegisterAsync(automobileManager, discountManager, null));
        }

        [Fact]
        public async Task RegisterAsync_WhenSeedingDisabled_ReturnsSuccess()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Automobile:Seeding:AddSeedingEntity"] = "false"
                })
                .Build();
            var register = new ApplicationRegister(config);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act
            var result = await register.RegisterAsync(automobileManager, discountManager, LookupRepository);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Value);
        }

        [Fact]
        public async Task RegisterAsync_WhenSeedingEnabledButNoDataFile_ReturnsSuccess()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Automobile:Seeding:AddSeedingEntity"] = "true",
                    ["Automobile:Seeding:SeedingDataFile"] = ""
                })
                .Build();
            var register = new ApplicationRegister(config);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act
            var result = await register.RegisterAsync(automobileManager, discountManager, LookupRepository);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Value);
        }

        [Fact]
        public async Task RegisterAsync_WhenSeedingEnabledWithNonExistentFile_ReturnsSuccess()
        {
            // Arrange
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Automobile:Seeding:AddSeedingEntity"] = "true",
                    ["Automobile:Seeding:SeedingDataFile"] = "nonexistent.json"
                })
                .Build();
            var register = new ApplicationRegister(config);
            var automobileManager = new Mock<IAutoEntityManager<AutomobileInfo>>().Object;
            var discountManager = new Mock<IAutoEntityManager<GasDiscount>>().Object;

            // Act
            var result = await register.RegisterAsync(automobileManager, discountManager, LookupRepository);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Value);
        }

        #endregion

        #region GetSeedingEntities Tests

        [Fact]
        public void GetSeedingEntities_WithNullFileName_ReturnsEmptyList()
        {
            // Act
            var entities = ApplicationRegister.GetSeedingEntities(null);

            // Assert
            Assert.NotNull(entities);
            Assert.Empty(entities);
        }

        [Fact]
        public void GetSeedingEntities_WithEmptyFileName_ReturnsEmptyList()
        {
            // Act
            var entities = ApplicationRegister.GetSeedingEntities("");

            // Assert
            Assert.NotNull(entities);
            Assert.Empty(entities);
        }

        [Fact]
        public void GetSeedingEntities_WithNonExistentFile_ReturnsEmptyList()
        {
            // Act
            var entities = ApplicationRegister.GetSeedingEntities("nonexistent_file.json");

            // Assert
            Assert.NotNull(entities);
            Assert.Empty(entities);
        }

        [Fact]
        public void GetSeedingEntities_WithValidJsonFile_ReturnsEntities()
        {
            // Arrange
            var testJsonContent = @"{
                ""AutomobileInfos"": [
                    {
                        ""VIN"": ""1HGBH41JXMN109186"",
                        ""Maker"": ""Honda"",
                        ""Brand"": ""Civic"",
                        ""Model"": ""EX"",
                        ""Year"": 2021,
                        ""Color"": ""Blue"",
                        ""Plate"": ""ABC123"",
                        ""EngineType"": 0,
                        ""FuelType"": 0
                    }
                ],
                ""GasDiscounts"": [
                    {
                        ""Program"": ""Test Discount"",
                        ""Amount"": { ""value"": 0.05, ""currencyCode"": ""CAD"" }
                    }
                ]
            }";
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, testJsonContent);

                // Act
                var entities = ApplicationRegister.GetSeedingEntities(tempFile);

                // Assert
                Assert.NotNull(entities);
                var entityList = entities.ToList();
                Assert.Equal(2, entityList.Count);
                Assert.Single(entityList.OfType<AutomobileInfo>());
                Assert.Single(entityList.OfType<GasDiscount>());
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetSeedingEntities_WithEmptyJsonRoot_ReturnsEmptyList()
        {
            // Arrange
            var testJsonContent = "null";
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, testJsonContent);

                // Act
                var entities = ApplicationRegister.GetSeedingEntities(tempFile);

                // Assert
                Assert.NotNull(entities);
                Assert.Empty(entities);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetSeedingEntities_WithOnlyAutomobiles_ReturnsAutomobiles()
        {
            // Arrange
            var testJsonContent = @"{
                ""AutomobileInfos"": [
                    {
                        ""VIN"": ""1HGBH41JXMN109186"",
                        ""Maker"": ""Honda"",
                        ""Brand"": ""Civic"",
                        ""Model"": ""EX"",
                        ""Year"": 2021,
                        ""Plate"": ""ABC123"",
                        ""EngineType"": 0,
                        ""FuelType"": 0
                    }
                ]
            }";
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, testJsonContent);

                // Act
                var entities = ApplicationRegister.GetSeedingEntities(tempFile);

                // Assert
                Assert.NotNull(entities);
                var entityList = entities.ToList();
                Assert.Single(entityList);
                Assert.IsType<AutomobileInfo>(entityList.First());
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetSeedingEntities_WithOnlyDiscounts_ReturnsDiscounts()
        {
            // Arrange
            var testJsonContent = @"{
                ""GasDiscounts"": [
                    {
                        ""Program"": ""Test Discount"",
                        ""Amount"": { ""value"": 0.05, ""currencyCode"": ""CAD"" }
                    }
                ]
            }";
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, testJsonContent);

                // Act
                var entities = ApplicationRegister.GetSeedingEntities(tempFile);

                // Assert
                Assert.NotNull(entities);
                var entityList = entities.ToList();
                Assert.Single(entityList);
                Assert.IsType<GasDiscount>(entityList.First());
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion

        #region GetCatalog/GetCatalogAsync Tests

        [Theory]
        [InlineData(NoteCatalogType.Automobile, AutomobileConstant.AutoMobileInfoCatalogName)]
        [InlineData(NoteCatalogType.GasDiscount, AutomobileConstant.GasDiscountCatalogName)]
        [InlineData(NoteCatalogType.GasLog, AutomobileConstant.GasLogCatalogName)]
        [InlineData(NoteCatalogType.GasStation, AutomobileConstant.GasStationCatalogName)]
        public async Task GetCatalogAsync_WithValidType_ReturnsCorrectCatalog(NoteCatalogType catalogType, string expectedName)
        {
            // Arrange
            var register = new ApplicationRegister(_configuration);

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
            var register = new ApplicationRegister(_configuration);

            // Act & Assert
            await Assert.ThrowsAsync<System.ArgumentNullException>(
                () => register.GetCatalogAsync(NoteCatalogType.Automobile, null));
        }

        [Fact]
        public async Task GetCatalogAsync_CachesCatalog()
        {
            // Arrange
            var register = new ApplicationRegister(_configuration);

            // Act
            var catalog1 = await register.GetCatalogAsync(NoteCatalogType.Automobile, LookupRepository);
            var catalog2 = await register.GetCatalogAsync(NoteCatalogType.Automobile, LookupRepository);

            // Assert
            Assert.NotNull(catalog1);
            Assert.NotNull(catalog2);
            Assert.Equal(catalog1.Name, catalog2.Name);
        }

        [Theory]
        [InlineData(NoteCatalogType.Automobile, AutomobileConstant.AutoMobileInfoCatalogName)]
        [InlineData(NoteCatalogType.GasDiscount, AutomobileConstant.GasDiscountCatalogName)]
        [InlineData(NoteCatalogType.GasLog, AutomobileConstant.GasLogCatalogName)]
        [InlineData(NoteCatalogType.GasStation, AutomobileConstant.GasStationCatalogName)]
        public void GetCatalog_WithValidType_ReturnsCorrectCatalog(NoteCatalogType catalogType, string expectedName)
        {
            // Arrange
            var register = new ApplicationRegister(_configuration);

            // Act
            var catalog = register.GetCatalog(catalogType, LookupRepository);

            // Assert
            Assert.NotNull(catalog);
            Assert.Equal(expectedName, catalog.Name);
        }

        [Fact]
        public void GetCatalog_WithInvalidType_ReturnsNull()
        {
            // Arrange
            var register = new ApplicationRegister(_configuration);

            // Act
            var catalog = register.GetCatalog((NoteCatalogType)999, LookupRepository);

            // Assert
            Assert.Null(catalog);
        }

        #endregion
    }
}
