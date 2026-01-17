using Hmm.Core.NoteSerializer;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
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

        public override Task<ProcessingResult<HmmNote>> GetNote(in T entity)
        {
            if (entity == null)
            {
                return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                    "Null entity found when trying to serialize entity to note",
                    ErrorCategory.NotFound));
            }

            var subject = GetSubject(entity);
            var content = GetNoteSerializationText(entity);

            if (string.IsNullOrEmpty(content))
            {
                return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                    "Failed to serialize entity content to JSON",
                    ErrorCategory.MappingError));
            }

            var note = new HmmNote
            {
                Id = entity.Id,
                Subject = subject,
                Content = content,
                Catalog = Catalog
            };

            return Task.FromResult(ProcessingResult<HmmNote>.Ok(note));
        }

        /// <summary>
        /// Gets the subject string for the entity based on its type.
        /// </summary>
        protected virtual string GetSubject(T entity)
        {
            return entity switch
            {
                AutomobileInfo => AutomobileConstant.AutoMobileRecordSubject,
                GasDiscount => AutomobileConstant.GasDiscountRecordSubject,
                GasLog log => GasLog.GetNoteSubject(log.AutomobileId),
                GasStation => AutomobileConstant.GasStationRecordSubject,
                _ => typeof(T).Name
            };
        }

        /// <summary>
        /// Extracts and parses the entity JSON from the note content.
        /// Returns the root JSON element and the entity-specific content.
        /// </summary>
        /// <param name="note">The note containing JSON content.</param>
        /// <param name="entityName">The expected entity name in the JSON (e.g., "gasLog", "automobile").</param>
        /// <returns>Tuple of (entityElement, fullDocument, errorMessage) or (null, null, errorMessage) if parsing fails.</returns>
        protected (JsonElement? entityElement, JsonDocument document, string error) GetEntityRoot(HmmNote note, string entityName)
        {
            if (note == null || string.IsNullOrEmpty(entityName))
            {
                return (null, null, "Note or entity name is null");
            }

            try
            {
                var noteContent = note.Content;
                if (string.IsNullOrEmpty(noteContent))
                {
                    return (null, null, "Empty note content found");
                }

                var document = JsonDocument.Parse(noteContent);

                // Validate against schema if available
                ValidateContent(document);

                // Expected structure: { "note": { "content": { "entityName": { ... } } } }
                if (!document.RootElement.TryGetProperty("note", out var noteElement))
                {
                    document.Dispose();
                    return (null, null, "Missing 'note' element in JSON");
                }

                if (!noteElement.TryGetProperty("content", out var contentElement))
                {
                    document.Dispose();
                    return (null, null, "Missing 'content' element in note JSON");
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
                    document.Dispose();
                    return (null, null, $"Missing '{entityName}' element in content JSON");
                }

                return (entityElement, document, null);
            }
            catch (JsonException ex)
            {
                return (null, null, $"Invalid JSON format: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (null, null, $"Error parsing JSON: {ex.Message}");
            }
        }

        // Note: Helper methods GetStringProperty, GetIntProperty, GetDateTimeProperty, GetBoolProperty, GetDoubleProperty
        // are inherited from DefaultJsonNoteSerializer<T>
    }
}
