using Hmm.Utility.Currency;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hmm.Utility.Json
{
    /// <summary>
    /// System.Text.Json converter for Money value objects.
    /// Serializes Money as JSON: { "value": 50.00, "currencyCode": "CAD" }
    /// </summary>
    public class MoneyJsonConverter : JsonConverter<Money>
    {
        public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Expected StartObject token, got {reader.TokenType}");
            }

            double value = 0;
            CurrencyCodeType currencyCode = CurrencyCodeType.Cad;
            bool hasValue = false;
            bool hasCurrencyCode = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (!hasValue)
                    {
                        throw new JsonException("Missing required property 'value' in Money JSON");
                    }
                    if (!hasCurrencyCode)
                    {
                        throw new JsonException("Missing required property 'currencyCode' in Money JSON");
                    }
                    return new Money(value, currencyCode);
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

                    case "currencycode":
                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw new JsonException($"Expected String token for 'currencyCode', got {reader.TokenType}");
                        }
                        string currencyCodeStr = reader.GetString();
                        if (!Enum.TryParse(currencyCodeStr, ignoreCase: true, out currencyCode))
                        {
                            throw new JsonException($"Invalid currency code: {currencyCodeStr}");
                        }
                        hasCurrencyCode = true;
                        break;

                    default:
                        // Skip unknown properties
                        reader.Skip();
                        break;
                }
            }

            throw new JsonException("Unexpected end of JSON while reading Money");
        }

        public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);

            writer.WriteStartObject();
            writer.WriteNumber("value", value.InternalAmount);
            writer.WriteString("currencyCode", value.Currency.ToString());
            writer.WriteEndObject();
        }
    }
}
