using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Utility.Services
{
    /// <summary>
    /// Extracts structured data from a receipt image or PDF via an LLM.
    /// </summary>
    public interface IReceiptExtractionService
    {
        /// <param name="bytes">The receipt file contents.</param>
        /// <param name="contentType">MIME type (image/* or application/pdf).</param>
        Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(byte[] bytes, string contentType);
    }
}
