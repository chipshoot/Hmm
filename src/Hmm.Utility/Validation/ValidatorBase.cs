using FluentValidation;
using Hmm.Utility.Misc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Utility.Validation
{
    public abstract class ValidatorBase<T> : AbstractValidator<T>, IHmmValidator<T>
    {
        /// <summary>
        /// Validates an entity and returns an immutable Result.
        /// Thread-safe and eliminates race conditions.
        /// </summary>
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