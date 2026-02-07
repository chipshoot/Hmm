using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
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
        private readonly Mock<IOptions<AutomobileSeedingOptions>> _optionsMock;
        private readonly Mock<ISeedingService> _seedingServiceMock;
        private readonly IDefaultAuthorProvider _authorProvider;
        private readonly Mock<ILogger<ApplicationRegister>> _loggerMock;

        public ApplicationRegisterTests()
        {
            InsertSeedRecords();

            _optionsMock = new Mock<IOptions<AutomobileSeedingOptions>>();
            _optionsMock.Setup(o => o.Value).Returns(new AutomobileSeedingOptions
            {
                AddSeedingEntity = false,
                SeedingDataFile = null
            });

            _seedingServiceMock = new Mock<ISeedingService>();
            _authorProvider = CreateMockDefaultAuthorProvider();
            _loggerMock = new Mock<ILogger<ApplicationRegister>>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationRegister(null, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullSeedingService_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationRegister(_optionsMock.Object, null, _authorProvider, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullAuthorProvider_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ApplicationRegister(_optionsMock.Object, _seedingServiceMock.Object, null, _loggerMock.Object));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange & Act
            var register = new ApplicationRegister(_optionsMock.Object, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object);

            // Assert
            Assert.NotNull(register);
        }

        [Fact]
        public void Constructor_WithNullLogger_CreatesInstance()
        {
            // Arrange & Act
            var register = new ApplicationRegister(_optionsMock.Object, _seedingServiceMock.Object, _authorProvider);

            // Assert
            Assert.NotNull(register);
        }

        [Fact]
        public void Constructor_WithNullOptionsValue_UsesDefaultOptions()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AutomobileSeedingOptions>>();
            optionsMock.Setup(o => o.Value).Returns((AutomobileSeedingOptions)null);

            // Act
            var register = new ApplicationRegister(optionsMock.Object, _seedingServiceMock.Object, _authorProvider);

            // Assert
            Assert.NotNull(register);
        }

        #endregion

        #region AuthorProvider Tests

        [Fact]
        public void AuthorProvider_ReturnsInjectedProvider()
        {
            // Arrange
            var register = new ApplicationRegister(_optionsMock.Object, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object);

            // Act
            var provider = register.AuthorProvider;

            // Assert
            Assert.NotNull(provider);
            Assert.Same(_authorProvider, provider);
        }

        [Fact]
        public async Task AuthorProvider_GetDefaultAuthorAsync_ReturnsAuthor()
        {
            // Arrange
            var register = new ApplicationRegister(_optionsMock.Object, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object);

            // Act
            var result = await register.AuthorProvider.GetDefaultAuthorAsync();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(TestDefaultAuthor.AccountName, result.Value.AccountName);
        }

        #endregion

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_WithNullLookupRepo_ThrowsArgumentNullException()
        {
            // Arrange
            var register = new ApplicationRegister(_optionsMock.Object, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => register.RegisterAsync(null));
        }

        [Fact]
        public async Task RegisterAsync_WhenSeedingDisabled_ReturnsSuccessWithoutCallingSeedingService()
        {
            // Arrange
            var optionsMock = CreateOptionsMock(addSeedingEntity: false, seedingDataFile: null);
            var register = new ApplicationRegister(optionsMock.Object, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object);

            // Act
            var result = await register.RegisterAsync(LookupRepository);

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
            var optionsMock = CreateOptionsMock(addSeedingEntity: true, seedingDataFile: "");
            var register = new ApplicationRegister(optionsMock.Object, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object);

            // Act
            var result = await register.RegisterAsync(LookupRepository);

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
            var optionsMock = CreateOptionsMock(addSeedingEntity: true, seedingDataFile: dataFile);

            _seedingServiceMock
                .Setup(x => x.SeedDataAsync(dataFile))
                .ReturnsAsync(ProcessingResult<int>.Ok(5, "Successfully seeded 5 entities"));

            var register = new ApplicationRegister(optionsMock.Object, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object);

            // Act
            var result = await register.RegisterAsync(LookupRepository);

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
            var optionsMock = CreateOptionsMock(addSeedingEntity: true, seedingDataFile: dataFile);

            _seedingServiceMock
                .Setup(x => x.SeedDataAsync(dataFile))
                .ReturnsAsync(ProcessingResult<int>.NotFound("Seeding data file not found"));

            var register = new ApplicationRegister(optionsMock.Object, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object);

            // Act
            var result = await register.RegisterAsync(LookupRepository);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Seeding failed", result.ErrorMessage);
        }

        [Fact]
        public async Task RegisterAsync_WhenSeedingSucceedsWithWarnings_PreservesWarnings()
        {
            // Arrange
            var dataFile = "test-data.json";
            var optionsMock = CreateOptionsMock(addSeedingEntity: true, seedingDataFile: dataFile);

            var seedResult = ProcessingResult<int>.Ok(3, "Seeded 3 entities")
                .WithWarning("Failed to create automobile 'TestCar': Validation error");

            _seedingServiceMock
                .Setup(x => x.SeedDataAsync(dataFile))
                .ReturnsAsync(seedResult);

            var register = new ApplicationRegister(optionsMock.Object, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object);

            // Act
            var result = await register.RegisterAsync(LookupRepository);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.HasWarning);
            Assert.Contains(result.Messages, m => m.Message.Contains("Failed to create automobile"));
        }

        [Fact]
        public async Task RegisterAsync_WhenExceptionThrown_ReturnsFailureFromException()
        {
            // Arrange
            var dataFile = "test-data.json";
            var optionsMock = CreateOptionsMock(addSeedingEntity: true, seedingDataFile: dataFile);

            _seedingServiceMock
                .Setup(x => x.SeedDataAsync(dataFile))
                .ThrowsAsync(new InvalidOperationException("Unexpected error"));

            var register = new ApplicationRegister(optionsMock.Object, _seedingServiceMock.Object, _authorProvider, _loggerMock.Object);

            // Act
            var result = await register.RegisterAsync(LookupRepository);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Unexpected error", result.ErrorMessage);
        }

        #endregion

        #region Helper Methods

        private static Mock<IOptions<AutomobileSeedingOptions>> CreateOptionsMock(
            bool addSeedingEntity,
            string seedingDataFile)
        {
            var optionsMock = new Mock<IOptions<AutomobileSeedingOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new AutomobileSeedingOptions
            {
                AddSeedingEntity = addSeedingEntity,
                SeedingDataFile = seedingDataFile
            });
            return optionsMock;
        }

        #endregion
    }
}
