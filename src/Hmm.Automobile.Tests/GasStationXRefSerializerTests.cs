using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasStationXRefSerializerTests : AutoTestFixtureBase
    {
        private GasStationXRefSerializer _serializer;
        private Author _author;
        private GasStation _testStation;
        private Mock<IAutoEntityManager<GasStation>> _stationManagerMock;

        public GasStationXRefSerializerTests()
        {
            SetupTestEnv();
        }

        #region DeserializeAsync Tests - ID Reference

        [Fact]
        public async Task DeserializeAsync_ValidStationId_ReturnsStation()
        {
            // Arrange
            var json = JsonDocument.Parse($"{_testStation.Id}").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(_testStation.Id, result.Value.Id);
            Assert.Equal(_testStation.Name, result.Value.Name);
        }

        [Fact]
        public async Task DeserializeAsync_InvalidStationId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = 999;
            _stationManagerMock.Setup(m => m.GetEntityByIdAsync(invalidId))
                .ReturnsAsync(ProcessingResult<GasStation>.NotFound($"Station {invalidId} not found"));

            var json = JsonDocument.Parse($"{invalidId}").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("not found", result.ErrorMessage);
        }

        [Fact]
        public async Task DeserializeAsync_ZeroStationId_ReturnsInvalid()
        {
            // Arrange
            var json = JsonDocument.Parse("0").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task DeserializeAsync_NegativeStationId_ReturnsInvalid()
        {
            // Arrange
            var json = JsonDocument.Parse("-1").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region DeserializeAsync Tests - String Name

        [Fact]
        public async Task DeserializeAsync_ValidStationName_ReturnsTransientStation()
        {
            // Arrange
            var json = JsonDocument.Parse("\"Shell Gas Station\"").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Shell Gas Station", result.Value.Name);
            Assert.Equal(0, result.Value.Id); // Transient station has no ID
        }

        [Fact]
        public async Task DeserializeAsync_EmptyStationName_ReturnsOkWithNull()
        {
            // Arrange
            var json = JsonDocument.Parse("\"\"").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task DeserializeAsync_WhitespaceStationName_ReturnsOkWithNull()
        {
            // Arrange
            var json = JsonDocument.Parse("\"   \"").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task DeserializeAsync_StationNameWithSpecialChars_ReturnsStation()
        {
            // Arrange
            var json = JsonDocument.Parse("\"Petro-Canada #123\"").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Petro-Canada #123", result.Value.Name);
        }

        #endregion

        #region DeserializeAsync Tests - Full Object

        [Fact]
        public async Task DeserializeAsync_FullObjectWithId_LooksUpStation()
        {
            // Arrange
            var json = JsonDocument.Parse($"{{\"id\":{_testStation.Id},\"name\":\"Ignored Name\"}}").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(_testStation.Id, result.Value.Id);
            Assert.Equal(_testStation.Name, result.Value.Name); // Should use looked-up name, not JSON name
        }

        [Fact]
        public async Task DeserializeAsync_FullObjectWithoutId_CreatesTransientStation()
        {
            // Arrange
            var json = JsonDocument.Parse("{\"name\":\"New Station\",\"address\":\"456 Test Ave\",\"city\":\"Toronto\",\"state\":\"ON\",\"zipCode\":\"M1M 1M1\"}").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("New Station", result.Value.Name);
            Assert.Equal("456 Test Ave", result.Value.Address);
            Assert.Equal("Toronto", result.Value.City);
            Assert.Equal("ON", result.Value.State);
            Assert.Equal("M1M 1M1", result.Value.ZipCode);
        }

        [Fact]
        public async Task DeserializeAsync_FullObjectWithZeroId_CreatesTransientStation()
        {
            // Arrange
            var json = JsonDocument.Parse("{\"id\":0,\"name\":\"Inline Station\",\"city\":\"Vancouver\"}").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Inline Station", result.Value.Name);
            Assert.Equal("Vancouver", result.Value.City);
        }

        [Fact]
        public async Task DeserializeAsync_FullObjectWithAllFields_ParsesAllFields()
        {
            // Arrange
            var json = JsonDocument.Parse("{\"name\":\"Complete Station\",\"address\":\"123 Full St\",\"city\":\"Calgary\",\"state\":\"AB\",\"zipCode\":\"T2P 1A1\",\"description\":\"A complete station\"}").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Complete Station", result.Value.Name);
            Assert.Equal("123 Full St", result.Value.Address);
            Assert.Equal("Calgary", result.Value.City);
            Assert.Equal("AB", result.Value.State);
            Assert.Equal("T2P 1A1", result.Value.ZipCode);
            Assert.Equal("A complete station", result.Value.Description);
        }

        [Fact]
        public async Task DeserializeAsync_FullObjectWithMissingFields_UsesDefaults()
        {
            // Arrange
            var json = JsonDocument.Parse("{\"name\":\"Minimal Station\"}").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Minimal Station", result.Value.Name);
            Assert.Null(result.Value.Address);
            Assert.Null(result.Value.City);
        }

        #endregion

        #region DeserializeAsync Tests - Invalid JSON

        [Fact]
        public async Task DeserializeAsync_NullValueKind_ReturnsOkWithNull()
        {
            // Arrange
            var json = JsonDocument.Parse("null").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task DeserializeAsync_ArrayValue_ReturnsOkWithNull()
        {
            // Arrange
            var json = JsonDocument.Parse("[]").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Value);
        }

        [Fact]
        public async Task DeserializeAsync_BooleanValue_ReturnsOkWithNull()
        {
            // Arrange
            var json = JsonDocument.Parse("true").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Value);
        }

        #endregion

        #region Serialize Tests

        [Fact]
        public void Serialize_NullStation_ReturnsOkWithNull()
        {
            // Arrange & Act
            var result = _serializer.Serialize(null);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Value);
        }

        [Fact]
        public void Serialize_StationWithId_ReturnsId()
        {
            // Arrange
            var station = new GasStation
            {
                Id = 42,
                Name = "Test Station",
                Address = "123 Test St"
            };

            // Act
            var result = _serializer.Serialize(station);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void Serialize_StationWithoutId_ReturnsObjectData()
        {
            // Arrange
            var station = new GasStation
            {
                Id = 0,
                Name = "New Station",
                Address = "456 New Ave",
                City = "Vancouver",
                State = "BC",
                ZipCode = "V6B 1A1",
                Description = "A new station"
            };

            // Act
            var result = _serializer.Serialize(station);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);

            // Verify it's an object with expected properties
            var json = JsonSerializer.Serialize(result.Value);
            Assert.Contains("\"name\":\"New Station\"", json);
            Assert.Contains("\"address\":\"456 New Ave\"", json);
            Assert.Contains("\"city\":\"Vancouver\"", json);
            Assert.Contains("\"state\":\"BC\"", json);
            Assert.Contains("\"zipCode\":\"V6B 1A1\"", json);
            Assert.Contains("\"description\":\"A new station\"", json);
        }

        [Fact]
        public void Serialize_StationWithNullFields_HandlesNullsGracefully()
        {
            // Arrange
            var station = new GasStation
            {
                Id = 0,
                Name = null,
                Address = null,
                City = null,
                State = null,
                ZipCode = null,
                Description = null
            };

            // Act
            var result = _serializer.Serialize(station);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);

            // Should have empty strings for null values
            var json = JsonSerializer.Serialize(result.Value);
            Assert.Contains("\"name\":\"\"", json);
            Assert.Contains("\"address\":\"\"", json);
        }

        #endregion

        #region SerializeToJson Tests

        [Fact]
        public void SerializeToJson_NullStation_ReturnsOkWithNull()
        {
            // Arrange & Act
            var result = _serializer.SerializeToJson(null);

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Value);
        }

        [Fact]
        public void SerializeToJson_StationWithId_ReturnsIdAsJson()
        {
            // Arrange
            var station = new GasStation
            {
                Id = 42,
                Name = "Test Station"
            };

            // Act
            var result = _serializer.SerializeToJson(station);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("42", result.Value);
        }

        [Fact]
        public void SerializeToJson_StationWithoutId_ReturnsJsonObject()
        {
            // Arrange
            var station = new GasStation
            {
                Id = 0,
                Name = "New Station",
                City = "Toronto"
            };

            // Act
            var result = _serializer.SerializeToJson(station);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Contains("\"name\":\"New Station\"", result.Value);
            Assert.Contains("\"city\":\"Toronto\"", result.Value);
        }

        #endregion

        #region FindOrCreateByNameAsync Tests

        [Fact]
        public async Task FindOrCreateByNameAsync_NullName_ReturnsInvalid()
        {
            // Arrange & Act
            var result = await _serializer.FindOrCreateByNameAsync(null);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("cannot be empty", result.ErrorMessage);
        }

        [Fact]
        public async Task FindOrCreateByNameAsync_EmptyName_ReturnsInvalid()
        {
            // Arrange & Act
            var result = await _serializer.FindOrCreateByNameAsync("");

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task FindOrCreateByNameAsync_WhitespaceName_ReturnsInvalid()
        {
            // Arrange & Act
            var result = await _serializer.FindOrCreateByNameAsync("   ");

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task FindOrCreateByNameAsync_ExistingStation_ReturnsExisting()
        {
            // Arrange
            var existingStations = new List<GasStation> { _testStation };
            _stationManagerMock.Setup(m => m.GetEntitiesAsync(null))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Ok(
                    PageList<GasStation>.Create(existingStations.AsQueryable(), 1, 20)));

            // Act
            var result = await _serializer.FindOrCreateByNameAsync(_testStation.Name);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(_testStation.Id, result.Value.Id);
        }

        [Fact]
        public async Task FindOrCreateByNameAsync_ExistingStationCaseInsensitive_ReturnsExisting()
        {
            // Arrange
            var existingStations = new List<GasStation> { _testStation };
            _stationManagerMock.Setup(m => m.GetEntitiesAsync(null))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Ok(
                    PageList<GasStation>.Create(existingStations.AsQueryable(), 1, 20)));

            // Act - search with different case
            var result = await _serializer.FindOrCreateByNameAsync(_testStation.Name.ToUpperInvariant());

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(_testStation.Id, result.Value.Id);
        }

        [Fact]
        public async Task FindOrCreateByNameAsync_NonExistingStation_CreatesNew()
        {
            // Arrange
            var existingStations = new List<GasStation> { _testStation };
            _stationManagerMock.Setup(m => m.GetEntitiesAsync(null))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Ok(
                    PageList<GasStation>.Create(existingStations.AsQueryable(), 1, 20)));

            // Act
            var result = await _serializer.FindOrCreateByNameAsync("Brand New Station");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("Brand New Station", result.Value.Name);
            Assert.Equal(0, result.Value.Id); // New station has no ID
        }

        [Fact]
        public async Task FindOrCreateByNameAsync_ManagerReturnsError_ReturnsNewStation()
        {
            // Arrange
            _stationManagerMock.Setup(m => m.GetEntitiesAsync(null))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Fail("Database error", ErrorCategory.ServerError));

            // Act
            var result = await _serializer.FindOrCreateByNameAsync("New Station");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("New Station", result.Value.Name);
        }

        [Fact]
        public async Task FindOrCreateByNameAsync_EmptyStationList_ReturnsNewStation()
        {
            // Arrange
            var emptyStations = new List<GasStation>();
            _stationManagerMock.Setup(m => m.GetEntitiesAsync(null))
                .ReturnsAsync(ProcessingResult<PageList<GasStation>>.Ok(
                    PageList<GasStation>.Create(emptyStations.AsQueryable(), 1, 20)));

            // Act
            var result = await _serializer.FindOrCreateByNameAsync("New Station");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("New Station", result.Value.Name);
            Assert.Contains("Gas station:", result.Value.Description);
        }

        #endregion

        #region Round-Trip Tests

        [Fact]
        public async Task RoundTrip_StationWithId_PreservesId()
        {
            // Arrange
            var original = _testStation;

            // Act
            var serializeResult = _serializer.Serialize(original);
            Assert.True(serializeResult.Success);

            // Create JSON element from serialized value
            var jsonString = JsonSerializer.Serialize(serializeResult.Value);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var deserializeResult = await _serializer.DeserializeAsync(jsonElement);

            // Assert
            Assert.True(deserializeResult.Success);
            Assert.Equal(original.Id, deserializeResult.Value.Id);
            Assert.Equal(original.Name, deserializeResult.Value.Name);
        }

        [Fact]
        public async Task RoundTrip_StationWithoutId_PreservesAllFields()
        {
            // Arrange
            var original = new GasStation
            {
                Id = 0,
                Name = "Test Station",
                Address = "123 Test St",
                City = "Vancouver",
                State = "BC",
                ZipCode = "V6B 1A1",
                Description = "A test station"
            };

            // Act
            var serializeResult = _serializer.Serialize(original);
            Assert.True(serializeResult.Success);

            var jsonString = JsonSerializer.Serialize(serializeResult.Value);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var deserializeResult = await _serializer.DeserializeAsync(jsonElement);

            // Assert
            Assert.True(deserializeResult.Success);
            Assert.Equal(original.Name, deserializeResult.Value.Name);
            Assert.Equal(original.Address, deserializeResult.Value.Address);
            Assert.Equal(original.City, deserializeResult.Value.City);
            Assert.Equal(original.State, deserializeResult.Value.State);
            Assert.Equal(original.ZipCode, deserializeResult.Value.ZipCode);
            Assert.Equal(original.Description, deserializeResult.Value.Description);
        }

        [Fact]
        public async Task RoundTrip_StringName_PreservesName()
        {
            // Arrange
            var stationName = "Quick Gas";
            var jsonElement = JsonDocument.Parse($"\"{stationName}\"").RootElement;

            // Act
            var deserializeResult = await _serializer.DeserializeAsync(jsonElement);

            // Assert
            Assert.True(deserializeResult.Success);
            Assert.Equal(stationName, deserializeResult.Value.Name);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task DeserializeAsync_VeryLongStationName_HandlesCorrectly()
        {
            // Arrange
            var longName = new string('A', 500);
            var json = JsonDocument.Parse($"\"{longName}\"").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(longName, result.Value.Name);
        }

        [Fact]
        public async Task DeserializeAsync_UnicodeStationName_HandlesCorrectly()
        {
            // Arrange
            var unicodeName = "Státion Café 日本語";
            var json = JsonDocument.Parse($"\"{unicodeName}\"").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(unicodeName, result.Value.Name);
        }

        [Fact]
        public async Task DeserializeAsync_ObjectWithInvalidId_LooksUpButFails()
        {
            // Arrange
            _stationManagerMock.Setup(m => m.GetEntityByIdAsync(999))
                .ReturnsAsync(ProcessingResult<GasStation>.NotFound("Not found"));

            var json = JsonDocument.Parse("{\"id\":999,\"name\":\"Unknown Station\"}").RootElement;

            // Act
            var result = await _serializer.DeserializeAsync(json);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public void Serialize_StationWithSpecialCharsInName_HandlesCorrectly()
        {
            // Arrange
            var station = new GasStation
            {
                Id = 0,
                Name = "Shell \"Super\" <Station> & More",
                Description = "Description with \"quotes\""
            };

            // Act
            var result = _serializer.Serialize(station);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);

            // Verify the JSON is valid
            var jsonString = JsonSerializer.Serialize(result.Value);
            var document = JsonDocument.Parse(jsonString);
            Assert.NotNull(document);
        }

        #endregion

        #region Helper Methods

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _author = ApplicationRegister.DefaultAuthor;

            _testStation = new GasStation
            {
                Id = 1,
                AuthorId = _author.Id,
                Name = "Costco Gas",
                Address = "123 Main Street",
                City = "Vancouver",
                State = "BC",
                ZipCode = "V6B 1A1",
                Description = "Costco gas station",
                IsActive = true
            };

            _stationManagerMock = new Mock<IAutoEntityManager<GasStation>>();
            _stationManagerMock.Setup(m => m.GetEntityByIdAsync(_testStation.Id))
                .ReturnsAsync(ProcessingResult<GasStation>.Ok(_testStation));

            _serializer = new GasStationXRefSerializer(
                _stationManagerMock.Object,
                new NullLogger<GasStation>());
        }

        #endregion
    }
}
