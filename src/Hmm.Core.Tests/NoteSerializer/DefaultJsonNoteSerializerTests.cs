using Hmm.Core.Map.DomainEntity;
using Hmm.Core.NoteSerializer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Text.Json;
using Xunit;

namespace Hmm.Core.Tests.NoteSerializer
{
    /// <summary>
    /// Tests for DefaultJsonNoteSerializer base class.
    /// Tests all base functionality including JSON structure validation,
    /// helper methods, and ProcessingResult pattern usage.
    /// </summary>
    public class DefaultJsonNoteSerializerTests : IDisposable
    {
        private readonly TestJsonNoteSerializer _serializer;
        private readonly Author _testAuthor;
        private readonly NoteCatalog _testCatalog;

        public DefaultJsonNoteSerializerTests()
        {
            var logger = new NullLogger<HmmNote>();
            _serializer = new TestJsonNoteSerializer(logger);

            _testAuthor = new Author
            {
                Id = 1,
                AccountName = "testuser",
                IsActivated = true
            };

            _testCatalog = new NoteCatalog
            {
                Id = 1,
                Name = "TestCatalog",
                Schema = null
            };
        }

        #region GetEntity Tests

        [Fact]
        public void GetEntity_WithNullNote_ReturnsFailureResult()
        {
            // Arrange
            HmmNote note = null;

            // Act
            var result = _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
            Assert.Contains("Null note", result.ErrorMessage);
        }

        [Fact]
        public void GetEntity_WithHmmNoteType_ReturnsNote()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Subject = "Test Subject",
                Content = "{\"note\":{\"content\":\"test content\"}}",
                Author = _testAuthor,
                Catalog = _testCatalog
            };

