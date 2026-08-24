using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// Keeps rows the API cannot model on the wire unchanged.
    ///
    /// A POCO cannot represent a row that is a string, a number or null, but
    /// the client stores exactly such rows rather than destroying data it could
    /// not parse. When <see cref="ApiCheatsheetRow.Raw"/> is set this converter
    /// writes that token instead of an object, and on read anything that is not
    /// a JSON object is captured into Raw.
    /// </summary>
    public class ApiCheatsheetRowConverter : JsonConverter<ApiCheatsheetRow>
    {

        public override void WriteJson(JsonWriter writer, ApiCheatsheetRow value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value.Raw != null)
            {
                value.Raw.WriteTo(writer);
                return;
            }

            var row = new JObject
            {
                [nameof(ApiCheatsheetRow.Label)] = value.Label ?? string.Empty,
                [nameof(ApiCheatsheetRow.ValueAction)] = value.ValueAction ?? "none",
                [nameof(ApiCheatsheetRow.OpenSource)] = value.OpenSource
            };

            if (value.Source != null)
            {
                row[nameof(ApiCheatsheetRow.Source)] = JObject.FromObject(value.Source, serializer);
            }

            if (value.ExtraFields != null)
            {
                foreach (var extra in value.ExtraFields)
                {
                    // Extras last: a preserved original always beats a default.
                    row[extra.Key] = extra.Value;
                }
            }

            row.WriteTo(writer);
        }

        public override ApiCheatsheetRow ReadJson(
            JsonReader reader,
            Type objectType,
            ApiCheatsheetRow existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            if (token.Type != JTokenType.Object)
            {
                return new ApiCheatsheetRow { Raw = token };
            }

            var source = (JObject)token;

            // Consume a known key ONLY when it carries the expected JSON type.
            // Anything else falls through to ExtraFields below and is written
            // back verbatim. Reading these with JToken.Value<T>() instead threw
            // InvalidCastException on an object/array and FormatException on an
            // unparsable scalar, and silently coerced a number to its string
            // form - so a row from a newer client schema was either rejected
            // with a 500 or quietly rewritten. This mirrors ReadString/ReadBool
            // in CheatsheetJsonNoteSerialize, which already gets this right one
            // layer down.
            var consumed = new HashSet<string>(StringComparer.Ordinal);
            var row = new ApiCheatsheetRow
            {
                Label = ReadString(source, nameof(ApiCheatsheetRow.Label), consumed) ?? string.Empty,
                ValueAction = ReadString(source, nameof(ApiCheatsheetRow.ValueAction), consumed) ?? "none",
                OpenSource = ReadBool(source, nameof(ApiCheatsheetRow.OpenSource), true, consumed)
            };

            if (source[nameof(ApiCheatsheetRow.Source)] is JObject sourceObject)
            {
                consumed.Add(nameof(ApiCheatsheetRow.Source));
                row.Source = sourceObject.ToObject<ApiCheatsheetSource>(serializer);
            }

            foreach (var property in source.Properties())
            {
                // Keyed on what was actually consumed, NOT on KnownKeys: a
                // mistyped known field is unconsumed, so it is preserved here
                // rather than dropped on the floor.
                if (consumed.Contains(property.Name))
                {
                    continue;
                }

                row.ExtraFields[property.Name] = property.Value;
            }

            return row;
        }

        private static string ReadString(JObject source, string name, HashSet<string> consumed)
        {
            if (source[name] is JValue value && value.Type == JTokenType.String)
            {
                consumed.Add(name);
                return (string)value.Value;
            }

            return null;
        }

        private static bool ReadBool(JObject source, string name, bool defaultValue, HashSet<string> consumed)
        {
            if (source[name] is JValue value && value.Type == JTokenType.Boolean)
            {
                consumed.Add(name);
                return (bool)value.Value;
            }

            return defaultValue;
        }
    }
}
