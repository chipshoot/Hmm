using FluentValidation;
using Hmm.Utility.Misc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Utility.Validation
{
    public abstract class ValidatorBase<T> : AbstractValidator<T>, IHmmValidator<T>
    {
        public async Task<bool> IsValidEntityAsync(T entity, ProcessingResult processResult)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));
            Guard.Against<ArgumentNullException>(processResult == null, nameof(processResult));

            try
            {
                var result = await ValidateAsync(entity);
                if (result.IsValid)
                {
                    return true;
                }

                // ReSharper disable once PossibleNullReferenceException
                processResult.Success = false;
                processResult.MessageList.AddRange(result.Errors.Select(e =>
                   new ReturnMessage
                   {
                       Message = $"{e.PropertyName} : {e.ErrorMessage}",
                       Type = MessageType.Error
                   }));
                return false;
            }
            catch (Exception ex)
            {
                processResult?.WrapException(ex);
                return false;
            }
        }
    }
}