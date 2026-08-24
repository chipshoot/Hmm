using System;
using System.Threading.Tasks;
using FluentValidation;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;

namespace Hmm.Cheatsheet.Validator
{
    /// <summary>
    /// Card-level rules only.
    ///
    /// There are deliberately NO rules on Rows, ExtraFields or RawJson. The
    /// client keeps rows it cannot parse so a save cannot destroy them; a
    /// validator that rejected such a card would turn this API into the thing
    /// that deletes them. Anything the serializer preserved is, by definition,
    /// valid enough to store.
    /// </summary>
    public class CheatsheetValidator : ValidatorBase<CheatsheetCard>
    {
        private readonly IEntityLookup _lookupRepo;

        public CheatsheetValidator(IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);
            _lookupRepo = lookupRepo;

            RuleFor(c => c.AuthorId)
                .MustAsync(async (id, _) => await HasValidAuthor(id))
                .WithMessage("Have valid author for cheatsheet card");

            RuleFor(c => c.Id)
                .NotEmpty().WithMessage("Cheatsheet card id is required")
                .MaximumLength(100)
                .WithMessage("Cheatsheet card id must be 100 characters or less");

            RuleFor(c => c.Title)
                .NotEmpty().WithMessage("Cheatsheet title is required")
                .MaximumLength(200)
                .WithMessage("Cheatsheet title must be 200 characters or less");

            RuleFor(c => c.WalletGroup)
                .NotEmpty().WithMessage("Wallet group is required")
                .MaximumLength(100)
                .WithMessage("Wallet group must be 100 characters or less");

            RuleFor(c => c.TemplateId)
                .NotEmpty().WithMessage("Template id is required")
                .MaximumLength(100)
                .WithMessage("Template id must be 100 characters or less");
        }

        private async Task<bool> HasValidAuthor(int authorId)
        {
            if (authorId <= 0)
            {
                return false;
            }

            var savedAuthorResult = await _lookupRepo.GetEntityAsync<Author>(authorId);
            return savedAuthorResult.Success && savedAuthorResult.Value != null;
        }
    }
}
