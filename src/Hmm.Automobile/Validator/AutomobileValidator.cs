using FluentValidation;
using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;

namespace Hmm.Automobile.Validator
{
    public class AutomobileValidator : EntityValidatorBase<AutomobileInfo>
    {
        public AutomobileValidator(IEntityLookup lookupRepo) : base(lookupRepo)
        {
            RuleFor(a => a.AuthorId).Must(HasValidAuthor).WithMessage("Have valid default author for Automobile");
            RuleFor(a => a.Maker).NotEmpty().WithMessage("Have valid automobile maker");
            RuleFor(a => a.Brand).NotEmpty().WithMessage("Have valid automobile brand");
            RuleFor(a => a.Year).NotEmpty().WithMessage("Have valid Year of the automobile");
            RuleFor(a => a.Color).NotEmpty().WithMessage("Have valid color of the automobile");
            RuleFor(a => a.Pin).NotEmpty().WithMessage("Have valid PIN of the automobile");
            RuleFor(a => a.Plate).NotEmpty().WithMessage("Have valid plate of the automobile");
            RuleFor(a => a.MeterReading).GreaterThan(0).WithMessage("Have valid meter reading of the automobile");
        }
    }
}