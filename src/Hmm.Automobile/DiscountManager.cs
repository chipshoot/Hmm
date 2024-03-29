﻿using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public class DiscountManager : EntityManagerBase<GasDiscount>
    {
        public DiscountManager(INoteSerializer<GasDiscount> noteSerializer, IHmmValidator<GasDiscount> validator, IHmmNoteManager noteManager, IEntityLookup lookupRepo)
            : base(validator, noteManager, lookupRepo)
        {
            Guard.Against<ArgumentNullException>(noteSerializer == null, nameof(noteSerializer));
            NoteSerializer = noteSerializer;
        }

        public override PageList<GasDiscount> GetEntities(ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var notes = GetNotes(new GasDiscount(), null,resourceCollectionParameters);
            if (notes == null)
            {
                return null;
            }
            var carList = notes.Select(note => NoteSerializer.GetEntity(note));
            var result = new PageList<GasDiscount>(carList, notes.TotalCount, notes.CurrentPage, notes.PageSize);
            return result;
        }

        public override async Task<PageList<GasDiscount>> GetEntitiesAsync(ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var notes = await GetNotesAsync(new GasDiscount(), null, resourceCollectionParameters);
                if (notes == null)
                {
                    return null;
                }
                var discountList = notes.Select(note => NoteSerializer.GetEntity(note));
                var result = new PageList<GasDiscount>(discountList, notes.TotalCount, notes.CurrentPage, notes.PageSize);
                return result;
            }
            catch (Exception e)
            {
                if (NoteSerializer.ProcessResult.Success)
                {
                    ProcessResult.WrapException(e);
                }
                else
                {
                    ProcessResult.PropagandaResult(NoteSerializer.ProcessResult);
                }

                return null;
            }
        }

        public override GasDiscount GetEntityById(int id)
        {
            var note = GetNote(id, new GasDiscount());
            return note == null ? null : NoteSerializer.GetEntity(note);
        }

        public override async Task<GasDiscount> GetEntityByIdAsync(int id)
        {
            var note = await GetNoteAsync(id, new GasDiscount());
            return note == null ? null : NoteSerializer.GetEntity(note);
        }

        public override GasDiscount Create(GasDiscount entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            entity.AuthorId = DefaultAuthor.Id;
            if (!Validator.IsValidEntity(entity, ProcessResult))
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

        public override async Task<GasDiscount> CreateAsync(GasDiscount entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            entity.AuthorId = DefaultAuthor.Id;
            if (!Validator.IsValidEntity(entity, ProcessResult))
            {
                return null;
            }
            var note = entity.GetNote(NoteSerializer, DefaultAuthor);
            await NoteManager.CreateAsync(note);
            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:
                    var newDiscount = await GetEntityByIdAsync(note.Id);
                    return newDiscount;
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

        public override async Task<GasDiscount> UpdateAsync(GasDiscount entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            var curDiscount = await GetEntityByIdAsync(entity.Id);
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
            await NoteManager.UpdateAsync(curNote);

            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:
                    var updDiscount = await GetEntityByIdAsync(curDiscount.Id);
                    return updDiscount;
            }
        }

        public override INoteSerializer<GasDiscount> NoteSerializer { get; }
    }
}