namespace Hmm.Utility.Services
{
    /// <summary>The kind of AI backend an engine talks to. Grows as providers
    /// are added (e.g. a self-hosted / OpenAI-compatible endpoint).</summary>
    public enum AiProvider
    {
        Anthropic,
        SelfHosted
    }
}
