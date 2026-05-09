using FluentValidation;
using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;

namespace Hmm.Automobile.Validator
{
    public class AutoScheduledServiceValidator : EntityValidatorBase<AutoScheduledService>
    {
        public AutoScheduledServiceValidator(IEntityLookup lookupRepo) : base(lookupRepo)
        {
            RuleFor(s => s.AuthorId)
                .MustAsync(async (id, cancellation) => await HasValidAuthor(id, cancellation))
                .WithMessage("Scheduled service must have a valid author");
            RuleFor(s => s.AutomobileId).GreaterThan(0).WithMessage("Scheduled service must belong to an automobile");
            RuleFor(s => s.Name).NotEmpty().MaximumLength(100).WithMessage("Schedule name is required (max 100 chars)");
            RuleFor(s => s)
                .Must(s => s.IntervalDays.HasValue || s.IntervalMileage.HasValue)
                .WithMessage("At least one of interval days or interval mileage must be set");
            RuleFor(s => s.IntervalDays).GreaterThan(0).When(s => s.IntervalDays.HasValue);
            RuleFor(s => s.IntervalMileage).GreaterThan(0).When(s => s.IntervalMileage.HasValue);
            RuleFor(s => s.NextDueMileage).GreaterThanOrEqualTo(0).When(s => s.NextDueMileage.HasValue);
        }
    }
}
