using FluentValidation;
using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;

namespace Hmm.Automobile.Validator
{
    public class GasDiscountValidator : EntityValidatorBase<GasDiscount>
    {
        public GasDiscountValidator(IEntityLookup lookupRepo) : base(lookupRepo)
        {
            RuleFor(a => a.AuthorId).Must(HasValidAuthor).WithMessage("Have valid default author for GasDiscount");
            RuleFor(d => d.Program).NotEmpty().WithMessage("Need has valid program");
            RuleFor(d => d.Amount).Must(HasValidMoney).WithMessage("Need has valid amount");
        }
    }
}