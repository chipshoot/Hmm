using Hmm.Utility.Misc;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Utility.Services
{
    public class ReceiptExtractionProviderRegistry : IReceiptExtractionProviderRegistry
    {
        private readonly Dictionary<AiProvider, IReceiptExtractionProvider> _byProvider;

        public ReceiptExtractionProviderRegistry(IEnumerable<IReceiptExtractionProvider> providers)
        {
            _byProvider = providers.ToDictionary(p => p.Provider);
        }

        public ProcessingResult<IReceiptExtractionProvider> Get(AiProvider provider) =>
            _byProvider.TryGetValue(provider, out var impl)
                ? ProcessingResult<IReceiptExtractionProvider>.Ok(impl)
                : ProcessingResult<IReceiptExtractionProvider>.Fail(
                    $"No AI provider registered for '{provider}'.");
    }
}
