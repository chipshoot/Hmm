using Hmm.Core.NoteSerializer;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;

namespace Hmm.Automobile.NoteSerialize
{
    /// <summary>
    /// Base JSON serializer for Automobile domain entities.
    /// Provides common JSON serialization functionality for AutomobileBase entities.
    /// Parallel to EntityXmlNoteSerializeBase for XML serialization.
    /// </summary>
    /// <typeparam name="T">The Automobile entity type to serialize.</typeparam>
    public class EntityJsonNoteSerializeBase<T> : DefaultJsonNoteSerializer<T> where T : AutomobileBase
    {
        protected EntityJsonNoteSerializeBase(ILogger<T> logger) : base(logger)
        {
        }

        public override HmmNote GetNote(in T entity)
        {
            if (entity == null)
            {
                ProcessResult.AddWaningMessage("Null entity found when trying to serialize entity to note", true);
                return null;
            }

            var subject = entity.GetSubject();
            var note = new DomainEntity.HmmNote
            {
                Id = entity.Id,
                Subject = subject,
                Content = GetNoteSerializationText(entity),
                Catalog = Catalog
            };

            return note;
        }

        /// <summary>
        /// Extracts and parses the entity JSON from the note content.
        /// Returns the root JSON element and the entity-specific content.
        /// </summary>
        /// <param name="note">The note containing JSON content.</param>
        /// <param name="entityName">The expected entity name in the JSON (e.g., "gasLog", "automobile").</param>
        /// <returns>Tuple of (entityElement, fullDocument) or (null, null) if parsing fails.</returns>
        protected (JsonElement? entityElement, JsonDocument document) GetEntityRoot(DomainEntity.HmmNote note, string entityName)
        {
            if (note == null || string.IsNullOrEmpty(entityName))
            {
                return (null, null);
            }

            try
            {
                var noteContent = note.Content;
                if (string.IsNullOrEmpty(noteContent))
                {
                    ProcessResult.AddErrorMessage("Empty note content found", logWarning: true);
                    return (null, null);
                }

                var document = JsonDocument.Parse(noteContent);

                // Validate against schema if available
                ValidateContent(document);

                // Expected structure: { "note": { "content": { "entityName": { ... } } } }
                if (!document.RootElement.TryGetProperty("note", out var noteElement))
                {
                    ProcessResult.AddErrorMessage($"Missing 'note' root element in JSON", logWarning: true);
                    return (null, null);
                }

                if (!noteElement.TryGetProperty("content", out var contentElement))
                {
                    ProcessResult.AddErrorMessage($"Missing 'content' element in note JSON", logWarning: true);
                    return (null, null);
                }

                // Use case-insensitive property lookup for entity name
                JsonElement entityElement;
                var found = contentElement.TryGetProperty(entityName, out entityElement);

                if (!found)
                {
                    // Try camelCase version
                    var camelCaseEntityName = char.ToLowerInvariant(entityName[0]) + entityName.Substring(1);
                    found = contentElement.TryGetProperty(camelCaseEntityName, out entityElement);
                }

                if (!found)
                {
                    ProcessResult.AddErrorMessage($"Missing '{entityName}' element in content JSON", logWarning: true);
                    return (null, null);
                }

                return (entityElement, document);
            }
            catch (JsonException ex)
            {
                ProcessResult.AddErrorMessage($"Invalid JSON format: {ex.Message}", logWarning: false);
                return (null, null);
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return (null, null);
            }
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
    }
}
