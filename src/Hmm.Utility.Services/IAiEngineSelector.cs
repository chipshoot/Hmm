using Hmm.Utility.Misc;

namespace Hmm.Utility.Services
{
    /// <summary>Resolves which AI engine a request should use. Precedence:
    /// explicit engine name &gt; purpose route &gt; default.</summary>
    public interface IAiEngineSelector
    {
        ProcessingResult<AiEngineDescriptor> Resolve(string requestedEngine, string purpose);
    }
}
