using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Json;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace Hmm.Core.NoteSerializer
{
    /// <summary>
    /// Base JSON serializer for note entities.
    /// Provides JSON serialization infrastructure parallel to DefaultXmlNoteSerializer.
    /// </summary>
    /// <typeparam name="T">The entity type to serialize.</typeparam>
    public class DefaultJsonNoteSerializer<T> : NoteSerializerBase<T>
    {
        private JsonSchemaValidator _validator;
        private NoteCatalog _catalog;
        protected JsonSerializerOptions JsonOptions { get; }

        public DefaultJsonNoteSerializer(ILogger<T> logger) : base(logger)
        {
            JsonOptions = JsonSerializerOptionsFactory.DefaultOptions;
        }

        /// <summary>
        /// Gets the JSON schema validator, initialized on first access.
        /// </summary>
        private JsonSchemaValidator Validator
        {
            get
            {
                if (_validator == null)
                {
                    _validator = new JsonSchemaValidator(ProcessResult);
                    if (Catalog != null && !string.IsNullOrEmpty(Catalog.Schema))
                    {
                        _validator.LoadSchema(Catalog.Schema);
                    }
                }
                return _validator;
            }
        }

        /// <summary>
        /// Gets the note catalog, initialized on first access.
        /// </summary>
        protected NoteCatalog Catalog => _catalog ??= GetCatalog();

        public override ProcessingResult<T> GetEntity(HmmNote note)
        {
            if (note == null)
            {
                return ProcessingResult<T>.Fail("Null note found when trying to de-serialize entity from note", ErrorCategory.NotFound);
            }

            return default;
        }

        public override ProcessingResult<HmmNote> GetNote(in T entity)
        {
            if (entity == null)
            {
                return ProcessingResult<HmmNote>.Fail("Null entity found when trying to serialize entity to note", ErrorCategory.NotFound);
            }

            try
            {
                switch (entity)
                {
                    // If entity is HmmNote or its child
                    case HmmNote hmmNote:
                        {
                            var note = new HmmNote
                            {
                                Subject = hmmNote.Subject,
                                Content = GetNoteSerializationText(entity),
                                CreateDate = hmmNote.CreateDate,
                                Description = hmmNote.Description,
                                Author = hmmNote.Author,
                                Catalog = hmmNote.Catalog
                            };
                            return ProcessingResult<HmmNote>.Ok(note);
                        }
                    default:
                        return ProcessingResult<HmmNote>.Fail("Unsupported entity type for serialization",
                            ErrorCategory.MappingError);
                }
            }
            catch (Exception ex)
            {
                return ProcessingResult<HmmNote>.FromException(ex);
            }
        }

        /// <summary>
        /// Converts entity to JSON string representation.
        /// Override this method to provide entity-specific JSON serialization.
        /// </summary>
        /// <param name="entity">The entity to serialize.</param>
        /// <returns>JSON string representation of the entity.</returns>
        public virtual string GetNoteSerializationText(T entity)
        {
            if (entity == null)
            {
                return string.Empty;
            }

            if (entity is not HmmNote note)
            {
                return string.Empty;
            }

            var jsonDocument = GetNoteContent(note.Content);
            return jsonDocument.RootElement.GetRawText();
        }

        /// <summary>
        /// Creates a JSON document with the note structure from plain text content.
        /// </summary>
        /// <param name="noteContent">The note content as a string.</param>
        /// <returns>JsonDocument containing the note structure.</returns>
        protected virtual JsonDocument GetNoteContent(string noteContent)
        {
            if (string.IsNullOrEmpty(noteContent))
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

            try
            {
                // Try to parse as existing JSON document
                var jsonDocument = JsonDocument.Parse(noteContent);

                // Validate against schema if available
                ValidateContent(jsonDocument);

                // Check if it's already a valid note JSON structure
                if (!ProcessResult.HasWarning && !ProcessResult.HasInfo)
                {
                    return jsonDocument;
                }

                // If validation had issues, wrap it properly
                return CreateNoteJsonDocument(noteContent);
            }
            catch (JsonException)
            {
                // Not valid JSON, treat as plain text content
                ProcessResult.AddErrorMessage("Content is not valid JSON, storing as plain text", logWarning: false, logToTrace: false);
                return CreateNoteJsonDocument(noteContent);
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return CreateNoteJsonDocument(noteContent);
            }
        }

        /// <summary>
        /// Creates a JSON document with the note structure from a JsonElement content.
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
                ProcessResult.WrapException(ex);
                var emptyNoteJson = JsonSerializer.Serialize(new
                {
                    note = new
                    {
                        content = string.Empty
                    }
                }, JsonOptions);
                return JsonDocument.Parse(emptyNoteJson);
            }
        }

        /// <summary>
        /// Gets the note catalog for this serializer.
        /// Override to provide catalog-specific logic.
        /// </summary>
        /// <returns>The note catalog.</returns>
        protected virtual NoteCatalog GetCatalog()
        {
            return new NoteCatalog();
        }

        /// <summary>
        /// Validates JSON content against the loaded schema.
        /// </summary>
        /// <param name="jsonDocument">The JSON document to validate.</param>
        protected void ValidateContent(JsonDocument jsonDocument)
        {
            if (jsonDocument == null || Catalog == null || string.IsNullOrEmpty(Catalog.Schema))
            {
                return;
            }

            Validator.ValidateDocument(jsonDocument);
        }

        /// <summary>
        /// Creates a properly structured note JSON document.
        /// </summary>
        /// <param name="content">The content to wrap in note structure.</param>
        /// <returns>JsonDocument with note structure.</returns>
        private JsonDocument CreateNoteJsonDocument(string content)
        {
            var noteJson = JsonSerializer.Serialize(new
            {
                note = new
                {
                    content = content
                }
            }, JsonOptions);
            return JsonDocument.Parse(noteJson);
        }
    }
}