using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// A cheatsheet card in API responses. Property names are PascalCase - the
    /// API's Newtonsoft formatter is registered without a camel-case contract
    /// resolver, unlike the camelCase note content underneath.
    /// </summary>
    public class ApiCheatsheet : ApiEntity
    {
        /// <summary>Stable card id. Also the route id and the note subject suffix.</summary>
        public string Id { get; set; } = string.Empty;

        public int SchemaVersion { get; set; } = 1;

        public string Title { get; set; } = string.Empty;

        public string WalletGroup { get; set; } = "Ungrouped";

        public IList<string> Tags { get; set; } = new List<string>();

        public string TemplateId { get; set; } = "blank";

        /// <summary>Stored and returned verbatim; the server never acts on it.</summary>
        public bool Protected { get; set; }

        public IList<ApiCheatsheetRow> Rows { get; set; } = new List<ApiCheatsheetRow>();

        /// <summary>Card fields this API version does not model, inlined on the wire.</summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; } = new Dictionary<string, JToken>();

        public void CreateLinks(ResultExecutingContext context, LinkGenerator linkGen)
        {
            var id = Id;
            Links =
            [
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetCheatsheetById", new { id }),
                    Rel = "self",
                    Method = "GET"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateCheatsheet", new { id }),
                    Rel = "update_cheatsheet",
                    Method = "PUT"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "DeleteCheatsheet", new { id }),
                    Rel = "delete_cheatsheet",
                    Method = "DELETE"
                }
            ];
        }
    }
}
