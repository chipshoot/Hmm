using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public class AutomobileManager : EntityManagerBase<AutomobileInfo>
    {
        public AutomobileManager(INoteSerializer<AutomobileInfo> noteSerializer, IHmmValidator<AutomobileInfo> validator, IHmmNoteManager noteManager, IEntityLookup lookupRepo)
            : base(validator, noteManager, lookupRepo)
        {
            Guard.Against<ArgumentNullException>(noteSerializer == null, nameof(noteSerializer));
            NoteSerializer = noteSerializer;
        }

        public override PageList<AutomobileInfo> GetEntities(ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var notes = GetNotes(new AutomobileInfo(), resourceCollectionParameters);
                if (notes == null)
                {
                    return null;
                }

                var carList = notes.Select(note => NoteSerializer.GetEntity(note));
                var result = new PageList<AutomobileInfo>(carList, notes.TotalCount, notes.CurrentPage, notes.PageSize);
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

        public override async Task<PageList<AutomobileInfo>> GetEntitiesAsync(ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var notes = await GetNotesAsync(new AutomobileInfo(), resourceCollectionParameters);
                var carList = notes.Select(note => NoteSerializer.GetEntity(note));
                var result = new PageList<AutomobileInfo>(carList, notes.TotalCount, notes.CurrentPage, notes.PageSize);
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

        public override AutomobileInfo GetEntityById(int id)
        {
            var note = GetNote(id, new AutomobileInfo());
            return note == null ? null : NoteSerializer.GetEntity(note);
        }

        public override async Task<AutomobileInfo> GetEntityByIdAsync(int id)
        {
            var note = await GetNoteAsync(id, new AutomobileInfo());
            return note == null ? null : NoteSerializer.GetEntity(note);
        }

        public override AutomobileInfo Create(AutomobileInfo entity)
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
            if (NoteManager.ProcessResult.HasReturnedMessage())
            {
                ProcessResult.PropagandaResult(NoteManager.ProcessResult);
            }

            return NoteManager.ProcessResult.Success switch
            {
                false => null,
                _ => GetEntityById(note.Id)
            };
        }

        public override async Task<AutomobileInfo> CreateAsync(AutomobileInfo entity)
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
            if (NoteManager.ProcessResult.HasReturnedMessage())
            {
                ProcessResult.PropagandaResult(NoteManager.ProcessResult);
            }

            if (!NoteManager.ProcessResult.Success)
            {
                return null;
            }

            var newAuto = await GetEntityByIdAsync(note.Id);
            return newAuto;
        }

        public override AutomobileInfo Update(AutomobileInfo entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            var curAuto = GetEntityById(entity.Id);
            if (curAuto == null)
            {
                ProcessResult.AddErrorMessage("Cannot find automobile in data source");
                return null;
            }

            curAuto.Brand = entity.Brand;
            curAuto.Maker = entity.Maker;
            curAuto.MeterReading = entity.MeterReading;
            curAuto.Pin = entity.Pin;
            curAuto.Year = entity.Year;

            var curNote = curAuto.GetNote(NoteSerializer, DefaultAuthor);
            NoteManager.Update(curNote);

            switch (NoteManager.ProcessResult.Success)
            {
                case false:
                    ProcessResult.PropagandaResult(NoteManager.ProcessResult);
                    return null;

                default:

                    return GetEntityById(curAuto.Id);
            }
        }

        public override async Task<AutomobileInfo> UpdateAsync(AutomobileInfo entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            // ReSharper disable once PossibleNullReferenceException
            var curAuto = await GetEntityByIdAsync(entity.Id);
            if (curAuto == null)
            {
                ProcessResult.AddErrorMessage("Cannot find automobile in data source");
                return null;
            }

            curAuto.Brand = entity.Brand;
            curAuto.Maker = entity.Maker;
            curAuto.MeterReading = entity.MeterReading;
            curAuto.Pin = entity.Pin;
            curAuto.Year = entity.Year;

            var curNote = curAuto.GetNote(NoteSerializer, DefaultAuthor);
            await NoteManager.UpdateAsync(curNote);

            if (!NoteManager.ProcessResult.Success)
            {
                return null;
            }

            var updatedAuto = await GetEntityByIdAsync(curAuto.Id);
            return updatedAuto;
        }

        public override INoteSerializer<AutomobileInfo> NoteSerializer { get; }
    }
}