namespace Hmm.Utility.Services
{
    /// <summary>
    /// Configuration for the Anthropic Messages API used by
    /// <see cref="ClaudeReceiptExtractionService"/>. Bind from the
    /// <c>AnthropicSettings</c> section; the API key must come from the
    /// environment/secret store on deployed hosts (never committed).
    /// </summary>
    public class AnthropicSettings
    {
        public const string SectionName = "AnthropicSettings";

        public string ApiKey { get; set; }

        public string Model { get; set; } = "claude-haiku-4-5";

        public string BaseUrl { get; set; } = "https://api.anthropic.com";

        public int MaxTokens { get; set; } = 2048;
    }
}
