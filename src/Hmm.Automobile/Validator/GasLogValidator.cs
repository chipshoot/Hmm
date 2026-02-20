using FluentValidation;
using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using Hmm.Utility.MeasureUnit;

namespace Hmm.Automobile.Validator
{
    public class GasLogValidator : EntityValidatorBase<GasLog>
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public GasLogValidator(IEntityLookup lookupRepo, IDateTimeProvider dateTimeProvider) : base(lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(dateTimeProvider);
            _dateTimeProvider = dateTimeProvider;

            RuleFor(l => l.AuthorId).MustAsync(async (id, cancellation) => await HasValidAuthor(id, cancellation)).WithMessage("Has valid author for GasLog");
            RuleFor(l => l.Date).NotNull().Must(HasValidDate).WithMessage("Gas log does not has valid date");
            RuleFor(l => l.AutomobileId).GreaterThan(0).WithMessage("Gas log must belongs to an automobile");
            RuleFor(l => l.Distance).Must((o, distance)=>HasValidDistance2(distance, o.Odometer)).WithMessage("Need has valid distance");
            RuleFor(l => l.Odometer).Must((o, meter)=> HasValidDistance2(o.Distance, meter)).WithMessage("Need has valid meter reading");
            RuleFor(l => l.Fuel).Must(HasValidVolume).WithMessage("Need has valid gas volume");
            RuleFor(l => l.UnitPrice).Must(HasValidMoney).WithMessage("Need has valid Price");
            RuleFor(l => l.Station).NotEmpty().WithMessage("Need has gas station");
            RuleFor(l => l.Station.Name).Length(1, 1000).WithMessage("Need has gas name").When(l => l.Station != null);
            RuleFor(l => l.CreateDate).Must(HasValidEarlyDate).WithMessage("Create date should not earlier then today");
            RuleForEach(l => l.Discounts).SetValidator(new GasDiscountInfoValidator(lookupRepo)).WithMessage("All valid GasDiscountInfo is required");
        }

        private bool HasValidDate(DateTime date)
        {
            return date != DateTime.MinValue && date <= _dateTimeProvider.UtcNow;
        }

        private bool HasValidEarlyDate(DateTime date)
        {
            return date != DateTime.MinValue && date <= _dateTimeProvider.UtcNow;
        }

        private static bool HasValidDistance2(Dimension distance, Dimension currentMeterReading)
        {
            return HasValidDistance(distance) && HasValidDistance(currentMeterReading) && distance <= currentMeterReading;
        }
    }
}