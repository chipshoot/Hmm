using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public abstract class EntityManagerBase<T> : IAutoEntityManager<T> where T : AutomobileBase
    {
        protected EntityManagerBase(IHmmValidator<T> validator, IHmmNoteManager noteManager, IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(noteManager);
            ArgumentNullException.ThrowIfNull(lookupRepo);

            Validator = validator;
            NoteManager = noteManager;
            LookupRepo = lookupRepo;
            DefaultAuthor = ApplicationRegister.DefaultAuthor;
        }

        public IHmmValidator<T> Validator { get; }

        protected IHmmNoteManager NoteManager { get; }

        protected IEntityLookup LookupRepo { get; }

        /// <summary>
        /// Get notes for specific entity
        /// </summary>
        protected async Task<ProcessingResult<PageList<HmmNote>>> GetNotesAsync(
            T entity,
            Expression<Func<HmmNote, bool>> query = null,
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var catId = await entity.GetCatalogIdAsync(LookupRepo);

            if (DefaultAuthor == null)
            {
                return ProcessingResult<PageList<HmmNote>>.Fail("Default author is not configured", ErrorCategory.ValidationError);
            }

            var authorResult = await LookupRepo.GetEntityAsync<Author>(DefaultAuthor.Id);
            if (!authorResult.Success || authorResult.Value == null)
            {
                return ProcessingResult<PageList<HmmNote>>.NotFound("Cannot find default author");
            }

            Expression<Func<HmmNote, bool>> baseFilter = n => n.Author.Id == DefaultAuthor.Id && n.Catalog.Id == catId;
            var finalQuery = query != null ? query.And(baseFilter) : baseFilter;

            return await NoteManager.GetNotesAsync(finalQuery, false, resourceCollectionParameters);
        }

        /// <summary>
        /// Get note for specific entity by id
        /// </summary>
        protected async Task<ProcessingResult<HmmNote>> GetNoteAsync(int id, T entity)
        {
            var catId = await entity.GetCatalogIdAsync(LookupRepo);

            if (DefaultAuthor == null)
            {
                return ProcessingResult<HmmNote>.Fail("Default author is not configured", ErrorCategory.ValidationError);
            }

            var authorResult = await LookupRepo.GetEntityAsync<Author>(DefaultAuthor.Id);
            if (!authorResult.Success || authorResult.Value == null)
            {
                return ProcessingResult<HmmNote>.NotFound("Cannot find default author");
            }

            var noteResult = await NoteManager.GetNoteByIdAsync(id);
            if (!noteResult.Success || noteResult.Value == null)
            {
                return ProcessingResult<HmmNote>.NotFound($"Cannot find note with id {id}");
            }

            var note = noteResult.Value;
            if (note.Author.Id == DefaultAuthor.Id && note.Catalog.Id == catId)
            {
                return ProcessingResult<HmmNote>.Ok(note);
            }

            return ProcessingResult<HmmNote>.NotFound("Note does not belong to the current author or catalog");
        }

        #region IAutoEntityManager<T> implementation

        public abstract INoteSerializer<T> NoteSerializer { get; }

        public Author DefaultAuthor { get; }

        public abstract Task<ProcessingResult<T>> GetEntityByIdAsync(int id);

        public abstract Task<ProcessingResult<PageList<T>>> GetEntitiesAsync(ResourceCollectionParameters resourceCollectionParameters = null);

        public abstract Task<ProcessingResult<T>> CreateAsync(T entity);

        public abstract Task<ProcessingResult<T>> UpdateAsync(T entity);

        public async Task<bool> IsEntityOwnerAsync(int id)
        {
            var entityResult = await GetEntityByIdAsync(id);
            if (!entityResult.Success || entityResult.Value == null)
            {
                return false;
            }

            return entityResult.Value.AuthorId == DefaultAuthor?.Id;
        }

        #endregion
    }
}
