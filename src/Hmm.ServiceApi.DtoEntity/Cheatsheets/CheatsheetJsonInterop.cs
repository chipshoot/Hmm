using System;
using System.Collections.Generic;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// Bridges the two JSON stacks this feature straddles: note content is
    /// System.Text.Json (JsonElement), the API wire format is Newtonsoft
    /// (JToken). Conversion goes through raw text, so nothing is interpreted
    /// and nothing is lost.
    /// </summary>
    public static class CheatsheetJsonInterop
    {
        public static JToken ToJToken(JsonElement element) => JToken.Parse(element.GetRawText());

        public static JsonElement ToJsonElement(JToken token)
        {
            using var document = JsonDocument.Parse(token.ToString(Formatting.None));
            // Clone: the document is disposed on the way out of this method.
            return document.RootElement.Clone();
        }

        public static IDictionary<string, JToken> ToJTokens(IDictionary<string, JsonElement> source)
        {
            var result = new Dictionary<string, JToken>(StringComparer.Ordinal);
            if (source == null)
            {
                return result;
            }

            foreach (var pair in source)
            {
                result[pair.Key] = ToJToken(pair.Value);
            }

            return result;
        }

        public static IDictionary<string, JsonElement> ToJsonElements(IDictionary<string, JToken> source)
        {
            var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            if (source == null)
            {
                return result;
            }

            foreach (var pair in source)
            {
                result[pair.Key] = ToJsonElement(pair.Value);
            }

            return result;
        }
    }
}
