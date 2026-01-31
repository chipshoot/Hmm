using FluentValidation;
using Hmm.Utility.Misc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Utility.Validation
{
    /// <summary>
    /// Base class for all validators in the application.
    /// Extends FluentValidation's AbstractValidator with thread-safe result handling.
    ///
    /// Thread-Safety Guidelines:
    /// 1. REGISTRATION: Always register validators as Transient in DI container.
    ///    This ensures each validation gets a fresh instance, preventing any
    ///    potential state issues with concurrent validations.
    ///
    /// 2. CONSTRUCTOR RULES: Configure all validation rules in the constructor only.
    ///    Never modify rules or add new rules after construction.
    ///
    /// 3. READONLY DEPENDENCIES: All injected dependencies (repositories, lookups)
    ///    should be stored in readonly fields and accessed only for querying.
    ///
    /// 4. ASYNC RULES: MustAsync rules are safe when following the above guidelines.
    ///    Each validation call creates its own execution context.
    ///
    /// 5. NO MUTABLE STATE: Never add instance fields that are modified during
    ///    validation. All state should be local to the validation method.
    ///
    /// Example of safe validator:
    /// <code>
    /// public class MyValidator : ValidatorBase&lt;MyEntity&gt;
    /// {
    ///     private readonly IRepository&lt;MyDao&gt; _repository; // readonly
    ///
    ///     public MyValidator(IRepository&lt;MyDao&gt; repository)
    ///     {
    ///         _repository = repository;
    ///         RuleFor(x => x.Name).NotEmpty(); // rules in constructor only
    ///         RuleFor(x => x.Name).MustAsync(BeUnique);
    ///     }
    ///
    ///     private async Task&lt;bool&gt; BeUnique(string name, CancellationToken ct)
    ///     {
    ///         // Safe: uses readonly dependency, no mutable state
    ///         return await _repository.GetEntitiesAsync(...);
    ///     }
    /// }
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type of entity to validate</typeparam>
    public abstract class ValidatorBase<T> : AbstractValidator<T>, IHmmValidator<T>
    {
        /// <summary>
        /// Validates an entity and returns an immutable ProcessingResult.
        /// Thread-safe: creates new result instances, no shared mutable state.
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <returns>ProcessingResult with validation outcome</returns>
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