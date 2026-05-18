using FluentValidation;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Vault;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
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
            // Attachments gate: route the typed domain refs through
            // the same codec the DAO column uses so the manager
            // rejects content-type / path / byteSize / disjointness
            // violations as Invalid (not a 500 at map time).
            RuleFor(n => n).Custom(ValidateAttachments);
        }

        private static void ValidateAttachments(
            HmmNote note,
            FluentValidation.ValidationContext<HmmNote> context)
        {
            if (note == null) return;
            try
            {
                var attachments = new NoteAttachments(
                    note.PrimaryImage,
                    note.Images ?? new List<VaultRef>());
                if (attachments.IsEmpty) return;
                // Round-trip through the codec — Encode + Decode is
                // the exact path the DAO column travels at read time,
                // and Decode runs both the JSON schema (content-type
                // allow-list, non-negative byteSize) and
                // VaultPathUtil.Validate (path-segment rules) on
                // every ref. Schema-only Validate would miss "..".
                var encoded = NoteAttachmentsCodec.Encode(attachments);
                NoteAttachmentsCodec.Decode(encoded);
            }
            catch (ArgumentException ex)
            {
                // NoteAttachments ctor surfaces disjointness (primary
                // also present in Images) as ArgumentException.
                context.AddFailure(
                    nameof(HmmNote.PrimaryImage),
                    $"Invalid attachments: {ex.Message}");
            }
            catch (FormatException ex)
            {
                // Decode throws FormatException on schema-violating
                // payloads, unknown kinds, and path-spec failures —
                // that's the full validation surface for the column.
                context.AddFailure(
                    nameof(HmmNote.PrimaryImage),
                    $"Invalid attachments: {ex.Message}");
            }
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