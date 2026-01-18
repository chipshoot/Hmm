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
    public class DiscountManager : EntityManagerBase<GasDiscount>
    {
        public DiscountManager(
            INoteSerializer<GasDiscount> noteSerializer,
            IHmmValidator<GasDiscount> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo)
            : base(validator, noteManager, lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            NoteSerializer = noteSerializer;
        }

        public override INoteSerializer<GasDiscount> NoteSerializer { get; }

        public override async Task<ProcessingResult<PageList<GasDiscount>>> GetEntitiesAsync(
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var notesResult = await GetNotesAsync(new GasDiscount(), null, resourceCollectionParameters);
                if (!notesResult.Success)
                {
                    return ProcessingResult<PageList<GasDiscount>>.Fail(notesResult.ErrorMessage, notesResult.ErrorType);
                }

                var notes = notesResult.Value;
                var discountTasks = notes.Select(async note =>
                {
                    var entityResult = await NoteSerializer.GetEntity(note);
                    return entityResult.Success ? entityResult.Value : null;
                });
                var discounts = await Task.WhenAll(discountTasks);
                var discountList = discounts.Where(discount => discount != null);

                var result = new PageList<GasDiscount>(discountList, notes.TotalCount, notes.CurrentPage, notes.PageSize);
                return ProcessingResult<PageList<GasDiscount>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<GasDiscount>>.FromException(ex);
            }
        }

        public override async Task<ProcessingResult<GasDiscount>> GetEntityByIdAsync(int id)
        {
            var noteResult = await GetNoteAsync(id, new GasDiscount());
            if (!noteResult.Success)
            {
                return ProcessingResult<GasDiscount>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            return await NoteSerializer.GetEntity(noteResult.Value);
        }

        public override async Task<ProcessingResult<GasDiscount>> CreateAsync(GasDiscount entity)
        {
            if (entity == null)
            {
                return ProcessingResult<GasDiscount>.Invalid("Entity cannot be null");
            }

            entity.AuthorId = DefaultAuthor.Id;

            var validationResult = await Validator.ValidateEntityAsync(entity);
            if (!validationResult.Success)
            {
                return ProcessingResult<GasDiscount>.Invalid(validationResult.ErrorMessage);
            }

            var noteResult = await NoteSerializer.GetNote(entity);
            if (!noteResult.Success)
            {
                return ProcessingResult<GasDiscount>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = DefaultAuthor;

            var createdNoteResult = await NoteManager.CreateAsync(note);
            if (!createdNoteResult.Success)
            {
                return ProcessingResult<GasDiscount>.Fail(createdNoteResult.ErrorMessage, createdNoteResult.ErrorType);
            }

            return await GetEntityByIdAsync(createdNoteResult.Value.Id);
        }

        public override async Task<ProcessingResult<GasDiscount>> UpdateAsync(GasDiscount entity)
        {
            if (entity == null)
            {
                return ProcessingResult<GasDiscount>.Invalid("Entity cannot be null");
            }

            var curDiscountResult = await GetEntityByIdAsync(entity.Id);
            if (!curDiscountResult.Success)
            {
                return ProcessingResult<GasDiscount>.NotFound("Cannot find discount in data source");
            }

            var curDiscount = curDiscountResult.Value;
            curDiscount.Amount = entity.Amount;
            curDiscount.Comment = entity.Comment;
            curDiscount.DiscountType = entity.DiscountType;
            curDiscount.IsActive = entity.IsActive;
            curDiscount.Program = entity.Program;

            var noteResult = await NoteSerializer.GetNote(curDiscount);
            if (!noteResult.Success)
            {
                return ProcessingResult<GasDiscount>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = DefaultAuthor;

            var updatedNoteResult = await NoteManager.UpdateAsync(note);
            if (!updatedNoteResult.Success)
            {
                return ProcessingResult<GasDiscount>.Fail(updatedNoteResult.ErrorMessage, updatedNoteResult.ErrorType);
            }

            return await GetEntityByIdAsync(curDiscount.Id);
        }
    }
}
