using FluentValidation;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager.Validator
{
    public class AuthorValidator : ValidatorBase<Author>
    {
        private readonly IRepository<AuthorDao> _authorRepository;

        public AuthorValidator(IRepository<AuthorDao> authorRepository)
        {
            ArgumentNullException.ThrowIfNull(authorRepository);
            _authorRepository = authorRepository;

            RuleFor(a => a.AccountName).NotNull().Length(1, 256).WithMessage("AccountName is longer then 256 characters");
            RuleFor(a => a.AccountName).MustAsync(UniqueAccountName).WithMessage("Duplicated account name");
            RuleFor(a => a.Description).Length(1, 1000);
        }

        private async Task<bool> UniqueAccountName(Author user, string accountName, CancellationToken cancellationToken)
        {
            var savedAuthorResult = await _authorRepository.GetEntityAsync(user.Id);

            // Create new user, make sure account name is unique
            var acc = accountName.Trim().ToLower();
            if (!savedAuthorResult.Success || savedAuthorResult.IsNotFound)
            {
                // Creating new author - check for existing account names
                var sameAccountUsersResult = await _authorRepository.GetEntitiesAsync(u => u.AccountName.ToLower() == acc && u.IsActivated);
                if (sameAccountUsersResult.Success && !sameAccountUsersResult.IsNotFound)
                {
                    return false;
                }
            }
            else
            {
                // Updating existing author - check for conflicts with other authors
                var userWithAccountsResult = await _authorRepository.GetEntitiesAsync(u => u.AccountName.ToLower() == acc && u.Id != user.Id && u.IsActivated);
                if (userWithAccountsResult.Success && !userWithAccountsResult.IsNotFound)
                {
                    return !userWithAccountsResult.Value.Any();
                }
            }

            return true;
        }
    }
}