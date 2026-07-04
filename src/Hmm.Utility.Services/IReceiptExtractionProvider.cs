using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Utility.Services
{
    /// <summary>One implementation per AI provider. Stateless — all per-engine
    /// config (model, endpoint, key) comes from the descriptor, so a single
    /// instance serves every engine of its provider kind.</summary>
    public interface IReceiptExtractionProvider
    {
        AiProvider Provider { get; }

        Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(
            AiEngineDescriptor engine, byte[] bytes, string contentType);
    }
}
