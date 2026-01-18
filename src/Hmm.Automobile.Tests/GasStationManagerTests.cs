using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Automobile.Validator;
using Hmm.Core;
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
    public class GasStationManagerTests : AutoTestFixtureBase
    {
        private GasStationManager _manager;

        public GasStationManagerTests()
        {
            SetupDevEnv();
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_WithValidStation_ReturnsCreatedEntity()
        {
            // Arrange
            var station = new GasStation
            {
                Name = "Costco Gas",
                Address = "123 Main St",
                City = "Vancouver",
                State = "BC",
                ZipCode = "V6B 1A1",
                Description = "Test station",
                IsActive = true
            };

            // Act
            var result = await _manager.CreateAsync(station);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id >= 1);
            Assert.Equal("Costco Gas", result.Value.Name);
            Assert.Equal(ApplicationRegister.DefaultAuthor.Id, result.Value.AuthorId);
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
            // Arrange - missing required name
            var station = new GasStation
            {
                Name = "", // Empty - invalid
                City = "Vancouver"
            };

            // Act
            var result = await _manager.CreateAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidStation_ReturnsUpdatedEntity()
        {
            // Arrange
            var stations = await SetupEnvironmentAsync();
            var station = stations.FirstOrDefault();
            Assert.NotNull(station);

            // Act
            station.Name = "Updated Gas Station";
            var result = await _manager.UpdateAsync(station);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Updated Gas Station", result.Value.Name);
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
            var station = new GasStation
            {
                Id = 99999, // Non-existent
                Name = "Test"
            };

            // Act
            var result = await _manager.UpdateAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesAllProperties()
        {
            // Arrange
            var stations = await SetupEnvironmentAsync();
            var station = stations.First();

            // Act
            station.Name = "Updated Name";
            station.Address = "Updated Address";
            station.City = "Updated City";
            station.State = "Updated State";
            station.ZipCode = "12345";
            station.Description = "Updated Description";
            station.IsActive = false;

            var result = await _manager.UpdateAsync(station);

            // Assert
            Assert.True(result.Success);
            var updated = result.Value;
            Assert.Equal("Updated Name", updated.Name);
            Assert.Equal("Updated Address", updated.Address);
            Assert.Equal("Updated City", updated.City);
            Assert.Equal("Updated State", updated.State);
            Assert.Equal("12345", updated.ZipCode);
            Assert.Equal("Updated Description", updated.Description);
            Assert.False(updated.IsActive);
        }

        #endregion

        #region GetEntitiesAsync Tests

        [Fact]
        public async Task GetEntitiesAsync_ReturnsAllStations()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetEntitiesAsync(new ResourceCollectionParameters());

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            var stations = result.Value.ToList();
            Assert.Equal(2, stations.Count);
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
                PageSize = 1
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
            var stations = await SetupEnvironmentAsync();
            var expectedStation = stations.First();

            // Act
            var result = await _manager.GetEntityByIdAsync(expectedStation.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(expectedStation.Id, result.Value.Id);
            Assert.Equal(expectedStation.Name, result.Value.Name);
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

        #region GetActiveStationsAsync Tests

        [Fact]
        public async Task GetActiveStationsAsync_ReturnsOnlyActiveStations()
        {
            // Arrange
            await SetupEnvironmentAsync();
            var allStations = await _manager.GetEntitiesAsync();
            var inactiveStation = allStations.Value.First();
            inactiveStation.IsActive = false;
            await _manager.UpdateAsync(inactiveStation);

            // Act
            var result = await _manager.GetActiveStationsAsync();

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.Value);
            Assert.True(result.Value.All(s => s.IsActive));
        }

        #endregion

        #region GetByNameAsync Tests

        [Fact]
        public async Task GetByNameAsync_WithValidName_ReturnsStation()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetByNameAsync("Costco Gas");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Costco Gas", result.Value.Name);
        }

        [Fact]
        public async Task GetByNameAsync_WithNonExistentName_ReturnsNotFound()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetByNameAsync("NonExistent Station");

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetByNameAsync_WithEmptyName_ReturnsInvalid()
        {
            // Act
            var result = await _manager.GetByNameAsync("");

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetByNameAsync_WithNullName_ReturnsInvalid()
        {
            // Act
            var result = await _manager.GetByNameAsync(null);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetByNameAsync_IsCaseInsensitive()
        {
            // Arrange
            await SetupEnvironmentAsync();

            // Act
            var result = await _manager.GetByNameAsync("COSTCO GAS");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
        }

        #endregion

        #region IsEntityOwnerAsync Tests

        [Fact]
        public async Task IsEntityOwnerAsync_WithOwnedEntity_ReturnsTrue()
        {
            // Arrange
            var stations = await SetupEnvironmentAsync();
            var station = stations.First();

            // Act
            var isOwner = await _manager.IsEntityOwnerAsync(station.Id);

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
        public void DefaultAuthor_ReturnsValidAuthor()
        {
            // Assert
            Assert.NotNull(_manager.DefaultAuthor);
            Assert.Equal(ApplicationRegister.DefaultAuthor.AccountName, _manager.DefaultAuthor.AccountName);
        }

        #endregion

        private async Task<List<GasStation>> SetupEnvironmentAsync()
        {
            var stations = new List<GasStation>();

            var station1 = new GasStation
            {
                Name = "Costco Gas",
                Address = "123 Main St",
                City = "Vancouver",
                State = "BC",
                ZipCode = "V6B 1A1",
                Description = "Costco gas station",
                IsActive = true
            };
            var result1 = await _manager.CreateAsync(station1);
            Assert.True(result1.Success);
            stations.Add(result1.Value);

            var station2 = new GasStation
            {
                Name = "Shell Station",
                Address = "456 Oak Ave",
                City = "Burnaby",
                State = "BC",
                ZipCode = "V5H 2Z8",
                Description = "Shell gas station",
                IsActive = true
            };
            var result2 = await _manager.CreateAsync(station2);
            Assert.True(result2.Success);
            stations.Add(result2.Value);

            return stations;
        }

        private void SetupDevEnv()
        {
            InsertSeedRecords();

            var noteSerializer = new GasStationJsonNoteSerialize(
                Application,
                new NullLogger<GasStation>(),
                LookupRepository);

            var noteManager = CreateNoteManager();

            _manager = new GasStationManager(
                noteSerializer,
                new GasStationValidator(LookupRepository),
                noteManager,
                LookupRepository);
        }

        private IHmmNoteManager CreateNoteManager()
        {
            var mockNoteManager = new Mock<IHmmNoteManager>();
            var notes = new List<HmmNote>();
            var noteIdCounter = 1;

            // Setup CreateAsync
            mockNoteManager.Setup(m => m.CreateAsync(It.IsAny<HmmNote>()))
                .ReturnsAsync((HmmNote note) =>
                {
                    note.Id = noteIdCounter++;
                    notes.Add(note);
                    return ProcessingResult<HmmNote>.Ok(note);
                });

            // Setup UpdateAsync
            mockNoteManager.Setup(m => m.UpdateAsync(It.IsAny<HmmNote>()))
                .ReturnsAsync((HmmNote note) =>
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
