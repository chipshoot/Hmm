using FluentValidation;
using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;

namespace Hmm.Automobile.Validator
{
    public class GasDiscountInfoValidator : EntityValidatorBase<GasDiscountInfo>
    {
        public GasDiscountInfoValidator(IEntityLookup lookupRepo) : base(lookupRepo)
        {
            RuleFor(d => d.Amount).Must(HasValidMoney).WithMessage("Need has valid discount saved money");
            RuleFor(d => d.Program).NotNull().WithMessage("Need has valid discount");
        }
    }
}