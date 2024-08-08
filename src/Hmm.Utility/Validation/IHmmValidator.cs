using System.Threading.Tasks;
using Hmm.Utility.Misc;

namespace Hmm.Utility.Validation
{
    public interface IHmmValidator<in T>
    {
        Task<bool> IsValidEntityAsync(T entity, ProcessingResult processResult);
    }
}