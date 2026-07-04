using Hmm.Utility.Misc;

namespace Hmm.Utility.Services
{
    /// <summary>Resolves the <see cref="IReceiptExtractionProvider"/> for a
    /// given <see cref="AiProvider"/>.</summary>
    public interface IReceiptExtractionProviderRegistry
    {
        ProcessingResult<IReceiptExtractionProvider> Get(AiProvider provider);
    }
}
