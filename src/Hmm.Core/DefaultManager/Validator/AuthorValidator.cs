using FluentValidation;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager.Validator
{
    public class AuthorValidator : ValidatorBase<Author>
    {
        private readonly IAuthorManager _authorManager;

        public AuthorValidator(IAuthorManager authorSource)
        {
            Guard.Against<ArgumentNullException>(authorSource == null, nameof(authorSource));
            _authorManager = authorSource;

            RuleFor(a => a.AccountName).NotNull().Length(1, 256).WithMessage("AccountName is longer then 256 characters");
            RuleFor(a => a.AccountName).MustAsync(UniqueAccountName).WithMessage("Duplicated account name");
            RuleFor(a => a.Description).Length(1, 1000);
        }

        private async Task<bool> UniqueAccountName(Author user, string accountName, CancellationToken cancellationToken)
        {
            var savedAuthor = await _authorManager.GetAuthorByIdAsync(user.Id);

            // create new user, make sure account name is unique
            var acc = accountName.Trim().ToLower();
            if (savedAuthor == null)
            {
                var sameAccountUsers = await _authorManager.GetEntitiesAsync(u => u.AccountName.ToLower() == acc && u.IsActivated);
                if (sameAccountUsers.Count > 0)
                {
                    return false;
                }
            }
            else
            {
                var userWithAccounts = await _authorManager.GetEntitiesAsync(u => u.AccountName.ToLower() == acc && u.Id != user.Id && u.IsActivated);
                return !userWithAccounts.Any();
            }

            return true;
        }
    }
}