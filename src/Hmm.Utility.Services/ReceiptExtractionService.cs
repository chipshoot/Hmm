using Hmm.Utility.Misc;
using System;
using System.Threading.Tasks;

namespace Hmm.Utility.Services
{
    /// <summary>
    /// Facade the controller calls: resolves which engine to use (config
    /// default, or a per-request override / purpose route), guards that the
    /// engine can read receipts (vision), then delegates to the matching
    /// provider.
    /// </summary>
    public class ReceiptExtractionService : IReceiptExtractionService
    {
        private readonly IAiEngineSelector _selector;
        private readonly IReceiptExtractionProviderRegistry _registry;

        public ReceiptExtractionService(
            IAiEngineSelector selector,
            IReceiptExtractionProviderRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(selector);
            ArgumentNullException.ThrowIfNull(registry);

            _selector = selector;
            _registry = registry;
        }

        public async Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(
            byte[] bytes, string contentType, string engine = null, string purpose = null)
        {
            var resolved = _selector.Resolve(engine, purpose);
            if (!resolved.Success)
            {
                return ProcessingResult<ReceiptExtractionResult>.Fail(resolved.ErrorMessage);
            }

            var descriptor = resolved.Value;
            if (!descriptor.SupportsVision)
            {
                return ProcessingResult<ReceiptExtractionResult>.Fail(
                    $"AI engine '{descriptor.Name}' can't read receipts (no vision support).");
            }

            var providerResult = _registry.Get(descriptor.Provider);
            if (!providerResult.Success)
            {
                return ProcessingResult<ReceiptExtractionResult>.Fail(providerResult.ErrorMessage);
            }

            return await providerResult.Value.ExtractAsync(descriptor, bytes, contentType);
        }
    }
}
