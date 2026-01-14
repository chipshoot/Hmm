using Hmm.Utility.MeasureUnit;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hmm.Utility.Json
{
    /// <summary>
    /// System.Text.Json converter for Dimension value objects.
    /// Serializes Dimension as JSON: { "value": 100.5, "unit": "Kilometre" }
    /// </summary>
    public class DimensionJsonConverter : JsonConverter<Dimension>
    {
        public override Dimension Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected StartObject token, got {reader.TokenType}");
            }

            double value = 0;
            DimensionUnit unit = DimensionUnit.Millimetre;
            bool hasValue = false;
            bool hasUnit = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (!hasValue)
                    {
                        throw new JsonException("Missing required property 'value' in Dimension JSON");
                    }
                    if (!hasUnit)
                    {
                        throw new JsonException("Missing required property 'unit' in Dimension JSON");
                    }
                    return new Dimension(value, unit);
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException($"Expected PropertyName token, got {reader.TokenType}");
                }

                string propertyName = reader.GetString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "value":
                        if (reader.TokenType != JsonTokenType.Number)
                        {
                            throw new JsonException($"Expected Number token for 'value', got {reader.TokenType}");
                        }
                        value = reader.GetDouble();
                        hasValue = true;
                        break;

                    case "unit":
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw new JsonException($"Expected String token for 'unit', got {reader.TokenType}");
                        }
                        string unitStr = reader.GetString();
                        if (!Enum.TryParse(unitStr, ignoreCase: true, out unit))
                        {
                            throw new JsonException($"Invalid dimension unit: {unitStr}");
                        }
                        hasUnit = true;
                        break;

                    default:
                        // Skip unknown properties
                        reader.Skip();
                        break;
                }
            }

            throw new JsonException("Unexpected end of JSON while reading Dimension");
        }

        public override void Write(Utf8JsonWriter writer, Dimension value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteStartObject();
            writer.WriteNumber("value", value.Value);
            writer.WriteString("unit", value.Unit.ToString());
            writer.WriteEndObject();
        }
    }
}
