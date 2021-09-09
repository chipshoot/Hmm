using FluentValidation;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Validation;

namespace Hmm.Core.DefaultManager.Validation
{
    public class NoteRenderValidator : ValidatorBase<NoteRender>
    {
        public NoteRenderValidator()
        {
            RuleFor(r => r.Name).NotNull().Length(1, 400);
            RuleFor(r => r.Namespace).NotNull().Length(1, 1000);
            RuleFor(r => r.IsDefault).NotNull();
            RuleFor(r => r.Description).Length(1, 1000);
        }
    }
}