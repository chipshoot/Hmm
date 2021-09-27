using FluentValidation;
using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using Hmm.Utility.MeasureUnit;

namespace Hmm.Automobile.Validator
{
    public class GasLogValidator : EntityValidatorBase<GasLog>
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public GasLogValidator(IEntityLookup lookupRepo, IDateTimeProvider dateTimeProvider) : base(lookupRepo)
        {
            Guard.Against<ArgumentNullException>(dateTimeProvider == null, nameof(dateTimeProvider));
            _dateTimeProvider = dateTimeProvider;

            RuleFor(l => l.AuthorId).Must(HasValidAuthor).WithMessage("Has valid author for GasLog");
            RuleFor(l => l.Date).NotNull().Must(HasValidDate).WithMessage("Gas log should not has valid date");
            RuleFor(l => l.Car).NotNull().WithMessage("Gas log must belongs to an automobile");
            RuleFor(l => l.Distance).Must((o, distance)=>HasValidDistance2(distance, o.CurrentMeterReading)).WithMessage("Need has valid distance");
            RuleFor(l => l.CurrentMeterReading).Must((o, meter)=> HasValidDistance2(o.Distance, meter)).WithMessage("Need has valid meter reading");
            RuleFor(l => l.Gas).Must(HasValidVolume).WithMessage("Need has valid gas volume");
            RuleFor(l => l.Price).Must(HasValidMoney).WithMessage("Need has valid Price");
            RuleFor(l => l.Station).Length(1, 1000).WithMessage("Need has gas station");
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