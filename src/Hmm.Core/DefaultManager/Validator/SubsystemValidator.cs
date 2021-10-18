using FluentValidation;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Validation;
using System;
using System.Linq;

namespace Hmm.Core.DefaultManager.Validator
{
    public class SubsystemValidator : ValidatorBase<Subsystem>
    {
        public SubsystemValidator(IGuidRepository<Author> authorSource)
        {
            Guard.Against<ArgumentNullException>(authorSource == null, nameof(authorSource));
            RuleFor(s => s.Name).NotNull().Length(1, 200);
            RuleFor(s => s.DefaultAuthor).NotNull().SetValidator(new AuthorValidator(authorSource));
            RuleFor(s => s.IsDefault).NotNull();
            RuleFor(s => s.Description).Length(1, 1000);
            When(s => s.NoteCatalogs != null && s.NoteCatalogs.Any(), () =>
            {
                RuleForEach(s => s.NoteCatalogs).SetValidator(new NoteCatalogValidator());
            });
        }
    }
}