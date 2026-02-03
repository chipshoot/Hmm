using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasStationJsonNoteSerializeTests : AutoTestFixtureBase
    {
        private GasStationJsonNoteSerialize _serializer;
        private Author _author;

        public GasStationJsonNoteSerializeTests()
        {
            SetupTestEnv();
        }

        #region GetNoteSerializationText Tests

        [Fact]
        public void GetNoteSerializationText_ValidEntity_ReturnsValidJson()
        {
            // Arrange
            var station = CreateValidStation();

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.NotEmpty(json);
            var document = JsonDocument.Parse(json);
            Assert.True(document.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.True(contentElement.TryGetProperty(AutomobileConstant.GasStationRecordSubject, out _));
        }

        [Fact]
        public void GetNoteSerializationText_NullEntity_ReturnsEmptyString()
        {
            // Arrange & Act
            var json = _serializer.GetNoteSerializationText(null);

            // Assert
            Assert.Empty(json);
        }

        [Fact]
        public void GetNoteSerializationText_ContainsAllFields()
        {
            // Arrange
            var station = CreateValidStation();

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.Contains("\"name\":", json);
            Assert.Contains("\"address\":", json);
            Assert.Contains("\"city\":", json);
            Assert.Contains("\"state\":", json);
            Assert.Contains("\"zipCode\":", json);
            Assert.Contains("\"description\":", json);
            Assert.Contains("\"isActive\":", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesNullName()
        {
            // Arrange
            var station = CreateValidStation();
            station.Name = null;

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("\"name\":\"\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesNullAddress()
        {
            // Arrange
            var station = CreateValidStation();
            station.Address = null;

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("\"address\":\"\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesNullCity()
        {
            // Arrange
            var station = CreateValidStation();
            station.City = null;

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("\"city\":\"\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesNullState()
        {
            // Arrange
            var station = CreateValidStation();
            station.State = null;

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("\"state\":\"\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesNullZipCode()
        {
            // Arrange
            var station = CreateValidStation();
            station.ZipCode = null;

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("\"zipCode\":\"\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesNullDescription()
        {
            // Arrange
            var station = CreateValidStation();
            station.Description = null;

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("\"description\":\"\"", json);
        }

        [Fact]
        public void GetNoteSerializationText_IsActiveTrue()
        {
            // Arrange
            var station = CreateValidStation();
            station.IsActive = true;

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.Contains("\"isActive\":true", json);
        }

        [Fact]
        public void GetNoteSerializationText_IsActiveFalse()
        {
            // Arrange
            var station = CreateValidStation();
            station.IsActive = false;

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.Contains("\"isActive\":false", json);
        }

        [Fact]
        public void GetNoteSerializationText_HandlesSpecialCharacters()
        {
            // Arrange
            var station = CreateValidStation();
            station.Name = "Shell \"Super\" <Station>";
            station.Address = "123 Main St & Oak Ave";
            station.Description = "Description with \"quotes\" and <brackets>";

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.NotEmpty(json);
            var document = JsonDocument.Parse(json);
            Assert.NotNull(document);
        }

        [Fact]
        public void GetNoteSerializationText_MinimalStation()
        {
            // Arrange - only required fields
            var station = new GasStation
            {
                Id = 1,
                AuthorId = _author.Id,
                Name = "Shell"
            };

            // Act
            var json = _serializer.GetNoteSerializationText(station);

            // Assert
            Assert.NotEmpty(json);
            Assert.Contains("\"name\":\"Shell\"", json);
        }

        #endregion

        #region GetEntity Tests

        [Fact]
        public async Task GetEntity_ValidNote_ReturnsStation()
        {
            // Arrange
            var station = CreateValidStation();
            var json = _serializer.GetNoteSerializationText(station);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(station.Name, result.Value.Name);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesAllFields()
        {
            // Arrange
            var station = CreateValidStation();
            station.Name = "Costco Gas";
            station.Address = "123 Main Street";
            station.City = "Vancouver";
            station.State = "BC";
            station.ZipCode = "V6B 1A1";
            station.Description = "Costco gas station";
            station.IsActive = true;

            var json = _serializer.GetNoteSerializationText(station);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Costco Gas", result.Value.Name);
            Assert.Equal("123 Main Street", result.Value.Address);
            Assert.Equal("Vancouver", result.Value.City);
            Assert.Equal("BC", result.Value.State);
            Assert.Equal("V6B 1A1", result.Value.ZipCode);
            Assert.Equal("Costco gas station", result.Value.Description);
            Assert.True(result.Value.IsActive);
        }

        [Fact]
        public async Task GetEntity_ValidNote_ParsesIsActiveField()
        {
            // Arrange
            var station = CreateValidStation();
            station.IsActive = false;

            var json = _serializer.GetNoteSerializationText(station);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.False(result.Value.IsActive);
        }

        [Fact]
        public async Task GetEntity_NullNote_ReturnsError()
        {
            // Arrange & Act
            var result = await _serializer.GetEntity(null);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_EmptyContent_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.GasStationRecordSubject,
                Content = "",
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_InvalidJson_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.GasStationRecordSubject,
                Content = "Not valid JSON",
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_MissingNoteElement_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.GasStationRecordSubject,
                Content = "{\"data\": {}}",
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_MissingContentElement_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.GasStationRecordSubject,
                Content = "{\"note\": {}}",
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_MissingGasStationElement_ReturnsError()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.GasStationRecordSubject,
                Content = "{\"note\": {\"content\": {}}}",
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetEntity_SetsIdFromNote()
        {
            // Arrange
            var station = CreateValidStation();
            var json = _serializer.GetNoteSerializationText(station);
            var note = new HmmNote
            {
                Id = 42,
                Author = _author,
                Subject = AutomobileConstant.GasStationRecordSubject,
                Content = json,
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(42, result.Value.Id);
        }

        [Fact]
        public async Task GetEntity_SetsAuthorIdFromNote()
        {
            // Arrange
            var station = CreateValidStation();
            var json = _serializer.GetNoteSerializationText(station);
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(_author.Id, result.Value.AuthorId);
        }

        [Fact]
        public async Task GetEntity_DefaultsIsActiveToTrue()
        {
            // Arrange - create JSON without isActive field
            var json = "{\"note\":{\"content\":{\"GasStation\":{\"name\":\"Shell\"}}}}";
            var note = CreateNote(json);

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.True(result.Value.IsActive);
        }

        #endregion

        #region GetNote Tests

        [Fact]
        public async Task GetNote_ValidEntity_ReturnsNote()
        {
            // Arrange
            var station = CreateValidStation();

            // Act
            var result = await _serializer.GetNote(station);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(AutomobileConstant.GasStationRecordSubject, result.Value.Subject);
            Assert.NotEmpty(result.Value.Content);
        }

        [Fact]
        public async Task GetNote_NullEntity_ReturnsError()
        {
            // Arrange & Act
            var result = await _serializer.GetNote(null);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GetNote_SetsNoteIdFromEntity()
        {
            // Arrange
            var station = CreateValidStation(123);

            // Act
            var result = await _serializer.GetNote(station);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(123, result.Value.Id);
        }

        [Fact]
        public async Task GetNote_SetsCorrectSubject()
        {
            // Arrange
            var station = CreateValidStation();

            // Act
            var result = await _serializer.GetNote(station);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(AutomobileConstant.GasStationRecordSubject, result.Value.Subject);
        }

        #endregion

        #region Round-Trip Tests

        [Fact]
        public async Task RoundTrip_AllFields_PreservesData()
        {
            // Arrange
            var original = CreateValidStation();

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(original.Name, result.Value.Name);
            Assert.Equal(original.Address, result.Value.Address);
            Assert.Equal(original.City, result.Value.City);
            Assert.Equal(original.State, result.Value.State);
            Assert.Equal(original.ZipCode, result.Value.ZipCode);
            Assert.Equal(original.Description, result.Value.Description);
            Assert.Equal(original.IsActive, result.Value.IsActive);
        }

        [Fact]
        public async Task RoundTrip_MinimalFields_PreservesData()
        {
            // Arrange
            var original = new GasStation
            {
                Id = 1,
                AuthorId = _author.Id,
                Name = "Shell"
            };

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Shell", result.Value.Name);
        }

        [Fact]
        public async Task RoundTrip_IsActiveFalse_PreservesData()
        {
            // Arrange
            var original = CreateValidStation();
            original.IsActive = false;

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.False(result.Value.IsActive);
        }

        [Fact]
        public async Task RoundTrip_SpecialCharacters_PreservesData()
        {
            // Arrange
            var original = CreateValidStation();
            original.Name = "Shell \"Super\" Station";
            original.Address = "123 Main St & Oak Ave";
            original.City = "Vancouver <BC>";
            original.Description = "Description with \"quotes\" and 'apostrophes'";

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(original.Name, result.Value.Name);
            Assert.Equal(original.Address, result.Value.Address);
            Assert.Equal(original.City, result.Value.City);
            Assert.Equal(original.Description, result.Value.Description);
        }

        [Fact]
        public async Task RoundTrip_LongDescription_PreservesData()
        {
            // Arrange
            var original = CreateValidStation();
            original.Description = new string('A', 500); // Max length for description

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(500, result.Value.Description.Length);
        }

        [Fact]
        public async Task RoundTrip_EmptyStringFields_PreservesData()
        {
            // Arrange
            var original = CreateValidStation();
            original.Address = "";
            original.City = "";
            original.State = "";
            original.ZipCode = "";
            original.Description = "";

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("", result.Value.Address);
            Assert.Equal("", result.Value.City);
            Assert.Equal("", result.Value.State);
            Assert.Equal("", result.Value.ZipCode);
            Assert.Equal("", result.Value.Description);
        }

        [Fact]
        public async Task RoundTrip_CanadianAddress_PreservesData()
        {
            // Arrange
            var original = CreateValidStation();
            original.Name = "Petro-Canada";
            original.Address = "4567 King Street West";
            original.City = "Toronto";
            original.State = "ON";
            original.ZipCode = "M5V 2T4";

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Petro-Canada", result.Value.Name);
            Assert.Equal("4567 King Street West", result.Value.Address);
            Assert.Equal("Toronto", result.Value.City);
            Assert.Equal("ON", result.Value.State);
            Assert.Equal("M5V 2T4", result.Value.ZipCode);
        }

        [Fact]
        public async Task RoundTrip_USAddress_PreservesData()
        {
            // Arrange
            var original = CreateValidStation();
            original.Name = "Chevron";
            original.Address = "123 Broadway";
            original.City = "Seattle";
            original.State = "WA";
            original.ZipCode = "98101";

            // Act
            var json = _serializer.GetNoteSerializationText(original);
            var note = CreateNote(json);
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Chevron", result.Value.Name);
            Assert.Equal("123 Broadway", result.Value.Address);
            Assert.Equal("Seattle", result.Value.City);
            Assert.Equal("WA", result.Value.State);
            Assert.Equal("98101", result.Value.ZipCode);
        }

        #endregion

        #region Helper Methods

        private GasStation CreateValidStation(int id = 0)
        {
            var stationId = id > 0 ? id : 1;
            return new GasStation
            {
                Id = stationId,
                AuthorId = _author.Id,
                Name = "Costco Gas",
                Address = "123 Main Street",
                City = "Vancouver",
                State = "BC",
                ZipCode = "V6B 1A1",
                Description = "Costco gas station with good prices",
                IsActive = true
            };
        }

        private HmmNote CreateNote(string content)
        {
            return new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.GasStationRecordSubject,
                Content = content,
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _serializer = new GasStationJsonNoteSerialize(CatalogProvider, new NullLogger<GasStation>());
            _author = TestDefaultAuthor;
        }

        #endregion
    }
}
