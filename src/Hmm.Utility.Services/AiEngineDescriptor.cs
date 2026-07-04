namespace Hmm.Utility.Services
{
    /// <summary>One configured AI engine — a single entry under
    /// <c>AiEngines:Engines</c>. Providers are stateless and read all
    /// per-engine config (model, endpoint, key) from a descriptor.</summary>
    public class AiEngineDescriptor
    {
        public string Name { get; set; }

        public AiProvider Provider { get; set; }

        public string Model { get; set; }

        public string BaseUrl { get; set; }

        /// <summary>From the environment; empty for a keyless local endpoint.</summary>
        public string ApiKey { get; set; }

        /// <summary>Whether the engine can read images/PDFs — gates receipt use.</summary>
        public bool SupportsVision { get; set; } = true;

        public int MaxTokens { get; set; } = 2048;
    }
}
