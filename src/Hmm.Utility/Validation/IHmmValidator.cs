using System.Threading.Tasks;
using Hmm.Utility.Misc;

namespace Hmm.Utility.Validation
{
    /// <summary>
    /// Standard validation interface for the Hmm solution.
    /// This is the preferred validation contract that all validators should implement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface follows immutable result pattern - validators return a <see cref="ProcessingResult{T}"/>
    /// rather than mutating internal state. This design provides several benefits:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Thread-safe: No mutable state between validations</description></item>
    ///   <item><description>Async-first: Supports async validation rules (e.g., uniqueness checks)</description></item>
    ///   <item><description>Immutable results: ProcessingResult cannot be modified after creation</description></item>
    ///   <item><description>Composable: Results can be combined using ProcessingResult.Combine()</description></item>
    /// </list>
    /// <para>
    /// Implementation note: Use <see cref="ValidatorBase{T}"/> as a base class for concrete validators.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of entity to validate</typeparam>
    public interface IHmmValidator<T>
    {
        /// <summary>
        /// Validates an entity asynchronously and returns an immutable result.
        /// </summary>
        /// <param name="entity">The entity to validate. May be null, which should return a validation error.</param>
        /// <returns>
        /// A <see cref="ProcessingResult{T}"/> where:
        /// <list type="bullet">
        ///   <item><description>Success = true and Value = entity if validation passes</description></item>
        ///   <item><description>Success = false with ErrorMessage if validation fails</description></item>
        /// </list>
        /// </returns>
        Task<ProcessingResult<T>> ValidateEntityAsync(T entity);
    }
}