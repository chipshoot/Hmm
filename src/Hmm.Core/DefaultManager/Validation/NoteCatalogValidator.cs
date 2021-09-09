using FluentValidation;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Validation;

namespace Hmm.Core.DefaultManager.Validation
{
    public class NoteCatalogValidator : ValidatorBase<NoteCatalog>
    {
        public NoteCatalogValidator()
        {
            RuleFor(c => c.Name).NotNull().Length(1, 200);
            RuleFor(c => c.Schema).NotNull();
            RuleFor(c => c.Render).NotNull();
            RuleFor(c => c.IsDefault).NotNull();
            RuleFor(c => c.Description).Length(0, 1000);
        }
    }
}