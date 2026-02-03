using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    /// <summary>
    /// Tests for AutomobileSeedingService functionality.
    /// </summary>
    public class AutomobileSeedingServiceTests : AutoTestFixtureBase
    {
        private readonly Mock<ILogger<AutomobileSeedingService>> _loggerMock;
        private readonly Mock<IAutoEntityManager<GasDiscount>> _discountManagerMock;
        private readonly Mock<IAutoEntityManager<AutomobileInfo>> _automobileManagerMock;

        public AutomobileSeedingServiceTests()
        {
            InsertSeedRecords();
            _loggerMock = new Mock<ILogger<AutomobileSeedingService>>();
            _discountManagerMock = new Mock<IAutoEntityManager<GasDiscount>>();
            _automobileManagerMock = new Mock<IAutoEntityManager<AutomobileInfo>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullAutomobileManager_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AutomobileSeedingService(null, _discountManagerMock.Object, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullDiscountManager_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AutomobileSeedingService(_automobileManagerMock.Object, null, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AutomobileSeedingService(_automobileManagerMock.Object, _discountManagerMock.Object, null));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            // Assert
            Assert.NotNull(service);
        }

        #endregion Constructor Tests

        #region SeedDataAsync Tests

        [Fact]
        public async Task SeedDataAsync_WithNullFilePath_ReturnsInvalidResult()
        {
            // Arrange
            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            // Act
            var result = await service.SeedDataAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
            Assert.Contains("File path cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task SeedDataAsync_WithEmptyFilePath_ReturnsInvalidResult()
        {
            // Arrange
            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            // Act
            var result = await service.SeedDataAsync("");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
            Assert.Contains("File path cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task SeedDataAsync_WithWhitespaceFilePath_ReturnsInvalidResult()
        {
            // Arrange
            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            // Act
            var result = await service.SeedDataAsync("   ");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task SeedDataAsync_WithNonExistentFile_ReturnsNotFoundResult()
        {
            // Arrange
            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);
            var nonExistentFile = "nonexistent_file_12345.json";

            // Act
            var result = await service.SeedDataAsync(nonExistentFile);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.NotFound, result.ErrorType);
            Assert.Contains("not found", result.ErrorMessage);
        }

        [Fact]
        public async Task SeedDataAsync_WithEmptyJsonFile_ReturnsSuccessWithZeroCount()
        {
            // Arrange
            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);
            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, "");

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(0, result.Value);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SeedDataAsync_WithNullJsonRoot_ReturnsFailureResult()
        {
            // Arrange
            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);
            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, "null");

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.False(result.Success);
                Assert.Equal(ErrorCategory.MappingError, result.ErrorType);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SeedDataAsync_WithInvalidJson_ReturnsFailureResult()
        {
            // Arrange
            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);
            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, "{ invalid json }");

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.False(result.Success);
                Assert.Equal(ErrorCategory.MappingError, result.ErrorType);
                Assert.Contains("JSON", result.ErrorMessage);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SeedDataAsync_WithValidAutomobileData_SeedsSuccessfully()
        {
            // Arrange
            _automobileManagerMock.Setup(m => m.CreateAsync(It.IsAny<AutomobileInfo>(), It.IsAny<bool>()))
                .ReturnsAsync((AutomobileInfo auto, bool _) => ProcessingResult<AutomobileInfo>.Ok(auto));

            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            var testJsonContent = @"{
                ""AutomobileInfos"": [
                    {
                        ""VIN"": ""1HGBH41JXMN109186"",
                        ""Maker"": ""Honda"",
                        ""Brand"": ""Civic"",
                        ""Model"": ""EX"",
                        ""Year"": 2021,
                        ""Color"": ""Blue"",
                        ""Plate"": ""TEST123"",
                        ""MeterReading"": 1000,
                        ""AuthorId"": 100
                    }
                ]
            }";

            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, testJsonContent);

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(1, result.Value);
                Assert.NotEmpty(result.Messages);
                Assert.Contains("Successfully seeded 1", result.Messages.First().Message);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SeedDataAsync_WithValidDiscountData_SeedsSuccessfully()
        {
            // Arrange
            _discountManagerMock.Setup(m => m.CreateAsync(It.IsAny<GasDiscount>(), It.IsAny<bool>()))
                .ReturnsAsync((GasDiscount discount, bool _) => ProcessingResult<GasDiscount>.Ok(discount));

            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            var testJsonContent = @"{
                ""GasDiscounts"": [
                    {
                        ""Program"": ""Test Discount Program"",
                        ""Amount"": { ""value"": 0.05, ""currencyCode"": ""CAD"" },
                        ""Comment"": ""Test discount"",
                        ""IsActive"": true,
                        ""AuthorId"": 100
                    }
                ]
            }";

            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, testJsonContent);

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(1, result.Value);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SeedDataAsync_WithMixedData_SeedsBothTypes()
        {
            // Arrange
            _automobileManagerMock.Setup(m => m.CreateAsync(It.IsAny<AutomobileInfo>(), It.IsAny<bool>()))
                .ReturnsAsync((AutomobileInfo auto, bool _) => ProcessingResult<AutomobileInfo>.Ok(auto));
            _discountManagerMock.Setup(m => m.CreateAsync(It.IsAny<GasDiscount>(), It.IsAny<bool>()))
                .ReturnsAsync((GasDiscount discount, bool _) => ProcessingResult<GasDiscount>.Ok(discount));

            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            var testJsonContent = @"{
                ""AutomobileInfos"": [
                    {
                        ""VIN"": ""1HGBH41JXMN109186"",
                        ""Maker"": ""Honda"",
                        ""Brand"": ""Civic"",
                        ""Model"": ""EX"",
                        ""Year"": 2021,
                        ""Plate"": ""ABC123"",
                        ""MeterReading"": 1000,
                        ""AuthorId"": 100
                    }
                ],
                ""GasDiscounts"": [
                    {
                        ""Program"": ""Test Discount"",
                        ""Amount"": { ""value"": 0.05, ""currencyCode"": ""CAD"" },
                        ""IsActive"": true,
                        ""AuthorId"": 100
                    }
                ]
            }";

            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, testJsonContent);

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(2, result.Value);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SeedDataAsync_WithMultipleAutomobiles_SeedsAll()
        {
            // Arrange
            _automobileManagerMock.Setup(m => m.CreateAsync(It.IsAny<AutomobileInfo>(), It.IsAny<bool>()))
                .ReturnsAsync((AutomobileInfo auto, bool _) => ProcessingResult<AutomobileInfo>.Ok(auto));

            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            var testJsonContent = @"{
                ""AutomobileInfos"": [
                    {
                        ""VIN"": ""VIN001"",
                        ""Maker"": ""Honda"",
                        ""Brand"": ""Civic"",
                        ""Year"": 2021,
                        ""Plate"": ""PLATE1"",
                        ""MeterReading"": 1000,
                        ""AuthorId"": 100
                    },
                    {
                        ""VIN"": ""VIN002"",
                        ""Maker"": ""Toyota"",
                        ""Brand"": ""Camry"",
                        ""Year"": 2022,
                        ""Plate"": ""PLATE2"",
                        ""MeterReading"": 2000,
                        ""AuthorId"": 100
                    }
                ]
            }";

            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, testJsonContent);

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(2, result.Value);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SeedDataAsync_WithPartialFailures_ReturnsSuccessWithWarnings()
        {
            // Arrange
            // Setup mock to succeed for valid VIN and fail for empty VIN
            _automobileManagerMock.Setup(m => m.CreateAsync(It.Is<AutomobileInfo>(a => !string.IsNullOrEmpty(a.VIN)), It.IsAny<bool>()))
                .ReturnsAsync((AutomobileInfo auto, bool _) => ProcessingResult<AutomobileInfo>.Ok(auto));
            _automobileManagerMock.Setup(m => m.CreateAsync(It.Is<AutomobileInfo>(a => string.IsNullOrEmpty(a.VIN)), It.IsAny<bool>()))
                .ReturnsAsync(ProcessingResult<AutomobileInfo>.Invalid("VIN is required"));

            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            // Create JSON with one valid and one invalid automobile (missing required field)
            var testJsonContent = @"{
                ""AutomobileInfos"": [
                    {
                        ""VIN"": ""VALIDVIN001"",
                        ""Maker"": ""Honda"",
                        ""Brand"": ""Civic"",
                        ""Year"": 2021,
                        ""Plate"": ""VALID1"",
                        ""MeterReading"": 1000,
                        ""AuthorId"": 100
                    },
                    {
                        ""VIN"": """",
                        ""Maker"": ""Toyota"",
                        ""Brand"": ""Camry"",
                        ""Year"": 2022,
                        ""Plate"": ""INVALID"",
                        ""MeterReading"": -100,
                        ""AuthorId"": 100
                    }
                ]
            }";

            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, testJsonContent);

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(1, result.Value); // Only valid automobile should be seeded
                Assert.True(result.HasWarning); // Should have warnings for the failure
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SeedDataAsync_WithEmptyArrays_ReturnsSuccessWithZeroCount()
        {
            // Arrange
            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            var testJsonContent = @"{
                ""AutomobileInfos"": [],
                ""GasDiscounts"": []
            }";

            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, testJsonContent);

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(0, result.Value);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SeedDataAsync_WithJsonComments_ParsesSuccessfully()
        {
            // Arrange
            _automobileManagerMock.Setup(m => m.CreateAsync(It.IsAny<AutomobileInfo>(), It.IsAny<bool>()))
                .ReturnsAsync((AutomobileInfo auto, bool _) => ProcessingResult<AutomobileInfo>.Ok(auto));

            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            var testJsonContent = @"{
                // This is a comment
                ""AutomobileInfos"": [
                    {
                        ""VIN"": ""TESTVIN001"",
                        ""Maker"": ""Honda"",
                        ""Brand"": ""Civic"",
                        ""Year"": 2021,
                        ""Plate"": ""TEST1"",
                        ""MeterReading"": 1000,
                        ""AuthorId"": 100
                    }
                ]
            }";

            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, testJsonContent);

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(1, result.Value);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task SeedDataAsync_LogsAppropriateMessages()
        {
            // Arrange
            _automobileManagerMock.Setup(m => m.CreateAsync(It.IsAny<AutomobileInfo>(), It.IsAny<bool>()))
                .ReturnsAsync((AutomobileInfo auto, bool _) => ProcessingResult<AutomobileInfo>.Ok(auto));

            var service = new AutomobileSeedingService(
                _automobileManagerMock.Object,
                _discountManagerMock.Object,
                _loggerMock.Object);

            var testJsonContent = @"{
                ""AutomobileInfos"": [
                    {
                        ""VIN"": ""TESTVIN001"",
                        ""Maker"": ""Honda"",
                        ""Brand"": ""Civic"",
                        ""Year"": 2021,
                        ""Plate"": ""LOG1"",
                        ""MeterReading"": 1000,
                        ""AuthorId"": 100
                    }
                ]
            }";

            var tempFile = Path.GetTempFileName();

            try
            {
                await File.WriteAllTextAsync(tempFile, testJsonContent);

                // Act
                var result = await service.SeedDataAsync(tempFile);

                // Assert
                Assert.True(result.Success);

                // Verify logging occurred
                _loggerMock.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Reading seeding data")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                    Times.AtLeastOnce);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        #endregion SeedDataAsync Tests
    }
}