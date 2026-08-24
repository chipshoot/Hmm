using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// Payload for POST /v1/cheatsheets. <see cref="Id"/> is optional: the
    /// client normally mints the card's v4 UUID, and the server fills one in
    /// when it is absent.
    /// </summary>
    public class ApiCheatsheetForCreate
    {
        public string Id { get; set; }

        public int SchemaVersion { get; set; } = 1;

        public string Title { get; set; } = string.Empty;

        public string WalletGroup { get; set; } = "Ungrouped";

        public IList<string> Tags { get; set; } = new List<string>();

        public string TemplateId { get; set; } = "blank";

        public bool Protected { get; set; }

        public IList<ApiCheatsheetRow> Rows { get; set; } = new List<ApiCheatsheetRow>();

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; } = new Dictionary<string, JToken>();
    }
}
