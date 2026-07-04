using System.Collections.Generic;

namespace Hmm.Utility.Services
{
    /// <summary>Configuration for the swappable AI engine (section
    /// <c>AiEngines</c>): named engines, a default, and optional purpose
    /// routes.</summary>
    public class AiEngineOptions
    {
        public const string SectionName = "AiEngines";

        /// <summary>Name of the engine used when no override/route applies.</summary>
        public string Default { get; set; }

        /// <summary>Optional purpose -> engine-name routes
        /// (e.g. "personal" -> "local").</summary>
        public Dictionary<string, string> Routes { get; set; }
            = new Dictionary<string, string>();

        public List<AiEngineDescriptor> Engines { get; set; }
            = new List<AiEngineDescriptor>();
    }
}
