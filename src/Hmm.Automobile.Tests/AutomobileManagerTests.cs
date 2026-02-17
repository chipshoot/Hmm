using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Automobile.Validator;
using Hmm.Core;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
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
    public class AutomobileManagerTests : AutoTestFixtureBase
    {
        private IAutoEntityManager<AutomobileInfo> _manager;

        public AutomobileManagerTests()
        {
            SetupDevEnv();
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidAutomobile_ReturnsCreatedEntity()
        {
            // Arrange
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

            // Act
            var result = await _manager.CreateAsync(car);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id >= 1);
            Assert.Equal("Outback", result.Value.Brand);
            Assert.Equal("Subaru", result.Value.Maker);
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
        public async Task CreateAsync_WithInvalidEntity_ReturnsValidationError()
        {
            // Arrange - missing required fields
            var car = new AutomobileInfo
            {
                Brand = "", // Empty - invalid
                Maker = "Subaru"
            };

            // Act
            var result = await _manager.CreateAsync(car);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidAutomobile_ReturnsUpdatedEntity()
        {
            // Arrange
            var cars = await SetupEnvironmentAsync();
            var car = cars.FirstOrDefault();
            Assert.NotNull(car);

            // Act
            car.Brand = "Outback1";
            var result = await _manager.UpdateAsync(car);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Outback1", result.Value.Brand);
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
            var car = new AutomobileInfo
            {
                Id = 99999, // Non-existent
                Brand = "Test",
                Maker = "Test"
            };

            // Act
            var result = await _manager.UpdateAsync(car);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task CreateAsync_SetsAuthorId()
        {
            // Arrange
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

            // Act
            var result = await _manager.CreateAsync(car);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(TestDefaultAuthor.Id, result.Value.AuthorId);
        }

        #endregion

        #region UpdateAsync Tests - Property Coverage

        [Fact]
        public async Task UpdateAsync_CopiesBrandMakerMeterReadingYearColorPlate()
        {
            // Arrange
            var cars = await SetupEnvironmentAsync();
            var car = cars.First();

            // Act
            car.Brand = "Forester";
            car.Maker = "Toyota";
            car.MeterReading = 5000;
            car.Year = 2024;
            car.Color = "Red";
            car.Plate = "NEW123";
            var result = await _manager.UpdateAsync(car);

            // Assert
            Assert.True(result.Success);
            var updated = result.Value;
            Assert.Equal("Forester", updated.Brand);
            Assert.Equal("Toyota", updated.Maker);
            Assert.Equal(5000, updated.MeterReading);
            Assert.Equal(2024, updated.Year);
            Assert.Equal("Red", updated.Color);
            Assert.Equal("NEW123", updated.Plate);
        }

        [Fact]
        public async Task UpdateAsync_PreservesEntityId()
        {
            // Arrange
            var cars = await SetupEnvironmentAsync();
            var car = cars.First();
            var originalId = car.Id;

            // Act
            car.Brand = "Updated";
            var result = await _manager.UpdateAsync(car);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(originalId, result.Value.Id);
        }

        #endregion

        #region Multiple Entities Tests

        [Fact]
        public async Task CreateAsync_MultipleEntities_AllRetrievable()
        {
            // Arrange
            var car1 = new AutomobileInfo
            {
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = 2018,
                VIN = "1HGBH41JXMN109186",
                Color = "Blue",
                Plate = "BCTT208"
            };
            var car2 = new AutomobileInfo
            {
                Brand = "Civic",
                Maker = "Honda",
                MeterReading = 50000,
                Year = 2020,
                VIN = "2HGBH41JXMN109187",
                Color = "Red",
                Plate = "XYZ789"
            };

            // Act
            var result1 = await _manager.CreateAsync(car1);
            var result2 = await _manager.CreateAsync(car2);

            // Assert
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.NotEqual(result1.Value.Id, result2.Value.Id);

            var allResult = await _manager.GetEntitiesAsync(new ResourceCollectionParameters());
            Assert.True(allResult.Success);
            Assert.Equal(2, allResult.Value.Count());
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
            var automobiles = result.Value.ToList();
            Assert.Single(automobiles);
            var savedCar = automobiles.First();
            Assert.Equal("Outback", savedCar.Brand);
            Assert.True(savedCar.Id >= 1);
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

        #endregion

        #region GetEntityByIdAsync Tests

        [Fact]
        public async Task GetEntityByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var cars = await SetupEnvironmentAsync();
            var expectedCar = cars.First();

            // Act
            var result = await _manager.GetEntityByIdAsync(expectedCar.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(expectedCar.Id, result.Value.Id);
            Assert.Equal("Outback", result.Value.Brand);
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

        #region IsEntityOwnerAsync Tests

        [Fact]
        public async Task IsEntityOwnerAsync_WithOwnedEntity_ReturnsTrue()
        {
            // Arrange
            var cars = await SetupEnvironmentAsync();
            var car = cars.First();

            // Act
            var isOwner = await _manager.IsEntityOwnerAsync(car.Id);

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

        private async Task<IEnumerable<AutomobileInfo>> SetupEnvironmentAsync()
        {
            var car = new AutomobileInfo
            {
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = 2018,
                VIN = "1HGBH41JXMN109186",
                Plate = "BCTT208",
                Color = "Blue"
            };

            var createResult = await _manager.CreateAsync(car);
            Assert.True(createResult.Success);

            var getResult = await _manager.GetEntitiesAsync(new ResourceCollectionParameters());
            Assert.True(getResult.Success);

            return getResult.Value.ToList();
        }

        private void SetupDevEnv()
        {
            InsertSeedRecords();

            var noteSerializer = new AutomobileJsonNoteSerialize(
                CatalogProvider,
                new NullLogger<AutomobileInfo>());

            var noteManager = CreateNoteManager();

            _manager = new AutomobileManager(
                noteSerializer,
                new AutomobileValidator(LookupRepository),
                noteManager,
                LookupRepository,
                CreateMockAuthorProvider());
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
    }
}
