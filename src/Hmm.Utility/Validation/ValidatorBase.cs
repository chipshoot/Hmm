using FluentValidation;
using Hmm.Utility.Misc;
using System;
using System.Linq;

namespace Hmm.Utility.Validation
{
    public abstract class ValidatorBase<T> : AbstractValidator<T>
    {
        public bool IsValidEntity(T entity, ProcessingResult processResult)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));
            Guard.Against<ArgumentNullException>(processResult == null, nameof(processResult));

            var result = Validate(entity);
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
    }
}