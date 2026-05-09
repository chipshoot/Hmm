using FluentValidation;
using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Dal.Query;
using System;

namespace Hmm.Automobile.Validator
{
    public class ServiceRecordValidator : EntityValidatorBase<ServiceRecord>
    {
        public ServiceRecordValidator(IEntityLookup lookupRepo) : base(lookupRepo)
        {
            RuleFor(r => r.AuthorId)
                .MustAsync(async (id, cancellation) => await HasValidAuthor(id, cancellation))
                .WithMessage("Service record must have a valid author");
            RuleFor(r => r.AutomobileId).GreaterThan(0).WithMessage("Service record must belong to an automobile");
            RuleFor(r => r.Date).Must(d => d != DateTime.MinValue).WithMessage("Service record must have a valid date");
            RuleFor(r => r.Mileage).GreaterThanOrEqualTo(0).WithMessage("Mileage cannot be negative");
            RuleFor(r => r.Description).MaximumLength(1000);
            RuleFor(r => r.Cost).Must(c => c == null || HasValidMoney(c)).WithMessage("Cost must be a non-negative money amount");
            RuleFor(r => r.ShopName).MaximumLength(100);
            RuleForEach(r => r.Parts)
                .ChildRules(item =>
                {
                    item.RuleFor(p => p.Name).NotEmpty().MaximumLength(100);
                    item.RuleFor(p => p.Quantity).GreaterThan(0);
                });
        }
    }
}
