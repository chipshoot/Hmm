﻿using FluentValidation;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Validation;
using System;

namespace Hmm.Core.DefaultManager.Validator
{
    public class NoteValidator : ValidatorBase<HmmNote>
    {
        private readonly IVersionRepository<HmmNote> _dataRepo;

        public NoteValidator(IVersionRepository<HmmNote> noteRepo)
        {
            Guard.Against<ArgumentNullException>(noteRepo == null, nameof(noteRepo));
            _dataRepo = noteRepo;

            RuleFor(n => n.Subject).NotNull().Length(1, 1000);
            RuleFor(n => n.Author).NotNull().Must(AuthorNotChanged).WithMessage("Cannot update note's author");
            RuleFor(n => n.Description).Length(1, 1000);
            RuleFor(n => n.Catalog).NotNull();
        }

        private bool AuthorNotChanged(HmmNote note, Author author)
        {
            if (author == null)
            {
                return false;
            }

            var savedNote = _dataRepo.GetEntity(note.Id);

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