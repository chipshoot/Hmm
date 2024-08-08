using FluentValidation;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Validation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager.Validator
{
    public class NoteValidator : ValidatorBase<HmmNote>
    {
        private readonly IHmmNoteManager _noteManager;

        public NoteValidator(IHmmNoteManager noteManager)
        {
            Guard.Against<ArgumentNullException>(noteManager == null, nameof(noteManager));
            _noteManager = noteManager;

            RuleFor(n => n.Subject).NotNull().Length(1, 1000);
            RuleFor(n => n.Author).NotNull().MustAsync(AuthorNotChanged).WithMessage("Cannot update note's author");
            RuleFor(n => n.Description).Length(1, 1000);
            RuleFor(n => n.Catalog).NotNull();
        }

        private async Task<bool> AuthorNotChanged(HmmNote note, Author author, CancellationToken cancellationToken)
        {
            if (author == null)
            {
                return false;
            }

            var savedNote = await _noteManager.GetNoteByIdAsync(note.Id);

            // create new user, make sure account name is unique
            var authorId = author.Id;
            if (savedNote == null)
            {
                return true;
            }

            return savedNote.Author.Id == authorId;
        }
    }
}