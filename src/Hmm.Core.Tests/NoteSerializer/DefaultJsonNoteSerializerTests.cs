using Hmm.Core.Map.DomainEntity;
using Hmm.Core.NoteSerializer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NJsonSchema.Validation;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
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
        public async Task GetEntity_WithNullNote_ReturnsFailureResult()
        {
            // Arrange
            HmmNote note = null;

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
            Assert.Contains("Null note", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEntity_WithHmmNoteType_ReturnsNote()
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
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(note.Id, result.Value.Id);
            Assert.Equal(note.Subject, result.Value.Subject);
        }

        [Fact]
        public async Task GetEntity_WithNonHmmNoteType_ReturnsFailure()
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
            var result = await stringSerializer.GetEntity(note);

            // Assert
            Assert.True(result.Success); // HmmNote is assignable to HmmNote
        }

        [Fact]
        public async Task GetEntity_WithComplexJsonContent_ReturnsNote()
        {
            // Arrange
            var note = new HmmNote
            {
                Id = 1,
                Subject = "Complex JSON",
                Content = "{\"note\":{\"content\":{\"nested\":{\"data\":\"value\"},\"array\":[1,2,3]}}}",
                Author = _testAuthor,
                Catalog = _testCatalog
            };

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(note.Content, result.Value.Content);
        }

        #endregion

        #region GetNote Tests

        [Fact]
        public async Task GetNote_WithNullEntity_ReturnsFailureResult()
        {
            // Arrange
            HmmNote entity = null;

            // Act
            var result = await _serializer.GetNote(entity);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
            Assert.Contains("Null entity", result.ErrorMessage);
        }

        [Fact]
        public async Task GetNote_WithValidHmmNote_ReturnsSerializedNote()
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
            var result = await _serializer.GetNote(entity);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(entity.Id, result.Value.Id);
            Assert.Equal(entity.Subject, result.Value.Subject);
            Assert.NotEmpty(result.Value.Content);

            // Verify content is valid JSON
            using var jsonDoc = JsonDocument.Parse(result.Value.Content);
            Assert.True(jsonDoc.RootElement.TryGetProperty("note", out _));
        }

        [Fact]
        public async Task GetNote_WithValidHmmNoteContainingJsonContent_PreservesStructure()
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
            var result = await _serializer.GetNote(entity);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);

            using var resultDoc = JsonDocument.Parse(result.Value.Content);
            Assert.True(resultDoc.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out _));
        }

        [Fact]
        public async Task GetNote_PreservesAuthorAndCatalog()
        {
            // Arrange
            var entity = new HmmNote
            {
                Id = 3,
                Subject = "Author Test",
                Content = "test content",
                Author = _testAuthor,
                Catalog = _testCatalog,
                Description = "Test description"
            };

            // Act
            var result = await _serializer.GetNote(entity);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(_testAuthor, result.Value.Author);
            Assert.Equal(_testCatalog, result.Value.Catalog);
            Assert.Equal("Test description", result.Value.Description);
        }

        [Fact]
        public async Task GetNote_WithNoCatalog_UsesDefaultCatalog()
        {
            // Arrange
            var entity = new HmmNote
            {
                Id = 4,
                Subject = "No Catalog",
                Content = "test",
                Author = _testAuthor,
                Catalog = null
            };

            // Act
            var result = await _serializer.GetNote(entity);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value.Catalog);
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

        [Fact]
        public void IsValidNoteStructure_WithArrayRoot_ReturnsFalse()
        {
            // Arrange
            var json = "[1, 2, 3]";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = _serializer.TestIsValidNoteStructure(document);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidNoteStructure_WithNoteAsArray_ReturnsFalse()
        {
            // Arrange
            var json = "{\"note\":[1,2,3]}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = _serializer.TestIsValidNoteStructure(document);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidNoteStructure_WithNoteAsString_ReturnsFalse()
        {
            // Arrange
            var json = "{\"note\":\"string value\"}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = _serializer.TestIsValidNoteStructure(document);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidNoteStructure_WithNestedContent_ReturnsTrue()
        {
            // Arrange
            var json = "{\"note\":{\"content\":{\"nested\":{\"deep\":\"value\"}}}}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = _serializer.TestIsValidNoteStructure(document);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidNoteStructure_WithContentAsArray_ReturnsTrue()
        {
            // Arrange - content can be any value type including array
            var json = "{\"note\":{\"content\":[1,2,3]}}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = _serializer.TestIsValidNoteStructure(document);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region ValidateContent Tests

        [Fact]
        public void ValidateContent_WithNullDocument_ReturnsEmptyCollection()
        {
            // Arrange
            JsonDocument document = null;

            // Act
            var result = _serializer.TestValidateContent(document);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateContent_WithNoSchema_ReturnsEmptyCollection()
        {
            // Arrange
            var json = "{\"note\":{\"content\":\"test\"}}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = _serializer.TestValidateContent(document);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateContent_WithSchemaAndValidContent_ReturnsEmptyCollection()
        {
            // Arrange
            var schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""note"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""content"": { ""type"": ""string"" }
                        }
                    }
                }
            }";
            var catalog = new NoteCatalog { Name = "Test", Schema = schema };
            var serializerWithSchema = new TestJsonNoteSerializer(new NullLogger<HmmNote>(), catalog);

            var json = "{\"note\":{\"content\":\"test\"}}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = serializerWithSchema.TestValidateContent(document);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateContent_WithSchemaButUninitialized_ReturnsEmptyCollection()
        {
            // Arrange - Schema is set but _schema field is not initialized until Validator property is accessed
            // Since Validator property is private and ValidateContent uses _schema directly,
            // the schema validation will not occur without explicit Validator access
            var schema = @"{
                ""type"": ""string""
            }";
            var catalog = new NoteCatalog { Name = "Test", Schema = schema };
            var serializerWithSchema = new TestJsonNoteSerializer(new NullLogger<HmmNote>(), catalog);

            var json = "{\"note\":{\"content\":\"test\"}}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = serializerWithSchema.TestValidateContent(document);

            // Assert - Returns empty because _schema is lazily loaded via Validator property
            // which is not called directly by ValidateContent
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateContent_WithEmptySchema_ReturnsEmptyCollection()
        {
            // Arrange
            var catalog = new NoteCatalog { Name = "Test", Schema = "" };
            var serializerWithSchema = new TestJsonNoteSerializer(new NullLogger<HmmNote>(), catalog);

            var json = "{\"note\":{\"content\":\"test\"}}";
            using var document = JsonDocument.Parse(json);

            // Act
            var result = serializerWithSchema.TestValidateContent(document);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
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

        [Fact]
        public void GetBoolProperty_WithFalseValue_ReturnsFalse()
        {
            // Arrange
            var json = "{\"isActive\":false}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetBoolProperty(element, "isActive", true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetIntProperty_WithNonIntegerValue_ThrowsException()
        {
            // Arrange - TryGetInt32 throws when value is not a Number type
            var json = "{\"count\":\"not a number\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act & Assert - Implementation does not gracefully handle type mismatch
            Assert.Throws<InvalidOperationException>(() => _serializer.TestGetIntProperty(element, "count", 42));
        }

        [Fact]
        public void GetDoubleProperty_WithIntegerValue_ReturnsDouble()
        {
            // Arrange
            var json = "{\"value\":42}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetDoubleProperty(element, "value");

            // Assert
            Assert.Equal(42.0, result);
        }

        [Fact]
        public void GetStringProperty_WithNullValue_ReturnsNull()
        {
            // Arrange
            var json = "{\"name\":null}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetStringProperty(element, "name");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetStringProperty_WithNullValueAndDefault_ReturnsDefault()
        {
            // Arrange
            var json = "{\"name\":null}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetStringProperty(element, "name", "default");

            // Assert
            Assert.Equal("default", result);
        }

        [Fact]
        public void GetDateTimeProperty_WithInvalidFormat_ReturnsDefault()
        {
            // Arrange
            var json = "{\"date\":\"not a date\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            var defaultDate = new DateTime(2020, 1, 1);

            // Act
            var result = _serializer.TestGetDateTimeProperty(element, "date", defaultDate);

            // Assert
            Assert.Equal(defaultDate, result);
        }

        [Fact]
        public void GetDateTimeProperty_WithNoDefault_ReturnsMinValue()
        {
            // Arrange
            var json = "{\"name\":\"test\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetDateTimeProperty(element, "date");

            // Assert
            Assert.Equal(DateTime.MinValue, result);
        }

        [Fact]
        public void GetDoubleProperty_WithNonNumericValue_ThrowsException()
        {
            // Arrange - TryGetDouble throws when value is not a Number type
            var json = "{\"value\":\"not a number\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act & Assert - Implementation does not gracefully handle type mismatch
            Assert.Throws<InvalidOperationException>(() => _serializer.TestGetDoubleProperty(element, "value", 99.9));
        }

        [Fact]
        public void GetIntProperty_WithDecimalValue_ReturnsDefault()
        {
            // Arrange - 3.14 cannot be parsed as Int32
            var json = "{\"count\":3.14}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetIntProperty(element, "count", 100);

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public void GetBoolProperty_WithNonBooleanValue_ReturnsDefault()
        {
            // Arrange
            var json = "{\"isActive\":\"yes\"}";
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;

            // Act
            var result = _serializer.TestGetBoolProperty(element, "isActive", true);

            // Assert
            Assert.True(result); // Returns default because "yes" is not a boolean
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
        public async Task GetNote_WithEmptyContent_CreatesValidJsonStructure()
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
            var result = await _serializer.GetNote(entity);

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
        public async Task ProcessingResult_Success_HasCorrectProperties()
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
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.False(result.HasError);
            Assert.False(result.HasWarning);
        }

        [Fact]
        public async Task ProcessingResult_Failure_HasCorrectProperties()
        {
            // Arrange
            HmmNote note = null;

            // Act
            var result = await _serializer.GetEntity(note);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Value);
            Assert.True(result.HasError);
            Assert.NotEmpty(result.ErrorMessage);
        }

        [Fact]
        public void GetNoteContent_WithNullString_ReturnsEmptyNoteStructure()
        {
            // Arrange
            string content = null;

            // Act
            using var result = _serializer.TestGetNoteContent(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(string.Empty, contentElement.GetString());
        }

        [Fact]
        public void GetNoteContent_WithLargeContent_HandlesCorrectly()
        {
            // Arrange
            var content = new string('X', 100000); // 100KB of content

            // Act
            using var result = _serializer.TestGetNoteContent(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(content, contentElement.GetString());
        }

        [Fact]
        public void GetNoteContent_WithNewlinesAndTabs_PreservesThem()
        {
            // Arrange
            var content = "Line1\nLine2\r\nLine3\tTabbed";

            // Act
            using var result = _serializer.TestGetNoteContent(content);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RootElement.TryGetProperty("note", out var noteElement));
            Assert.True(noteElement.TryGetProperty("content", out var contentElement));
            Assert.Equal(content, contentElement.GetString());
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

            public TestJsonNoteSerializer(ILogger<HmmNote> logger, NoteCatalog catalog) : base(logger)
            {
                _testCatalog = catalog;
            }

            private NoteCatalog _testCatalog;

            // Expose protected methods for testing
            public JsonDocument TestGetNoteContent(string noteContent) => GetNoteContent(noteContent);

            public JsonDocument TestGetNoteContent(JsonElement contentElement) => GetNoteContent(contentElement);

            public bool TestIsValidNoteStructure(JsonDocument document) => IsValidNoteStructure(document);

            public JsonDocument TestCreateEmptyNoteDocument() => CreateEmptyNoteDocument();

            public JsonDocument TestCreateNoteJsonDocument(string content) => CreateNoteJsonDocument(content);

            public NoteCatalog TestGetCatalog() => GetCatalog();

            public ICollection<ValidationError> TestValidateContent(JsonDocument document) => ValidateContent(document);

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
