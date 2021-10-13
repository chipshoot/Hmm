using FluentValidation;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Validation;

namespace Hmm.Core.DefaultManager.Validator
{
    public class SubsystemValidator : ValidatorBase<Subsystem>
    {
        public SubsystemValidator()
        {
            RuleFor(r => r.Name).NotNull().Length(1, 200);
            RuleFor(r => r.DefaultAuthor).NotNull();
            RuleFor(r => r.IsDefault).NotNull();
            RuleFor(r => r.Description).Length(1, 1000);
        }
    }
}