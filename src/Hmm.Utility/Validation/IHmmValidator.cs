using System.Threading.Tasks;
using Hmm.Utility.Misc;

namespace Hmm.Utility.Validation
{
    public interface IHmmValidator<T>
    {
        /// <summary>
        /// Validates an entity asynchronously and returns an immutable result.
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <returns>A ProcessingResult&lt;T&gt; indicating success or containing validation errors</returns>
        Task<ProcessingResult<T>> ValidateEntityAsync(T entity);
    }
}