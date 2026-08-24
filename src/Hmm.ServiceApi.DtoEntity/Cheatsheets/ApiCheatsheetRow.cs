using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// One labelled line of a card. A row may be unbound (Source is null), and
    /// a row this API version cannot model at all is carried whole in
    /// <see cref="Raw"/> - see <see cref="ApiCheatsheetRowConverter"/>.
    /// </summary>
    [JsonConverter(typeof(ApiCheatsheetRowConverter))]
    public class ApiCheatsheetRow
    {
        public string Label { get; set; } = string.Empty;

        /// <summary>"none" | "call" | "map", passed through verbatim.</summary>
        public string ValueAction { get; set; } = "none";

        public bool OpenSource { get; set; } = true;

        /// <summary>Null = unbound.</summary>
        public ApiCheatsheetSource Source { get; set; }

        /// <summary>
        /// Row fields this API version does not model. Note the class-level
        /// <see cref="ApiCheatsheetRowConverter"/> intercepts serialization, so
        /// Newtonsoft never applies its own extension-data handling here - the
        /// converter reads and writes this dictionary itself. The attribute is
        /// kept as a fallback should that converter ever be removed.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; } = new Dictionary<string, JToken>();

        /// <summary>
        /// The whole row, verbatim, when it is not a JSON object. Serialized in
        /// place of the object, so the wire shape is byte-identical to what was
        /// stored.
        /// </summary>
        [JsonIgnore]
        public JToken Raw { get; set; }
    }
}
