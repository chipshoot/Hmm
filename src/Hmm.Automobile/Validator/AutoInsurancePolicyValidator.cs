using FluentValidation;
using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;

namespace Hmm.Automobile.Validator
{
    public class AutoInsurancePolicyValidator : EntityValidatorBase<AutoInsurancePolicy>
    {
        public AutoInsurancePolicyValidator(IEntityLookup lookupRepo) : base(lookupRepo)
        {
            RuleFor(p => p.AuthorId)
                .MustAsync(async (id, cancellation) => await HasValidAuthor(id, cancellation))
                .WithMessage("Auto insurance policy must have a valid author");
            RuleFor(p => p.AutomobileId).GreaterThan(0).WithMessage("Auto insurance policy must belong to an automobile");
            RuleFor(p => p.Provider).NotEmpty().MaximumLength(100).WithMessage("Provider is required (max 100 chars)");
            RuleFor(p => p.PolicyNumber).NotEmpty().MaximumLength(50).WithMessage("Policy number is required (max 50 chars)");
            RuleFor(p => p.EffectiveDate).LessThan(p => p.ExpiryDate).WithMessage("Effective date must be earlier than expiry date");
            RuleFor(p => p.Premium).Must(HasValidMoney).WithMessage("Premium must be a non-negative money amount");
            RuleForEach(p => p.Coverage)
                .ChildRules(item =>
                {
                    item.RuleFor(c => c.Type).NotEmpty().MaximumLength(50);
                    item.RuleFor(c => c.Limit).GreaterThanOrEqualTo(0);
                    item.RuleFor(c => c.Deductible).GreaterThanOrEqualTo(0);
                });
        }
    }
}
