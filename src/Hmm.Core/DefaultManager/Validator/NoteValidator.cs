using FluentValidation;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Validation;
using System;
using System.Threading;
using System.Threading.Tasks;
using Hmm.Utility.Dal.Query;

namespace Hmm.Core.DefaultManager.Validator
{
    public class NoteValidator : ValidatorBase<HmmNote>
    {
        private readonly IEntityLookup _lookup;

        public NoteValidator(IEntityLookup lookup)
        {
            ArgumentNullException.ThrowIfNull(lookup);
            _lookup = lookup;

            RuleFor(n => n.Subject).NotNull().Length(1, 1000);
            RuleFor(n => n.Author).NotNull().MustAsync(AuthorExists).WithMessage("Cannot find author of the note");
            RuleFor(n => n.Author).NotNull().MustAsync(AuthorNotChanged).WithMessage("Cannot update note's author");
            RuleFor(n => n.Catalog).NotNull().MustAsync(NoteCatalogExists).WithMessage("Cannot find note's catalog");
            RuleFor(n => n.Description).Length(0, 1000);
            RuleFor(n => n.Catalog).NotNull();
        }

        private async Task<bool> AuthorExists(Author author, CancellationToken cancellationToken)
        {
            if (author == null)
            {
                return false;
            }

            var savedAuthorResult = await _lookup.GetEntityAsync<AuthorDao>(author.Id);
            return savedAuthorResult.Success && !savedAuthorResult.IsNotFound;
        }

        private async Task<bool> AuthorNotChanged(HmmNote note, Author author, CancellationToken cancellationToken)
        {
            if (author == null)
            {
                return false;
            }

            var savedNoteResult = await _lookup.GetEntityAsync<HmmNoteDao>(note.Id);

            // create new user, make sure account name is unique
            var authorId = author.Id;
            if (savedNoteResult.Success && savedNoteResult.IsNotFound)
            {
                return true;
            }

            return savedNoteResult.Value.Author?.Id == authorId;
        }

        private async Task<bool> NoteCatalogExists(NoteCatalog catalog, CancellationToken cancellationToken)
        {
            if (catalog == null)
            {
                return false;
            }

            var savedCatalogResult = await _lookup.GetEntityAsync<NoteCatalogDao>(catalog.Id);
            return savedCatalogResult.Success && !savedCatalogResult.IsNotFound;
        }
    }
}