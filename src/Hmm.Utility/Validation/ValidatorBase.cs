using FluentValidation;
using Hmm.Utility.Misc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Utility.Validation
{
    /// <summary>
    /// Base class for all entity validators using FluentValidation.
    ///
    /// <para><strong>Thread-Safety Guarantees:</strong></para>
    /// <para>
    /// This class is designed to be thread-safe when used with proper DI registration:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <strong>Scoped Registration (Recommended):</strong> Register validators as Scoped services
    ///     (AddScoped) to ensure each HTTP request gets its own validator instance. This eliminates
    ///     any potential for shared state between concurrent requests.
    ///   </description></item>
    ///   <item><description>
    ///     <strong>No Mutable State:</strong> ValidatorBase does not store any mutable instance state.
    ///     All validation results are created fresh for each call to ValidateEntityAsync.
    ///   </description></item>
    ///   <item><description>
    ///     <strong>Immutable Results:</strong> The returned ProcessingResult is immutable, ensuring
    ///     no race conditions when results are shared or passed between threads.
    ///   </description></item>
    /// </list>
    ///
    /// <para><strong>Best Practices:</strong></para>
    /// <list type="number">
    ///   <item><description>
    ///     Derived validators should only store readonly dependencies (repositories, services)
    ///     injected through constructor.
    ///   </description></item>
    ///   <item><description>
    ///     Avoid storing mutable instance fields in derived validators.
    ///   </description></item>
    ///   <item><description>
    ///     Always use async validation rules (MustAsync) for database queries.
    ///   </description></item>
    /// </list>
    /// </summary>
    /// <typeparam name="T">The type of entity to validate.</typeparam>
    public abstract class ValidatorBase<T> : AbstractValidator<T>, IHmmValidator<T>
    {
        /// <summary>
        /// Validates an entity and returns an immutable ProcessingResult.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is thread-safe. Each call creates independent local variables and
        /// returns a new immutable ProcessingResult instance. Multiple concurrent calls
        /// to this method (even on the same validator instance) will not interfere with
        /// each other.
        /// </para>
        /// <para>
        /// The underlying FluentValidation's ValidateAsync is also thread-safe for validators
        /// that don't store mutable state between validations.
        /// </para>
        /// </remarks>
        /// <param name="entity">The entity to validate. Must not be null.</param>
        /// <returns>
        /// A ProcessingResult containing the validated entity on success,
        /// or validation error messages on failure.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when entity is null.</exception>
        public async Task<ProcessingResult<T>> ValidateEntityAsync(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                var validationResult = await ValidateAsync(entity);

                if (validationResult.IsValid)
                {
                    return ProcessingResult<T>.Ok(entity);
                }

                // Convert FluentValidation errors to Result error messages
                var errorMessages = validationResult.Errors
                    .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                    .ToArray();

                return ProcessingResult<T>.Invalid(errorMessages);
            }
            catch (Exception ex)
            {
                return ProcessingResult<T>.FromException(ex);
            }
        }
    }
}