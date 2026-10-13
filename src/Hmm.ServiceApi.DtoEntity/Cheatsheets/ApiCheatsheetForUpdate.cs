using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// Payload for PUT /v1/cheatsheets/{id}. The card id comes from the route,
    /// never from the body, so it is absent here - a body id could disagree with
    /// the route and silently re-identify the card.
    /// </summary>
    public class ApiCheatsheetForUpdate
    {
        public int SchemaVersion { get; set; } = 1;

        public string Title { get; set; } = string.Empty;

        public string WalletGroup { get; set; } = "Ungrouped";

        public IList<string> Tags { get; set; } = new List<string>();

        public string TemplateId { get; set; } = "blank";

        public bool Protected { get; set; }

        public IList<ApiCheatsheetRow> Rows { get; set; } = new List<ApiCheatsheetRow>();

        private JToken _style;

        /// <summary>
        /// The card's look, verbatim. This DTO is mapped ONTO the stored card,
        /// so presence matters: a client that predates styles sends no "Style"
        /// key and must not wipe one (<see cref="StyleSpecified"/> stays false),
        /// while "Style": null is a deliberate reset. Newtonsoft calls this
        /// setter only for a key that is present - null included.
        /// </summary>
        public JToken Style
        {
            get => _style;
            set
            {
                _style = value;
                StyleSpecified = true;
            }
        }

        /// <summary>True when the request body carried a "Style" key at all.</summary>
        [JsonIgnore]
        public bool StyleSpecified { get; private set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; } = new Dictionary<string, JToken>();
    }
}
