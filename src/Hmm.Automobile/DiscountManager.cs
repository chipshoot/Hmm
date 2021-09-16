using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Automobile
{
    public class DiscountManager : EntityManagerBase<GasDiscount>
    {
        public DiscountManager(INoteSerializer<GasDiscount> noteSerializer, IHmmNoteManager noteManager, IEntityLookup lookupRepo, Author defaultAuthor) : base(noteManager, lookupRepo, defaultAuthor)
        {
            Guard.Against<ArgumentNullException>(noteSerializer == null, nameof(noteSerializer));
            NoteSerializer = noteSerializer;
        }

        public override IEnumerable<GasDiscount> GetEntities()
        {
            var notes = GetNotes(new GasDiscount());
            return notes?.Select(note => NoteSerializer.GetEntity(note)).ToList();
        }

        public override GasDiscount GetEntityById(int id)
        {
            return GetEntities().FirstOrDefault(d => d.Id == id);
        }

        public override GasDiscount Create(GasDiscount entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            if (!AuthorValid())
            {
                return null;
            }

            var note = entity.GetNote(NoteSerializer, DefaultAuthor);
            NoteManager.Create(note);
            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:
                    return GetEntityById(note.Id);
            }
        }

        public override GasDiscount Update(GasDiscount entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            var curDiscount = GetEntityById(entity.Id);
            if (curDiscount == null)
            {
                ProcessResult.AddErrorMessage("Cannot find discount in data source");
                return null;
            }

            curDiscount.Amount = entity.Amount;
            curDiscount.Comment = entity.Comment;
            curDiscount.DiscountType = entity.DiscountType;
            curDiscount.IsActive = entity.IsActive;
            curDiscount.Program = entity.Program;

            var curNote = curDiscount.GetNote(NoteSerializer, DefaultAuthor);
            NoteManager.Update(curNote);

            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:
                    return GetEntityById(curDiscount.Id);
            }
        }

        public override INoteSerializer<GasDiscount> NoteSerializer { get; }
    }
}