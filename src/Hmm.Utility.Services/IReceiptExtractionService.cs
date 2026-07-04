using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Utility.Services
{
    /// <summary>
    /// Extracts structured data from a receipt image or PDF via the selected AI
    /// engine. The engine is resolved from config (default) unless overridden
    /// per request by <paramref name="engine"/> (explicit name) or
    /// <paramref name="purpose"/> (a configured route, e.g. "personal").
    /// </summary>
    public interface IReceiptExtractionService
    {
        Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(
            byte[] bytes, string contentType, string engine = null, string purpose = null);
    }
}
