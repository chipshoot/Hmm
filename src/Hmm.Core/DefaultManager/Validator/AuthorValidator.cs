using FluentValidation;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Validation;
using System;
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

        private async Task<bool> UniqueAccountName(Author author, string accountName, CancellationToken cancellationToken)
        {
            return await UniqueNameValidationHelper.IsNameUniqueAsync<Author, AuthorDao>(
                _authorRepository,
                author.Id,
                accountName,
                dao => dao.AccountName,
                dao => dao.IsActivated);
        }
    }
}