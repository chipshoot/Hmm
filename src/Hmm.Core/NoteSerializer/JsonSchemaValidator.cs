using Hmm.Utility.Misc;
using NJsonSchema;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Core.NoteSerializer
{
    /// <summary>
    /// Helper class for validating JSON content against JSON Schema.
    /// Provides schema validation with error/warning reporting via ProcessingResult.
    /// </summary>
    public class JsonSchemaValidator
    {
        private readonly ProcessingResult _processingResult;
        private JsonSchema _schema;

        public JsonSchemaValidator(ProcessingResult processingResult)
        {
            ArgumentNullException.ThrowIfNull(processingResult);
            _processingResult = processingResult;
        }

        /// <summary>
        /// Loads and caches a JSON Schema from the provided schema string.
        /// </summary>
        /// <param name="schemaText">The JSON Schema as a string.</param>
        public void LoadSchema(string schemaText)
        {
            if (string.IsNullOrEmpty(schemaText))
            {
                _schema = null;
                return;
            }

            try
            {
                _schema = JsonSchema.FromJsonAsync(schemaText).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _processingResult.WrapException(ex);
                _schema = null;
            }
        }

        /// <summary>
        /// Validates JSON content against the loaded schema.
        /// Adds validation errors and warnings to ProcessingResult.
        /// </summary>
        /// <param name="jsonContent">The JSON content to validate.</param>
        /// <returns>True if validation passed (or no schema loaded), false otherwise.</returns>
        public bool ValidateContent(string jsonContent)
        {
            if (_schema == null || string.IsNullOrEmpty(jsonContent))
            {
                return true; // No schema or content to validate
            }

            try
            {
                // Validate the JSON content against the schema
                var errors = _schema.Validate(jsonContent);

                if (errors == null || !errors.Any())
                {
                    return true;
                }

                // Add validation errors to ProcessingResult
                foreach (var error in errors)
                {
                    var errorMessage = $"JSON Schema validation error at {error.Path}: {error.Kind} - {error.ToString()}";

                    // Consider critical errors as errors, others as warnings
                    if (error.Kind == NJsonSchema.Validation.ValidationErrorKind.NoAdditionalPropertiesAllowed ||
                        error.Kind == NJsonSchema.Validation.ValidationErrorKind.PropertyRequired)
                    {
                        _processingResult.AddErrorMessage(errorMessage, logWarning: false);
                    }
                    else
                    {
                        _processingResult.AddWaningMessage(errorMessage, logWarning: true);
                    }
                }

                return false;
            }
            catch (JsonException ex)
            {
                _processingResult.AddErrorMessage($"Invalid JSON format: {ex.Message}", logWarning: false);
                return false;
            }
            catch (Exception ex)
            {
                _processingResult.WrapException(ex);
                return false;
            }
        }

        /// <summary>
        /// Validates a JsonDocument against the loaded schema.
        /// </summary>
        /// <param name="jsonDocument">The JsonDocument to validate.</param>
        /// <returns>True if validation passed, false otherwise.</returns>
        public bool ValidateDocument(JsonDocument jsonDocument)
        {
            if (jsonDocument == null)
            {
                return true;
            }

            try
            {
                var jsonText = jsonDocument.RootElement.GetRawText();
                return ValidateContent(jsonText);
            }
            catch (Exception ex)
            {
                _processingResult.WrapException(ex);
                return false;
            }
        }
    }
}
