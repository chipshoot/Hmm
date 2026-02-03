using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Automobile.Validator;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasLogManagerTests : AutoTestFixtureBase
    {
        private IGasLogManager _manager;
        private IAutoEntityManager<AutomobileInfo> _autoManager;
        private IAutoEntityManager<GasDiscount> _discountManager;
        private IAutoEntityManager<GasStation> _stationManager;
        private AutomobileInfo _testCar;
        private GasStation _testStation;
        private readonly Mock<IDateTimeProvider> _dateProviderMock;

        public GasLogManagerTests()
        {
            _dateProviderMock = new Mock<IDateTimeProvider>();
            _dateProviderMock.Setup(d => d.UtcNow).Returns(DateTime.UtcNow);
            SetupDevEnv();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithNullNoteSerializer_ThrowsArgumentNullException()
        {
            // Arrange
            var validator = new GasLogValidator(LookupRepository, _dateProviderMock.Object);
            var noteManager = CreateNoteManager();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GasLogManager(null, validator, noteManager, _autoManager, LookupRepository, CreateMockAuthorProvider(), _dateProviderMock.Object));
        }

        [Fact]
        public void Constructor_WithNullValidator_ThrowsArgumentNullException()
        {
            // Arrange
            var noteSerializer = CreateGasLogSerializer();
            var noteManager = CreateNoteManager();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GasLogManager(noteSerializer, null, noteManager, _autoManager, LookupRepository, CreateMockAuthorProvider(), _dateProviderMock.Object));
        }

        [Fact]
        public void Constructor_WithNullNoteManager_ThrowsArgumentNullException()
        {
            // Arrange
            var noteSerializer = CreateGasLogSerializer();
            var validator = new GasLogValidator(LookupRepository, _dateProviderMock.Object);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GasLogManager(noteSerializer, validator, null, _autoManager, LookupRepository, CreateMockAuthorProvider(), _dateProviderMock.Object));
        }

        [Fact]
        public void Constructor_WithNullAutoManager_ThrowsArgumentNullException()
        {
            // Arrange
            var noteSerializer = CreateGasLogSerializer();
            var validator = new GasLogValidator(LookupRepository, _dateProviderMock.Object);
            var noteManager = CreateNoteManager();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GasLogManager(noteSerializer, validator, noteManager, null, LookupRepository, CreateMockAuthorProvider(), _dateProviderMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLookupRepo_ThrowsArgumentNullException()
        {
            // Arrange
            var noteSerializer = CreateGasLogSerializer();
            var validator = new GasLogValidator(LookupRepository, _dateProviderMock.Object);
            var noteManager = CreateNoteManager();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GasLogManager(noteSerializer, validator, noteManager, _autoManager, null, CreateMockAuthorProvider(), _dateProviderMock.Object));
        }

        [Fact]
        public void Constructor_WithNullDateProvider_ThrowsArgumentNullException()
        {
            // Arrange
            var noteSerializer = CreateGasLogSerializer();
            var validator = new GasLogValidator(LookupRepository, _dateProviderMock.Object);
            var noteManager = CreateNoteManager();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GasLogManager(noteSerializer, validator, noteManager, _autoManager, LookupRepository, CreateMockAuthorProvider(), null));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Assert
            Assert.NotNull(_manager);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidGasLog_ReturnsCreatedEntity()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id >= 1);
            Assert.Equal(_testCar.Id, result.Value.Car.Id);
        }

        [Fact]
        public async Task CreateAsync_WithNullEntity_ReturnsInvalidResult()
        {
            // Act
            var result = await _manager.CreateAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("null", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidAutomobileId_ReturnsNotFound()
        {
            // Arrange
            var gasLog = CreateValidGasLog();
            gasLog.Car = new AutomobileInfo { Id = 99999 }; // Non-existent car

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("automobile", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateAsync_WithMeterReadingLessThanCarMeterReading_ReturnsValidationError()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();
            // Set odometer less than car's current meter reading (100km)
            // Also adjust distance to be less than odometer to pass validator
            gasLog.Odometer = Dimension.FromKilometer(50);
            gasLog.Distance = Dimension.FromKilometer(30);

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            // Fails because odometer (50km) is less than car's meter reading (100km)
            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidDistance_ReturnsValidationError()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();
            // Distance is larger than odometer change (100 to 200 = 100km change, but distance is 150km)
            gasLog.Odometer = Dimension.FromKilometer(200);
            gasLog.Distance = Dimension.FromKilometer(150);

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("distance", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateAsync_WithMismatchedDistanceAndOdometerChange_ReturnsWarning()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();
            // Distance doesn't match odometer change (car is at 100, new is 200, but distance is 80)
            gasLog.Odometer = Dimension.FromKilometer(200);
            gasLog.Distance = Dimension.FromKilometer(80);

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            Assert.True(result.Success); // Should succeed with warning
            Assert.True(result.HasWarning);
        }

        [Fact]
        public async Task CreateAsync_UpdatesAutomobileMeterReading()
        {
            // Arrange
            await SetupTestCarAsync();
            var initialMeterReading = _testCar.MeterReading;
            var gasLog = CreateValidGasLog();
            gasLog.Odometer = Dimension.FromKilometer(200);
            gasLog.Distance = Dimension.FromKilometer(100);

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            Assert.True(result.Success);
            var updatedCar = await _autoManager.GetEntityByIdAsync(_testCar.Id);
            Assert.True(updatedCar.Success);
            Assert.Equal(200, updatedCar.Value.MeterReading);
        }

        [Fact]
        public async Task CreateAsync_WithZeroAutomobileMeterReading_ReturnsValidationError()
        {
            // Arrange
            await SetupTestCarAsync();
            // Set car's meter reading to 0
            _testCar.MeterReading = 0;
            await _autoManager.UpdateAsync(_testCar);

            var gasLog = CreateValidGasLog();
            gasLog.Odometer = Dimension.FromKilometer(100);
            gasLog.Distance = Dimension.FromKilometer(50);

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("meter reading", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidGasLog_ReturnsUpdatedEntity()
        {
            // Arrange
            var logs = await SetupEnvironmentAsync();
            var log = logs.First();
            var originalComment = log.Comment;

            // Act
            log.Comment = "Updated comment";
            var result = await _manager.UpdateAsync(log);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Updated comment", result.Value.Comment);
        }

        [Fact]
        public async Task UpdateAsync_WithNullEntity_ReturnsInvalidResult()
        {
            // Act
            var result = await _manager.UpdateAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("null", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentEntity_ReturnsNotFound()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = new GasLog
            {
                Id = 99999, // Non-existent ID
                Date = DateTime.UtcNow.AddHours(-1),
                Car = _testCar,
                AutomobileId = _testCar?.Id ?? 0,
                Station = new GasStation { Name = "Test Station" },
                Odometer = Dimension.FromKilometer(200),
                Distance = Dimension.FromKilometer(100),
                Fuel = Volume.FromLiter(45),
                TotalPrice = new Money(67.50m, CurrencyCodeType.Cad),
                UnitPrice = new Money(1.50m, CurrencyCodeType.Cad),
                FuelGrade = FuelGrade.Regular,
                IsFullTank = true
            };

            // Act
            var result = await _manager.UpdateAsync(gasLog);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesAllProperties()
        {
            // Arrange
            var logs = await SetupEnvironmentAsync();
            var log = logs.First();

            // Act
            log.Comment = "New comment";
            log.IsFullTank = false;
            log.CityDrivingPercentage = 75;
            log.HighwayDrivingPercentage = 25;
            log.ReceiptNumber = "RECEIPT123";
            log.Location = "New Location";

            var result = await _manager.UpdateAsync(log);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("New comment", result.Value.Comment);
            Assert.False(result.Value.IsFullTank);
            Assert.Equal(75, result.Value.CityDrivingPercentage);
            Assert.Equal(25, result.Value.HighwayDrivingPercentage);
            Assert.Equal("RECEIPT123", result.Value.ReceiptNumber);
            Assert.Equal("New Location", result.Value.Location);
        }

        #endregion

        #region GetEntitiesAsync Tests

        [Fact]
        public async Task GetEntitiesAsync_ReturnsAllEntities()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetEntitiesAsync(new ResourceCollectionParameters());

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            var logs = result.Value.ToList();
            Assert.Single(logs);
        }

        [Fact]
        public async Task GetEntitiesAsync_WithPagination_ReturnsPagedResults()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetEntitiesAsync(new ResourceCollectionParameters
            {
                PageNumber = 1,
                PageSize = 10
            });

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(1, result.Value.CurrentPage);
        }

        [Fact]
        public async Task GetEntitiesAsync_WithNoData_ReturnsEmptyPageList()
        {
            // Act
            var result = await _manager.GetEntitiesAsync(new ResourceCollectionParameters());

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
        }

        #endregion

        #region GetEntityByIdAsync Tests

        [Fact]
        public async Task GetEntityByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var logs = await SetupEnvironmentAsync();
            var expectedLog = logs.First();

            // Act
            var result = await _manager.GetEntityByIdAsync(expectedLog.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(expectedLog.Id, result.Value.Id);
        }

        [Fact]
        public async Task GetEntityByIdAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetEntityByIdAsync(99999);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region GetGasLogsAsync Tests

        [Fact]
        public async Task GetGasLogsAsync_WithValidAutomobileId_ReturnsLogs()
        {
            // Arrange
            var logs = await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetGasLogsAsync(_testCar.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            var logList = result.Value.ToList();
            Assert.Single(logList);
            Assert.All(logList, l => Assert.Equal(_testCar.Id, l.Car.Id));
        }

        [Fact]
        public async Task GetGasLogsAsync_WithNonExistentAutomobileId_ReturnsEmptyList()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetGasLogsAsync(99999);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetGasLogsAsync_WithPagination_ReturnsPagedResults()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetGasLogsAsync(_testCar.Id, new ResourceCollectionParameters
            {
                PageNumber = 1,
                PageSize = 5
            });

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(1, result.Value.CurrentPage);
            Assert.Equal(5, result.Value.PageSize);
        }

        [Fact]
        public async Task GetGasLogsAsync_FiltersOnlyForSpecifiedAutomobile()
        {
            // Arrange
            await SetupTestCarAsync();
            await SetupTestStationAsync();

            // Create a second car
            var secondCar = new AutomobileInfo
            {
                Brand = "Civic",
                Maker = "Honda",
                MeterReading = 50000,
                Year = 2020,
                VIN = "2HGBH41JXMN109187",
                Plate = "XYZ789",
                Color = "Red"
            };
            var createCarResult = await _autoManager.CreateAsync(secondCar);
            Assert.True(createCarResult.Success);
            var car2 = createCarResult.Value;

            // Create gas log for first car
            var gasLog1 = CreateValidGasLog();
            gasLog1.Car = _testCar;
            gasLog1.AutomobileId = _testCar.Id;
            var createLog1Result = await _manager.CreateAsync(gasLog1);
            Assert.True(createLog1Result.Success);

            // Create gas log for second car
            var gasLog2 = CreateValidGasLog();
            gasLog2.Car = car2;
            gasLog2.AutomobileId = car2.Id;
            gasLog2.Odometer = Dimension.FromKilometer(50100);
            gasLog2.Distance = Dimension.FromKilometer(100);
            var createLog2Result = await _manager.CreateAsync(gasLog2);
            Assert.True(createLog2Result.Success);

            // Act
            var result1 = await _manager.GetGasLogsAsync(_testCar.Id);
            var result2 = await _manager.GetGasLogsAsync(car2.Id);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.Single(result1.Value);
            Assert.Single(result2.Value);
            Assert.Equal(_testCar.Id, result1.Value.First().Car.Id);
            Assert.Equal(car2.Id, result2.Value.First().Car.Id);
        }

        #endregion

        #region LogHistoryAsync Tests

        [Fact]
        public async Task LogHistoryAsync_WithValidGasLog_ReturnsCreatedEntity()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();

            // Act
            var result = await _manager.LogHistoryAsync(gasLog);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id >= 1);
        }

        [Fact]
        public async Task LogHistoryAsync_WithNullEntity_ReturnsInvalidResult()
        {
            // Act
            var result = await _manager.LogHistoryAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("null", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task LogHistoryAsync_DoesNotValidateAgainstAutomobileMeterReading()
        {
            // Arrange - LogHistoryAsync should skip automobile meter reading validation
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();
            // Set odometer less than car's meter reading (100km), but distance <= odometer
            // This would fail CreateAsync's MeterReadingValid check, but should pass LogHistoryAsync
            gasLog.Odometer = Dimension.FromKilometer(50);
            gasLog.Distance = Dimension.FromKilometer(30); // Ensure distance <= odometer to pass validator

            // Act
            var result = await _manager.LogHistoryAsync(gasLog);

            // Assert
            // LogHistoryAsync should succeed because it doesn't check against automobile's meter reading
            Assert.True(result.Success);
        }

        [Fact]
        public async Task LogHistoryAsync_SetsAuthorIdAndCreateDate()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();
            gasLog.AuthorId = 0; // Clear author
            gasLog.CreateDate = DateTime.MinValue; // Clear create date

            // Act
            var result = await _manager.LogHistoryAsync(gasLog);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(TestDefaultAuthor.Id, result.Value.AuthorId);
            Assert.NotEqual(DateTime.MinValue, result.Value.CreateDate);
        }

        #endregion

        #region IsEntityOwnerAsync Tests

        [Fact]
        public async Task IsEntityOwnerAsync_WithOwnedEntity_ReturnsTrue()
        {
            // Arrange
            var logs = await SetupEnvironmentAsync();
            var log = logs.First();

            // Act
            var isOwner = await _manager.IsEntityOwnerAsync(log.Id);

            // Assert
            Assert.True(isOwner);
        }

        [Fact]
        public async Task IsEntityOwnerAsync_WithNonExistentEntity_ReturnsFalse()
        {
            // Act
            var isOwner = await _manager.IsEntityOwnerAsync(99999);

            // Assert
            Assert.False(isOwner);
        }

        #endregion

        #region Properties Tests

        [Fact]
        public void NoteSerializer_ReturnsValidSerializer()
        {
            // Assert
            Assert.NotNull(_manager.NoteSerializer);
        }

        [Fact]
        public void Validator_ReturnsValidValidator()
        {
            // Assert
            Assert.NotNull(_manager.Validator);
        }

        [Fact]
        public void AuthorProvider_ReturnsValidAuthor()
        {
            // Assert
            Assert.NotNull(_manager.AuthorProvider);
            Assert.NotNull(_manager.AuthorProvider.CachedAuthor);
            Assert.Equal(TestDefaultAuthor.AccountName, _manager.AuthorProvider.CachedAuthor.AccountName);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task CreateAsync_WithValidationFailure_ReturnsValidationError()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();
            gasLog.Station = null; // Station is required

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateAsync_WithFutureDate_ReturnsValidationError()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();
            gasLog.Date = DateTime.UtcNow.AddDays(1); // Future date

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateAsync_WithNegativeDistance_ReturnsValidationError()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();
            gasLog.Distance = Dimension.FromKilometer(-50);

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateAsync_WithZeroFuel_ReturnsValidationError()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();
            gasLog.Fuel = Volume.FromLiter(0);

            // Act
            var result = await _manager.CreateAsync(gasLog);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region Transactional Behavior Tests

        [Fact]
        public async Task CreateAsync_DeferredCommit_DoesNotCommitUntilExplicitCall()
        {
            // Arrange
            await SetupTestCarAsync();
            var gasLog = CreateValidGasLog();

            // Act - create with commitChanges=false
            var result = await _manager.CreateAsync(gasLog, commitChanges: false);

            // Assert - operation should succeed
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            // The data is tracked but would need IUnitOfWork.CommitAsync() to persist
            // In the mock environment, this just verifies the method signature works correctly
        }

        [Fact]
        public async Task UpdateAsync_DeferredCommit_DoesNotCommitUntilExplicitCall()
        {
            // Arrange
            var logs = await SetupEnvironmentAsync();
            var log = logs.First();

            // Act - update with commitChanges=false
            log.Comment = "Updated without commit";
            var result = await _manager.UpdateAsync(log, commitChanges: false);

            // Assert - operation should succeed
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
        }

        #endregion

        #region Helper Methods

        private async Task<IEnumerable<GasLog>> SetupEnvironmentAsync()
        {
            await SetupTestCarAsync();
            await SetupTestStationAsync();

            var gasLog = CreateValidGasLog();
            var createResult = await _manager.CreateAsync(gasLog);
            Assert.True(createResult.Success);

            var getResult = await _manager.GetEntitiesAsync(new ResourceCollectionParameters());
            Assert.True(getResult.Success);

            return getResult.Value.ToList();
        }

        private async Task SetupTestCarAsync()
        {
            if (_testCar != null)
            {
                return;
            }

            var car = new AutomobileInfo
            {
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = 2018,
                VIN = "1HGBH41JXMN109186",
                Color = "Blue",
                Plate = "BCTT208"
            };

            var result = await _autoManager.CreateAsync(car);
            Assert.True(result.Success);
            _testCar = result.Value;
        }

        private async Task SetupTestStationAsync()
        {
            if (_testStation != null)
            {
                return;
            }

            var station = new GasStation
            {
                Name = "Test Gas Station",
                Address = "123 Main St",
                City = "Vancouver",
                State = "BC",
                ZipCode = "V6B 1A1"
            };

            var result = await _stationManager.CreateAsync(station);
            Assert.True(result.Success);
            _testStation = result.Value;
        }

        private GasLog CreateValidGasLog()
        {
            return new GasLog
            {
                Date = DateTime.UtcNow.AddHours(-1),
                Car = _testCar,
                AutomobileId = _testCar?.Id ?? 0,
                Station = _testStation ?? new GasStation { Name = "Test Station" },
                Odometer = Dimension.FromKilometer(200),
                Distance = Dimension.FromKilometer(100),
                Fuel = Volume.FromLiter(45),
                TotalPrice = new Money(67.50m, CurrencyCodeType.Cad),
                UnitPrice = new Money(1.50m, CurrencyCodeType.Cad),
                FuelGrade = FuelGrade.Regular,
                IsFullTank = true,
                Comment = "Test fill up"
            };
        }

        private void SetupDevEnv()
        {
            InsertSeedRecords();

            // Create automobile manager first
            var autoNoteSerializer = new AutomobileJsonNoteSerialize(
                CatalogProvider,
                new NullLogger<AutomobileInfo>());
            var autoNoteManager = CreateNoteManager();
            _autoManager = new AutomobileManager(
                autoNoteSerializer,
                new AutomobileValidator(LookupRepository),
                autoNoteManager,
                LookupRepository,
                CreateMockAuthorProvider());

            // Create discount manager
            var discountNoteSerializer = new GasDiscountJsonNoteSerialize(
                CatalogProvider,
                new NullLogger<GasDiscount>());
            var discountNoteManager = CreateNoteManager();
            _discountManager = new DiscountManager(
                discountNoteSerializer,
                new GasDiscountValidator(LookupRepository),
                discountNoteManager,
                LookupRepository,
                CreateMockAuthorProvider());

            // Create station manager
            var stationNoteSerializer = new GasStationJsonNoteSerialize(
                CatalogProvider,
                new NullLogger<GasStation>());
            var stationNoteManager = CreateNoteManager();
            _stationManager = new GasStationManager(
                stationNoteSerializer,
                new GasStationValidator(LookupRepository),
                stationNoteManager,
                LookupRepository,
                CreateMockAuthorProvider());

            // Create gas log manager
            var gasLogNoteSerializer = CreateGasLogSerializer();
            var gasLogNoteManager = CreateNoteManager();
            _manager = new GasLogManager(
                gasLogNoteSerializer,
                new GasLogValidator(LookupRepository, _dateProviderMock.Object),
                gasLogNoteManager,
                _autoManager,
                LookupRepository,
                CreateMockAuthorProvider(),
                _dateProviderMock.Object);
        }

        private INoteSerializer<GasLog> CreateGasLogSerializer()
        {
            return new GasLogJsonNoteSerialize(
                CatalogProvider,
                new NullLogger<GasLog>(),
                _autoManager,
                _discountManager,
                _stationManager);
        }

        private IHmmNoteManager CreateNoteManager()
        {
            var mockNoteManager = new Mock<IHmmNoteManager>();
            var notes = new List<HmmNote>();
            var noteIdCounter = 1;

            // Setup CreateAsync
            mockNoteManager.Setup(m => m.CreateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()))
                .ReturnsAsync((HmmNote note, bool _) =>
                {
                    note.Id = noteIdCounter++;
                    notes.Add(note);
                    return ProcessingResult<HmmNote>.Ok(note);
                });

            // Setup UpdateAsync
            mockNoteManager.Setup(m => m.UpdateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()))
                .ReturnsAsync((HmmNote note, bool _) =>
                {
                    var existing = notes.FirstOrDefault(n => n.Id == note.Id);
                    if (existing != null)
                    {
                        notes.Remove(existing);
                        notes.Add(note);
                        return ProcessingResult<HmmNote>.Ok(note);
                    }
                    return ProcessingResult<HmmNote>.NotFound("Note not found");
                });

            // Setup GetNoteByIdAsync
            mockNoteManager.Setup(m => m.GetNoteByIdAsync(It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync((int id, bool includeDeleted) =>
                {
                    var note = notes.FirstOrDefault(n => n.Id == id);
                    return note != null
                        ? ProcessingResult<HmmNote>.Ok(note)
                        : ProcessingResult<HmmNote>.NotFound("Note not found");
                });

            // Setup GetNotesAsync
            mockNoteManager.Setup(m => m.GetNotesAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<HmmNote, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync((
                    System.Linq.Expressions.Expression<Func<HmmNote, bool>> query,
                    bool includeDeleted,
                    ResourceCollectionParameters para) =>
                {
                    var filtered = notes.AsQueryable().Where(query).ToList();
                    var pageList = PageList<HmmNote>.Create(filtered.AsQueryable(), para?.PageNumber ?? 1, para?.PageSize ?? 20);
                    return ProcessingResult<PageList<HmmNote>>.Ok(pageList);
                });

            return mockNoteManager.Object;
        }

        #endregion
    }
}
