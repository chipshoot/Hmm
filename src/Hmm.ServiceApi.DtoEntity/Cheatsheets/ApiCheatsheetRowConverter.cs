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
        private static readonly HashSet<string> KnownKeys = new(StringComparer.Ordinal)
        {
            nameof(ApiCheatsheetRow.Label),
            nameof(ApiCheatsheetRow.ValueAction),
            nameof(ApiCheatsheetRow.OpenSource),
            nameof(ApiCheatsheetRow.Source)
        };

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
            var row = new ApiCheatsheetRow
            {
                Label = source.Value<string>(nameof(ApiCheatsheetRow.Label)) ?? string.Empty,
                ValueAction = source.Value<string>(nameof(ApiCheatsheetRow.ValueAction)) ?? "none",
                OpenSource = source.Value<bool?>(nameof(ApiCheatsheetRow.OpenSource)) ?? true
            };

            if (source[nameof(ApiCheatsheetRow.Source)] is JObject sourceObject)
            {
                row.Source = sourceObject.ToObject<ApiCheatsheetSource>(serializer);
            }

            foreach (var property in source.Properties())
            {
                if (KnownKeys.Contains(property.Name))
                {
                    continue;
                }

                row.ExtraFields[property.Name] = property.Value;
            }

            return row;
        }
    }
}
