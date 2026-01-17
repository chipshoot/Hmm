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
        public AutomobileManager(
            INoteSerialize<AutomobileInfo> noteSerializer,
            IHmmValidator<AutomobileInfo> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo)
            : base(validator, noteManager, lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            NoteSerializer = noteSerializer;
        }

        public override INoteSerialize<AutomobileInfo> NoteSerializer { get; }

        public override async Task<ProcessingResult<PageList<AutomobileInfo>>> GetEntitiesAsync(
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var notesResult = await GetNotesAsync(new AutomobileInfo(), null, resourceCollectionParameters);
                if (!notesResult.Success)
                {
                    return ProcessingResult<PageList<AutomobileInfo>>.Fail(notesResult.ErrorMessage, notesResult.ErrorType);
                }

                var notes = notesResult.Value;
                var carTasks = notes.Select(async note =>
                {
                    var entityResult = await NoteSerializer.GetEntity(note);
                    return entityResult.Success ? entityResult.Value : null;
                });
                var cars = await Task.WhenAll(carTasks);
                var carList = cars.Where(car => car != null);

                var result = new PageList<AutomobileInfo>(carList, notes.TotalCount, notes.CurrentPage, notes.PageSize);
                return ProcessingResult<PageList<AutomobileInfo>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<AutomobileInfo>>.FromException(ex);
            }
        }

        public override async Task<ProcessingResult<AutomobileInfo>> GetEntityByIdAsync(int id)
        {
            var noteResult = await GetNoteAsync(id, new AutomobileInfo());
            if (!noteResult.Success)
            {
                return ProcessingResult<AutomobileInfo>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            return await NoteSerializer.GetEntity(noteResult.Value);
        }

        public override async Task<ProcessingResult<AutomobileInfo>> CreateAsync(AutomobileInfo entity)
        {
            if (entity == null)
            {
                return ProcessingResult<AutomobileInfo>.Invalid("Entity cannot be null");
            }

            entity.AuthorId = DefaultAuthor.Id;

            var validationResult = await Validator.ValidateEntityAsync(entity);
            if (!validationResult.Success)
            {
                return ProcessingResult<AutomobileInfo>.Invalid(validationResult.ErrorMessage);
            }

            var noteResult = await NoteSerializer.GetNote(entity);
            if (!noteResult.Success)
            {
                return ProcessingResult<AutomobileInfo>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = DefaultAuthor;

            var createdNoteResult = await NoteManager.CreateAsync(note);
            if (!createdNoteResult.Success)
            {
                return ProcessingResult<AutomobileInfo>.Fail(createdNoteResult.ErrorMessage, createdNoteResult.ErrorType);
            }

            return await GetEntityByIdAsync(createdNoteResult.Value.Id);
        }

        public override async Task<ProcessingResult<AutomobileInfo>> UpdateAsync(AutomobileInfo entity)
        {
            if (entity == null)
            {
                return ProcessingResult<AutomobileInfo>.Invalid("Entity cannot be null");
            }

            var curAutoResult = await GetEntityByIdAsync(entity.Id);
            if (!curAutoResult.Success)
            {
                return ProcessingResult<AutomobileInfo>.NotFound("Cannot find automobile in data source");
            }

            var curAuto = curAutoResult.Value;
            curAuto.Brand = entity.Brand;
            curAuto.Maker = entity.Maker;
            curAuto.MeterReading = entity.MeterReading;
            curAuto.Year = entity.Year;
            curAuto.Color = entity.Color;
            curAuto.Plate = entity.Plate;

            var noteResult = await NoteSerializer.GetNote(curAuto);
            if (!noteResult.Success)
            {
                return ProcessingResult<AutomobileInfo>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = DefaultAuthor;

            var updatedNoteResult = await NoteManager.UpdateAsync(note);
            if (!updatedNoteResult.Success)
            {
                return ProcessingResult<AutomobileInfo>.Fail(updatedNoteResult.ErrorMessage, updatedNoteResult.ErrorType);
            }

            return await GetEntityByIdAsync(curAuto.Id);
        }
    }
}