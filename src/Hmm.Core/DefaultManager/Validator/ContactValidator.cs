using FluentValidation;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Validation;
using System.Linq;

namespace Hmm.Core.DefaultManager.Validator
{
    public class ContactValidator : ValidatorBase<Contact>
    {
        public ContactValidator()
        {
            RuleFor(c => c.FirstName).NotEmpty().Length(1, 200);
            RuleFor(c => c.LastName).NotEmpty().Length(1, 200);
            RuleFor(c => c.IsActivated).NotNull();
            RuleForEach(c => c.Emails).ChildRules(email =>
            {
                email.RuleFor(e => e.Address).NotEmpty().Length(1, 200);
            }).When(c => c.Emails.Any());

            RuleForEach(c => c.Addresses).ChildRules(address =>
            {
                address.RuleFor(a => a.Address).NotEmpty().Length(1, 500);
                address.RuleFor(a => a.City).NotEmpty().Length(1, 50);
                address.RuleFor(a => a.State).NotEmpty().Length(1, 50);
                address.RuleFor(a => a.Country).NotEmpty().Length(1, 50);
                address.RuleFor(a => a.PostalCode).NotEmpty().Length(1, 50);
            }).When(c => c.Addresses.Any());

            RuleForEach(c => c.Phones).ChildRules(phone =>
            {
                phone.RuleFor(p => p.Number).NotEmpty().Length(1, 50);
            }).When(c => c.Phones.Any());
            RuleFor(c => c.Description).Length(0, 1000);
        }
    }
}