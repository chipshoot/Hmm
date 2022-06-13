using FluentValidation;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Validation;
using System;
using System.Linq;

namespace Hmm.Core.DefaultManager.Validator
{
    public class AuthorValidator : ValidatorBase<Author>
    {
        private readonly IGuidRepository<Author> _dataSource;

        public AuthorValidator(IGuidRepository<Author> authorSource)
        {
            Guard.Against<ArgumentNullException>(authorSource == null, nameof(authorSource));
            _dataSource = authorSource;

            RuleFor(a => a.AccountName).NotNull().Length(1, 256).Must(UniqueAccountName).WithMessage("Duplicated account name");
            RuleFor(a => a.Description).Length(1, 1000);
        }

        private bool UniqueAccountName(Author user, string accountName)
        {
            var savedAuthor = _dataSource.GetEntity(user.Id);

            // create new user, make sure account name is unique
            var acc = accountName.Trim().ToLower();
            if (savedAuthor == null)
            {
                var sameAccountUser = _dataSource.GetEntities(u => u.AccountName.ToLower() == acc).FirstOrDefault();
                if (sameAccountUser != null)
                {
                    return false;
                }
            }
            else
            {
                var userWithAccount = _dataSource.GetEntities(u => u.AccountName.ToLower() == acc && u.Id != user.Id).FirstOrDefault();
                return userWithAccount == null;
            }

            return true;
        }
    }
}