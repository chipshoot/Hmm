using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Json;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using NJsonSchema.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Core.NoteSerializer
{
    /// <summary>
    /// Base JSON serializer for note entities.
    /// Provides JSON serialization infrastructure parallel to DefaultXmlNoteSerializer.
    /// This class handles the serialization and deserialization of entities to/from JSON stored in HmmNote.Content.
    ///
    /// Design Pattern:
    /// - Uses ProcessingResult pattern for explicit error handling
    /// - Provides JSON schema validation support via NoteCatalog
    /// - Separates concerns: base class handles structure, derived classes handle entity-specific logic
    /// - Thread-safe through immutable ProcessingResult pattern
    ///
    /// Usage:
    /// - Derive from this class for domain-specific entities (e.g., GasLog, Automobile)
    /// - Override GetEntity to deserialize JSON to your entity type
    /// - Override GetNoteSerializationText to serialize your entity to JSON
    /// - Override GetCatalog to provide entity-specific schema validation
    /// </summary>
    /// <typeparam name="T">The entity type to serialize.</typeparam>
    public class DefaultJsonNoteSerializer<T>(ILogger<T> logger) : NoteSerializerBase<T>(logger)
    {
        private JsonSchemaValidator _validator;
        private NoteCatalog _catalog;
        private JsonSchema _schema;

        /// <summary>
        /// Gets the JSON serializer options used for serialization/deserialization.
        /// Uses the default options from JsonSerializerOptionsFactory for consistency across the application.
        /// </summary>
        protected JsonSerializerOptions JsonOptions { get; } = JsonSerializerOptionsFactory.DefaultOptions;

        /// <summary>
        /// Gets the JSON schema validator, initialized on first access.
        /// Lazy-loaded to avoid unnecessary schema parsing when validation is not needed.
        /// </summary>
        private JsonSchemaValidator Validator
        {
            get
            {
                if (_validator == null)
                {
                    _validator = new JsonSchemaValidator();
                    if (Catalog != null && !string.IsNullOrEmpty(Catalog.Schema))
                    {
                        _schema = JsonSchema.FromJsonAsync(Catalog.Schema).GetAwaiter().GetResult();
                    }
                }
                return _validator;
            }
        }

        /// <summary>
        /// Gets the note catalog, initialized on first access.
        /// The catalog contains the JSON schema used for validation.
        /// </summary>
        protected NoteCatalog Catalog => _catalog ??= GetCatalog();

        /// <summary>
        /// Deserializes a HmmNote to an entity of type T.
        ///
        /// Base Implementation:
        /// - Returns the note cast as T if T is HmmNote
        /// - Otherwise returns NotFound result
        ///
        /// Override this method in derived classes to:
        /// - Parse JSON from note.Content
        /// - Validate the structure
        /// - Create and populate your entity type
        /// - Return ProcessingResult.Ok(entity) on success
        /// - Return ProcessingResult.Fail/Invalid/NotFound on errors
        /// </summary>
        /// <param name="note">The note containing JSON content.</param>
        /// <returns>ProcessingResult containing the deserialized entity or error information.</returns>
        public override Task<ProcessingResult<T>> GetEntity(HmmNote note)
        {
            if (note == null)
            {
                return Task.FromResult(ProcessingResult<T>.Fail(
                    "Null note found when trying to deserialize entity from note",
                    ErrorCategory.NotFound));
            }

            try
            {
                // If T is HmmNote, just return the note itself
                if (typeof(T) == typeof(HmmNote) || typeof(T).IsAssignableFrom(typeof(HmmNote)))
                {
                    if (note is T typedNote)
                    {
                        return Task.FromResult(ProcessingResult<T>.Ok(typedNote));
                    }
                }

                // For other entity types, derived classes must override this method
                // Base implementation cannot deserialize without knowing the entity structure
                return Task.FromResult(ProcessingResult<T>.Fail(
                    $"GetEntity must be overridden to deserialize {typeof(T).Name} from JSON. Base implementation only supports HmmNote.",
                    ErrorCategory.MappingError));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error deserializing entity from note");
                return Task.FromResult(ProcessingResult<T>.FromException(ex));
            }
        }

        /// <summary>
        /// Serializes an entity of type T to a HmmNote with JSON content.
        ///
        /// Base Implementation:
        /// - If entity is HmmNote, serializes its content to proper JSON structure
        /// - For other types, returns error (derived classes must override)
        ///
        /// Override this method in derived classes to:
        /// - Extract data from your entity
        /// - Call GetNoteSerializationText to generate JSON
        /// - Create HmmNote with appropriate Subject, Content, Catalog
        /// - Return ProcessingResult.Ok(note)
        /// </summary>
        /// <param name="entity">The entity to serialize.</param>
        /// <returns>ProcessingResult containing the HmmNote or error information.</returns>
        public override Task<ProcessingResult<HmmNote>> GetNote(in T entity)
        {
            if (entity == null)
            {
                return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                    "Null entity found when trying to serialize entity to note",
                    ErrorCategory.NotFound));
            }

            try
            {
                // Handle HmmNote entities
                if (entity is HmmNote hmmNote)
                {
                    var content = GetNoteSerializationText(entity);

                    // Check for serialization errors
                    if (string.IsNullOrEmpty(content))
                    {
                        return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                            "Failed to serialize note content to JSON",
                            ErrorCategory.MappingError));
                    }

                    var note = new HmmNote
                    {
                        Id = hmmNote.Id,
                        Subject = hmmNote.Subject,
                        Content = content,
                        CreateDate = hmmNote.CreateDate,
                        LastModifiedDate = hmmNote.LastModifiedDate,
                        Description = hmmNote.Description,
                        Author = hmmNote.Author,
                        Catalog = hmmNote.Catalog ?? Catalog
                    };

                    return Task.FromResult(ProcessingResult<HmmNote>.Ok(note));
                }

                // For other entity types, derived classes must override
                return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                    $"GetNote must be overridden to serialize {typeof(T).Name} to JSON. Base implementation only supports HmmNote.",
                    ErrorCategory.MappingError));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error serializing entity to note");
                return Task.FromResult(ProcessingResult<HmmNote>.FromException(ex));
            }
        }

        /// <summary>
        /// Converts entity to JSON string representation.
        ///
        /// Base Implementation:
        /// - For HmmNote: wraps content in proper JSON structure
        /// - For other types: returns empty string (derived classes must override)
        ///
        /// Override this method in derived classes to:
        /// - Serialize your entity-specific properties to JSON
        /// - Use the proper JSON structure: { "note": { "content": { "entityName": {...} } } }
        /// - Handle nested objects (Money, Dimension, Volume, etc.)
        /// - Return the complete JSON string
        /// </summary>
        /// <param name="entity">The entity to serialize.</param>
        /// <returns>JSON string representation of the entity.</returns>
        public virtual string GetNoteSerializationText(T entity)
        {
            if (entity == null)
            {
                Logger?.LogWarning("Null entity provided for serialization");
                return string.Empty;
            }

            try
            {
                // If entity is HmmNote, extract and format its content
                if (entity is HmmNote note)
                {
                    using var jsonDocument = GetNoteContent(note.Content);
                    return jsonDocument.RootElement.GetRawText();
                }

                // For non-HmmNote entities, derived classes must override
                Logger?.LogWarning(
                    "GetNoteSerializationText called on non-HmmNote entity type {EntityType}. " +
                    "Derived class should override this method.",
                    typeof(T).Name);
                return string.Empty;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error serializing entity to JSON text");
                return string.Empty;
            }
        }

        /// <summary>
        /// Creates a JSON document with the note structure from plain text content.
        ///
        /// This method handles three scenarios:
        /// 1. Empty content: Returns empty note structure
        /// 2. Valid JSON: Validates and returns (or wraps if invalid structure)
        /// 3. Plain text: Wraps in note structure
        ///
        /// Expected JSON structure:
        /// {
        ///   "note": {
        ///     "content": "..." or {...}
        ///   }
        /// }
        /// </summary>
        /// <param name="noteContent">The note content as a string.</param>
        /// <returns>JsonDocument containing the note structure.</returns>
        protected virtual JsonDocument GetNoteContent(string noteContent)
        {
            if (string.IsNullOrEmpty(noteContent))
            {
                return CreateEmptyNoteDocument();
            }

            try
            {
                // Try to parse as existing JSON document
                var jsonDocument = JsonDocument.Parse(noteContent);

                // Validate structure
                if (!IsValidNoteStructure(jsonDocument))
                {
                    Logger?.LogWarning("Content is valid JSON but not in expected note structure, wrapping it");
                    jsonDocument.Dispose();
                    return CreateNoteJsonDocument(noteContent);
                }

                // Validate against schema if available
                var errors = ValidateContent(jsonDocument);
                if (errors.Count > 0)
                {
                    var errorMessages = string.Join("; ", errors.Select(e => e.ToString()));
                    Logger?.LogWarning("JSON schema validation failed: {Errors}", errorMessages);

                    // Still return the document even with validation errors
                    // Validation errors are warnings, not fatal errors
                }

                return jsonDocument;
            }
            catch (JsonException ex)
            {
                // Not valid JSON, treat as plain text content
                Logger?.LogInformation("Content is not valid JSON, storing as plain text: {Error}", ex.Message);
                return CreateNoteJsonDocument(noteContent);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Unexpected error parsing note content");
                return CreateNoteJsonDocument(noteContent);
            }
        }

        /// <summary>
        /// Creates a JSON document with the note structure from a JsonElement content.
        /// Useful when you already have a parsed JSON element to embed.
        /// </summary>
        /// <param name="contentElement">The content as a JsonElement.</param>
        /// <returns>JsonDocument containing the note structure.</returns>
        protected virtual JsonDocument GetNoteContent(JsonElement contentElement)
        {
            try
            {
                var noteJson = JsonSerializer.Serialize(new
                {
                    note = new
                    {
                        content = contentElement
                    }
                }, JsonOptions);
                return JsonDocument.Parse(noteJson);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error creating note content from JsonElement");
                return CreateEmptyNoteDocument();
            }
        }

        /// <summary>
        /// Asynchronously gets the note catalog for this serializer.
        ///
        /// Override in derived classes to:
        /// - Return entity-specific catalog with JSON schema
        /// - Enable schema validation for your entity type
        /// - Provide metadata about the note type
        /// </summary>
        /// <returns>A task containing the note catalog.</returns>
        protected virtual Task<NoteCatalog> GetCatalogAsync()
        {
            return Task.FromResult(new NoteCatalog
            {
                Name = $"{typeof(T).Name}Catalog",
                Schema = null // No schema validation by default
            });
        }

        /// <summary>
        /// Synchronously gets the note catalog for this serializer.
        /// This method exists for backward compatibility with code that cannot be made async.
        /// Prefer using <see cref="GetCatalogAsync"/> when possible.
        /// </summary>
        /// <returns>The note catalog.</returns>
        protected NoteCatalog GetCatalog()
        {
            return GetCatalogAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Validates JSON content against the loaded schema.
        /// Returns an empty collection if no schema is configured.
        /// </summary>
        /// <param name="jsonDocument">The JSON document to validate.</param>
        /// <returns>Collection of validation errors (empty if valid or no schema).</returns>
        protected ICollection<ValidationError> ValidateContent(JsonDocument jsonDocument)
        {
            if (jsonDocument == null || Catalog == null || string.IsNullOrEmpty(Catalog.Schema))
            {
                return Array.Empty<ValidationError>();
            }

            try
            {
                var jsonString = jsonDocument.RootElement.GetRawText();
                var errors = _schema?.Validate(jsonString);
                return errors ?? Array.Empty<ValidationError>();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error during JSON schema validation");
                return new List<ValidationError>();
            }
        }

        /// <summary>
        /// Checks if the JSON document has the expected note structure.
        /// Expected: { "note": { "content": ... } }
        /// </summary>
        /// <param name="document">The document to check.</param>
        /// <returns>True if structure is valid, false otherwise.</returns>
        protected bool IsValidNoteStructure(JsonDocument document)
        {
            if (document?.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!document.RootElement.TryGetProperty("note", out var noteElement))
            {
                return false;
            }

            if (noteElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            // Content property is required
            return noteElement.TryGetProperty("content", out _);
        }

        /// <summary>
        /// Creates an empty note JSON document.
        /// Structure: { "note": { "content": "" } }
        /// </summary>
        /// <returns>JsonDocument with empty note structure.</returns>
        protected JsonDocument CreateEmptyNoteDocument()
        {
            var emptyNoteJson = JsonSerializer.Serialize(new
            {
                note = new
                {
                    content = string.Empty
                }
            }, JsonOptions);
            return JsonDocument.Parse(emptyNoteJson);
        }

        /// <summary>
        /// Creates a properly structured note JSON document from content string.
        /// Wraps the content in the expected structure: { "note": { "content": content } }
        /// </summary>
        /// <param name="content">The content to wrap in note structure.</param>
        /// <returns>JsonDocument with note structure.</returns>
        protected JsonDocument CreateNoteJsonDocument(string content)
        {
            var noteJson = JsonSerializer.Serialize(new
            {
                note = new
                {
                    content = content ?? string.Empty
                }
            }, JsonOptions);
            return JsonDocument.Parse(noteJson);
        }

        /// <summary>
        /// Helper method to safely get a string property from a JsonElement.
        /// </summary>
        /// <param name="element">The JSON element.</param>
        /// <param name="propertyName">The property name to retrieve.</param>
        /// <param name="defaultValue">Default value if property not found.</param>
        /// <returns>The string value or default.</returns>
        protected string GetStringProperty(JsonElement element, string propertyName, string defaultValue = null)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                return property.GetString() ?? defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Helper method to safely get an integer property from a JsonElement.
        /// </summary>
        /// <param name="element">The JSON element.</param>
        /// <param name="propertyName">The property name to retrieve.</param>
        /// <param name="defaultValue">Default value if property not found.</param>
        /// <returns>The integer value or default.</returns>
        protected int GetIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value))
            {
                return value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Helper method to safely get a DateTime property from a JsonElement.
        /// Supports ISO 8601 format.
        /// </summary>
        /// <param name="element">The JSON element.</param>
        /// <param name="propertyName">The property name to retrieve.</param>
        /// <param name="defaultValue">Default value if property not found.</param>
        /// <returns>The DateTime value or default.</returns>
        protected DateTime GetDateTimeProperty(JsonElement element, string propertyName, DateTime? defaultValue = null)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.TryGetDateTime(out var value))
            {
                return value;
            }
            return defaultValue ?? DateTime.MinValue;
        }

        /// <summary>
        /// Helper method to safely get a boolean property from a JsonElement.
        /// </summary>
        /// <param name="element">The JSON element.</param>
        /// <param name="propertyName">The property name to retrieve.</param>
        /// <param name="defaultValue">Default value if property not found.</param>
        /// <returns>The boolean value or default.</returns>
        protected bool GetBoolProperty(JsonElement element, string propertyName, bool defaultValue = false)
        {
            if (element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
            {
                return property.GetBoolean();
            }
            return defaultValue;
        }

        /// <summary>
        /// Helper method to safely get a double property from a JsonElement.
        /// </summary>
        /// <param name="element">The JSON element.</param>
        /// <param name="propertyName">The property name to retrieve.</param>
        /// <param name="defaultValue">Default value if property not found.</param>
        /// <returns>The double value or default.</returns>
        protected double GetDoubleProperty(JsonElement element, string propertyName, double defaultValue = 0.0)
        {
            if (element.TryGetProperty(propertyName, out var property) && property.TryGetDouble(out var value))
            {
                return value;
            }
            return defaultValue;
        }
    }
}