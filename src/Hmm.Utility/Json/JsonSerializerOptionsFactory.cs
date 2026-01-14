using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hmm.Utility.Json
{
    /// <summary>
    /// Factory class for creating configured JsonSerializerOptions instances.
    /// Provides centralized configuration for JSON serialization throughout the application.
    /// </summary>
    public static class JsonSerializerOptionsFactory
    {
        /// <summary>
        /// Creates a JsonSerializerOptions instance configured for Hmm domain entities.
        /// Includes custom converters for Money, Dimension, and Volume value objects.
        /// </summary>
        /// <param name="writeIndented">If true, JSON will be formatted with indentation for readability. Default is false for production.</param>
        /// <returns>Configured JsonSerializerOptions instance.</returns>
        public static JsonSerializerOptions CreateOptions(bool writeIndented = false)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = writeIndented,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new MoneyJsonConverter(),
                    new DimensionJsonConverter(),
                    new VolumeJsonConverter(),
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            return options;
        }

        /// <summary>
        /// Gets a cached, production-ready JsonSerializerOptions instance.
        /// Uses compact formatting (no indentation) for optimal payload size.
        /// </summary>
        public static JsonSerializerOptions DefaultOptions { get; } = CreateOptions(writeIndented: false);

        /// <summary>
        /// Gets a cached JsonSerializerOptions instance with indented formatting.
        /// Useful for debugging, logging, and human-readable output.
        /// </summary>
        public static JsonSerializerOptions IndentedOptions { get; } = CreateOptions(writeIndented: true);
    }
}