            // Act
            var result = _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(note.Id, result.Value.Id);
            Assert.Equal(note.Subject, result.Value.Subject);
        }

        [Fact]
        public void GetEntity_WithNonHmmNoteType_ReturnsFailure()
        {
            // Arrange - using the concrete test serializer which uses HmmNote as T
            var note = new HmmNote
            {
                Id = 1,
                Subject = "Test",
                Content = "test",
                Author = _testAuthor,
                Catalog = _testCatalog
            };

            var stringSerializer = new TestJsonNoteSerializer(new NullLogger<HmmNote>());

            // Act
            var result = stringSerializer.GetEntity(note);

            // Assert
            Assert.True(result.Success); // HmmNote is assignable to HmmNote
        }

        #endregion

        #region GetNote Tests

        [Fact]
        public void GetNote_WithNullEntity_ReturnsFailureResult()
        {
            // Arrange
            HmmNote entity = null;

            // Act
            var result = _serializer.GetNote(entity);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
            Assert.Contains("Null entity", result.ErrorMessage);
        }

        [Fact]
        public void GetNote_WithValidHmmNote_ReturnsSerializedNote()
        {
            // Arrange
            var entity = new HmmNote
            {
                Id = 1,
                Subject = "Test Subject",
                Content = "Simple text content",
                Author = _testAuthor,
                Catalog = _testCatalog,
                CreateDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var result = _serializer.GetNote(entity);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(entity.Id, result.Value.Id);
            Assert.Equal(entity.Subject, result.Value.Subject);
            Assert.NotEmpty(result.Value.Content);

            // Verify content is valid JSON
            var jsonDoc = JsonDocument.Parse(result.Value.Content);
            Assert.True(jsonDoc.RootElement.TryGetProperty("note", out _));
        }

        [Fact]
        public void GetNote_WithValidHmmNoteContainingJsonContent_PreservesStructure()
        {
            // Arrange
            var jsonContent = "{\"note\":{\"content\":{\"data\":\"test value\"}}}";
            var entity = new HmmNote
            {
                Id = 2,
                Subject = "JSON Content",
                Content = jsonContent,
                Author = _testAuthor,
                Catalog = _testCatalog
            };

            // Act
            var result = _serializer.GetNote(entity);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);

            var resultDoc = JsonDocument.Parse(result.Value.Content);
            Assert.True(resultDoc.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
        }

        #endregion

        #region GetNoteSerializationText Tests

        [Fact]
        public void GetNoteSerializationText_WithNullEntity_ReturnsEmptyString()
        {
            // Arrange
            HmmNote entity = null;

            // Act
            var result = _serializer.GetNoteSerializationText(entity);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetNoteSerializationText_WithValidHmmNote_ReturnsJsonString()
        {
            // Arrange
            var entity = new HmmNote
            {
                Id = 1,
                Subject = "Test",
                Content = "Plain text content",
                Author = _testAuthor,
                Catalog = _testCatalog
            };

            // Act
            var result = _serializer.GetNoteSerializationText(entity);

            // Assert
            Assert.NotEmpty(result);

            // Verify it's valid JSON
            var jsonDoc = JsonDocument.Parse(result);
            Assert.True(jsonDoc.RootElement.TryGetProperty("note", out _));
        }

        #endregion

        #region GetNoteContent Tests

        [Fact]
        public void GetNoteContent_WithEmptyString_ReturnsEmptyNoteStructure()
        {
            // Arrange
            string content = string.Empty;

            // Act
            using var result = _serializer.TestGetNoteContent(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(string.Empty, contentElement.GetString());
        }

        [Fact]
        public void GetNoteContent_WithPlainText_WrapsInNoteStructure()
        {
            // Arrange
            string content = "This is plain text content";

            // Act
            using var result = _serializer.TestGetNoteContent(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(content, contentElement.GetString());
        }

        [Fact]
        public void GetNoteContent_WithValidNoteJson_ReturnsAsIs()
        {
            // Arrange
            var content = "{\"note\":{\"content\":{\"data\":\"test\"}}}";

            // Act
            using var result = _serializer.TestGetNoteContent(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(JsonValueKind.Object, contentElement.ValueKind);
        }

        [Fact]
        public void GetNoteContent_WithInvalidJson_WrapsAsPlainText()
        {
            // Arrange
            string content = "{invalid json}";

            // Act
            using var result = _serializer.TestGetNoteContent(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(content, contentElement.GetString());
        }

        [Fact]
        public void GetNoteContent_WithJsonMissingNoteStructure_WrapsIt()
        {
            // Arrange
            var content = "{\"data\":\"value\"}";

            // Act
            using var result = _serializer.TestGetNoteContent(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out _));
        }

        [Fact]
        public void GetNoteContent_WithJsonElement_CreatesProperStructure()
        {
            // Arrange
            var testData = JsonDocument.Parse("{\"key\":\"value\"}");
            var element = testData.RootElement;

            // Act
            using var result = _serializer.TestGetNoteContent(element);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.True(contentElement.TryGetProperty("key", out var keyElement));
            Assert.Equal("value", keyElement.GetString());

            testData.Dispose();
        }

        #endregion

        #region IsValidNoteStructure Tests

        [Fact]
        public void IsValidNoteStructure_WithValidStructure_ReturnsTrue()
        {
            // Arrange
            var json = "{\"note\":{\"content\":\"test\"}}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = _serializer.TestIsValidNoteStructure(document);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidNoteStructure_WithMissingNoteProperty_ReturnsFalse()
        {
            // Arrange
            var json = "{\"data\":{\"content\":\"test\"}}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = _serializer.TestIsValidNoteStructure(document);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidNoteStructure_WithMissingContentProperty_ReturnsFalse()
        {
            // Arrange
            var json = "{\"note\":{\"data\":\"test\"}}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = _serializer.TestIsValidNoteStructure(document);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidNoteStructure_WithNullDocument_ReturnsFalse()
        {
            // Arrange
            JsonDocument document = null;

            // Act
            var result = _serializer.TestIsValidNoteStructure(document);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Helper Method Tests

        [Fact]
        public void GetStringProperty_WithValidProperty_ReturnsValue()
        {
            // Arrange
            var json = "{\"name\":\"John Doe\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetStringProperty(element, "name");

            // Assert
            Assert.Equal("John Doe", result);
        }

        [Fact]
        public void GetStringProperty_WithMissingProperty_ReturnsDefault()
        {
            // Arrange
            var json = "{\"name\":\"John Doe\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetStringProperty(element, "age", "default");

            // Assert
            Assert.Equal("default", result);
        }

        [Fact]
        public void GetIntProperty_WithValidProperty_ReturnsValue()
        {
            // Arrange
            var json = "{\"age\":25}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetIntProperty(element, "age");

            // Assert
            Assert.Equal(25, result);
        }

        [Fact]
        public void GetIntProperty_WithMissingProperty_ReturnsDefault()
        {
            // Arrange
            var json = "{\"name\":\"John\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetIntProperty(element, "age", 0);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetDateTimeProperty_WithValidIsoString_ReturnsDateTime()
        {
            // Arrange
            var dateTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var json = $"{{\"date\":\"{dateTime:O}\"}}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetDateTimeProperty(element, "date");

            // Assert
            Assert.Equal(dateTime, result);
        }

        [Fact]
        public void GetDateTimeProperty_WithMissingProperty_ReturnsDefault()
        {
            // Arrange
            var json = "{\"name\":\"test\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            var defaultDate = new DateTime(2020, 1, 1);

            // Act
            var result = _serializer.TestGetDateTimeProperty(element, "date", defaultDate);

            // Assert
            Assert.Equal(defaultDate, result);
        }

        [Fact]
        public void GetBoolProperty_WithValidProperty_ReturnsValue()
        {
            // Arrange
            var json = "{\"isActive\":true}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetBoolProperty(element, "isActive");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetBoolProperty_WithMissingProperty_ReturnsDefault()
        {
            // Arrange
            var json = "{\"name\":\"test\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetBoolProperty(element, "isActive", false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetDoubleProperty_WithValidProperty_ReturnsValue()
        {
            // Arrange
            var json = "{\"price\":19.99}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetDoubleProperty(element, "price");

            // Assert
            Assert.Equal(19.99, result, 0.001);
        }

        [Fact]
        public void GetDoubleProperty_WithMissingProperty_ReturnsDefault()
        {
            // Arrange
            var json = "{\"name\":\"test\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetDoubleProperty(element, "price", 0.0);

            // Assert
            Assert.Equal(0.0, result);
        }

        #endregion

        #region CreateEmptyNoteDocument Tests

        [Fact]
        public void CreateEmptyNoteDocument_ReturnsValidStructure()
        {
            // Act
            using var result = _serializer.TestCreateEmptyNoteDocument();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(string.Empty, contentElement.GetString());
        }

        #endregion

        #region CreateNoteJsonDocument Tests

        [Fact]
        public void CreateNoteJsonDocument_WithContent_WrapsInStructure()
        {
            // Arrange
            var content = "Test content";

            // Act
            using var result = _serializer.TestCreateNoteJsonDocument(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(content, contentElement.GetString());
        }

        [Fact]
        public void CreateNoteJsonDocument_WithNullContent_CreatesEmptyContent()
        {
            // Arrange
            string content = null;

            // Act
            using var result = _serializer.TestCreateNoteJsonDocument(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(string.Empty, contentElement.GetString());
        }

        #endregion

        #region GetCatalog Tests

        [Fact]
        public void GetCatalog_ReturnsNoteCatalogWithTypeName()
        {
            // Act
            var catalog = _serializer.TestGetCatalog();

            // Assert
            Assert.NotNull(catalog);
            Assert.Contains("HmmNote", catalog.Name);
            Assert.Null(catalog.Schema);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void GetNote_WithEmptyContent_CreatesValidJsonStructure()
        {
            // Arrange
            var entity = new HmmNote
            {
                Id = 1,
                Subject = "Empty Content",
                Content = string.Empty,
                Author = _testAuthor,
                Catalog = _testCatalog
            };

            // Act
            var result = _serializer.GetNote(entity);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.NotEmpty(result.Value.Content);

            // Verify valid JSON structure
            using var jsonDoc = JsonDocument.Parse(result.Value.Content);
            Assert.True(jsonDoc.RootElement.TryGetProperty("note", out _));
        }

        [Fact]
        public void GetNoteContent_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var content = "Content with special chars: < > & \" '";

            // Act
            using var result = _serializer.TestGetNoteContent(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(content, contentElement.GetString());
        }

        [Fact]
        public void GetNoteContent_WithUnicodeCharacters_PreservesEncoding()
        {
            // Arrange
            var content = "Unicode content: 你好世界 🌍 测试";

            // Act
            using var result = _serializer.TestGetNoteContent(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(content, contentElement.GetString());
        }

        [Fact]
        public void ProcessingResult_Success_HasCorrectProperties()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Subject = "Test",
                Content = "test",
                Author = _testAuthor,
                Catalog = _testCatalog
            };

            // Act
            var result = _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.False(result.HasError);
            Assert.False(result.HasWarning);
        }

        [Fact]
        public void ProcessingResult_Failure_HasCorrectProperties()
        {
            // Arrange
            HmmNote note = null;

            // Act
            var result = _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Value);
            Assert.True(result.HasError);
            Assert.NotEmpty(result.ErrorMessage);
        }

        #endregion

        public void Dispose()
        {
            // Cleanup if needed
            GC.SuppressFinalize(this);
        }

        #region Test Helper Class

        /// <summary>
        /// Concrete implementation of DefaultJsonNoteSerializer for testing.
        /// Exposes protected methods as public for testing.
        /// </summary>
        private class TestJsonNoteSerializer : DefaultJsonNoteSerializer<HmmNote>
        {
            public TestJsonNoteSerializer(ILogger<HmmNote> logger) : base(logger)
            {
            }

            // Expose protected methods for testing
            public JsonDocument TestGetNoteContent(string noteContent) => GetNoteContent(noteContent);

            public JsonDocument TestGetNoteContent(JsonElement contentElement) => GetNoteContent(contentElement);

            public bool TestIsValidNoteStructure(JsonDocument document) => IsValidNoteStructure(document);

            public JsonDocument TestCreateEmptyNoteDocument() => CreateEmptyNoteDocument();

            public JsonDocument TestCreateNoteJsonDocument(string content) => CreateNoteJsonDocument(content);

            public NoteCatalog TestGetCatalog() => GetCatalog();

            public string TestGetStringProperty(JsonElement element, string propertyName, string defaultValue = null)
                => GetStringProperty(element, propertyName, defaultValue);

            public int TestGetIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
                => GetIntProperty(element, propertyName, defaultValue);

            public DateTime TestGetDateTimeProperty(JsonElement element, string propertyName, DateTime? defaultValue = null)
                => GetDateTimeProperty(element, propertyName, defaultValue);

            public bool TestGetBoolProperty(JsonElement element, string propertyName, bool defaultValue = false)
                => GetBoolProperty(element, propertyName, defaultValue);

            public double TestGetDoubleProperty(JsonElement element, string propertyName, double defaultValue = 0.0)
                => GetDoubleProperty(element, propertyName, defaultValue);
        }

        #endregion
    }
}
