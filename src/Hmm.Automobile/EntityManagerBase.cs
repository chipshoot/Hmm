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
        protected EntityManagerBase(
            IHmmValidator<T> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo,
            IAuthorProvider authorProvider)
        {
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(noteManager);
            ArgumentNullException.ThrowIfNull(lookupRepo);
            ArgumentNullException.ThrowIfNull(authorProvider);

            Validator = validator;
            NoteManager = noteManager;
            LookupRepo = lookupRepo;
            AuthorProvider = authorProvider;
        }

        public IHmmValidator<T> Validator { get; }

        protected IHmmNoteManager NoteManager { get; }

        protected IEntityLookup LookupRepo { get; }

        /// <summary>
        /// Gets the author provider for resolving the author used in automobile operations.
        /// This can be either the default author provider or the current user provider.
        /// </summary>
        public IAuthorProvider AuthorProvider { get; }

        /// <summary>
        /// Gets the current author. Returns the cached author if available, otherwise returns null.
        /// For async access with proper initialization, use AuthorProvider.GetAuthorAsync().
        /// </summary>
        protected Author DefaultAuthor => AuthorProvider.CachedAuthor;

        /// <summary>
        /// Get notes for specific entity
        /// </summary>
        protected async Task<ProcessingResult<PageList<HmmNote>>> GetNotesAsync(
            T entity,
            Expression<Func<HmmNote, bool>> query = null,
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var catId = await entity.GetCatalogIdAsync(LookupRepo);

            var authorResult = await AuthorProvider.GetAuthorAsync();
            if (!authorResult.Success)
            {
                return ProcessingResult<PageList<HmmNote>>.Fail(authorResult.ErrorMessage, authorResult.ErrorType);
            }

            var author = authorResult.Value;
            Expression<Func<HmmNote, bool>> baseFilter = n => n.Author.Id == author.Id && n.Catalog.Id == catId;
            var finalQuery = query != null ? query.And(baseFilter) : baseFilter;

            return await NoteManager.GetNotesAsync(finalQuery, false, resourceCollectionParameters);
        }

        /// <summary>
        /// Get note for specific entity by id
        /// </summary>
        protected async Task<ProcessingResult<HmmNote>> GetNoteAsync(int id, T entity)
        {
            var catId = await entity.GetCatalogIdAsync(LookupRepo);

            var authorResult = await AuthorProvider.GetAuthorAsync();
            if (!authorResult.Success)
            {
                return ProcessingResult<HmmNote>.Fail(authorResult.ErrorMessage, authorResult.ErrorType);
            }

            var author = authorResult.Value;

            var noteResult = await NoteManager.GetNoteByIdAsync(id);
            if (!noteResult.Success || noteResult.Value == null)
            {
                return ProcessingResult<HmmNote>.NotFound($"Cannot find note with id {id}");
            }

            var note = noteResult.Value;
            if (note.Author.Id == author.Id && note.Catalog.Id == catId)
            {
                return ProcessingResult<HmmNote>.Ok(note);
            }

            return ProcessingResult<HmmNote>.NotFound("Note does not belong to the current author or catalog");
        }

        #region IAutoEntityManager<T> implementation

        public abstract INoteSerializer<T> NoteSerializer { get; }

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

            var authorResult = await AuthorProvider.GetAuthorAsync();
            if (!authorResult.Success)
            {
                return false;
            }

            return entityResult.Value.AuthorId == authorResult.Value.Id;
        }

        #endregion
    }
}
