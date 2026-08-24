using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// A reference to a piece of a note. Addressed by NoteUuid - the
    /// cross-device-stable identity - never by the local int note id.
    /// </summary>
    public class ApiCheatsheetSource
    {
        public string NoteUuid { get; set; } = string.Empty;

        /// <summary>"field" | "section" | "whole", passed through verbatim.</summary>
        public string Kind { get; set; } = "whole";

        /// <summary>field -&gt; dotted JSON path; section -&gt; heading text; whole -&gt; null.</summary>
        public string Locator { get; set; }

        /// <summary>
        /// Source fields this API version does not model, inlined on the wire so
        /// a client that knows them keeps them across a GET/PUT cycle.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; } = new Dictionary<string, JToken>();
    }
}
