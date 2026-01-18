using FluentValidation;
using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;

namespace Hmm.Automobile.Validator
{
    public class GasStationValidator : EntityValidatorBase<GasStation>
    {
        public GasStationValidator(IEntityLookup lookupRepo) : base(lookupRepo)
        {
            RuleFor(s => s.AuthorId).MustAsync(async (id, cancellation) => await HasValidAuthor(id, cancellation)).WithMessage("Have valid default author for GasStation");
            RuleFor(s => s.Name).NotEmpty().MaximumLength(100).WithMessage("Gas station name is required and must be 100 characters or less");
            RuleFor(s => s.Address).MaximumLength(200).WithMessage("Address must be 200 characters or less");
            RuleFor(s => s.City).MaximumLength(50).WithMessage("City must be 50 characters or less");
            RuleFor(s => s.State).MaximumLength(50).WithMessage("State must be 50 characters or less");
            RuleFor(s => s.ZipCode).MaximumLength(20).WithMessage("Zip code must be 20 characters or less");
            RuleFor(s => s.Description).MaximumLength(500).WithMessage("Description must be 500 characters or less");
        }
    }
}
