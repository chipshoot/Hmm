using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Base class for automobile entity managers that provides common CRUD operations.
    /// Eliminates duplicate error handling and common patterns across AutomobileManager,
    /// DiscountManager, GasLogManager, and GasStationManager.
    /// </summary>
    /// <typeparam name="T">The entity type derived from AutomobileBase</typeparam>
    public abstract class EntityManagerBase<T> : IAutoEntityManager<T> where T : AutomobileBase, new()
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

        /// <summary>
        /// Gets an entity by its ID. This is a common implementation that handles
        /// note retrieval and deserialization with proper error handling.
        /// </summary>
        public virtual async Task<ProcessingResult<T>> GetEntityByIdAsync(int id)
        {
            var noteResult = await GetNoteAsync(id, new T());
            if (!noteResult.Success)
            {
                return ProcessingResult<T>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            return await NoteSerializer.GetEntity(noteResult.Value);
        }

        /// <summary>
        /// Gets all entities with optional pagination. This is a common implementation that handles
        /// note retrieval, deserialization, and pagination with proper error handling.
        /// </summary>
        public virtual async Task<ProcessingResult<PageList<T>>> GetEntitiesAsync(
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var notesResult = await GetNotesAsync(new T(), null, resourceCollectionParameters);
                if (!notesResult.Success)
                {
                    return ProcessingResult<PageList<T>>.Fail(notesResult.ErrorMessage, notesResult.ErrorType);
                }

                var notes = notesResult.Value;
                var entityTasks = notes.Select(async note =>
                {
                    var entityResult = await NoteSerializer.GetEntity(note);
                    return entityResult.Success ? entityResult.Value : null;
                });
                var entities = await Task.WhenAll(entityTasks);
                var entityList = entities.Where(e => e != null);

                var result = new PageList<T>(entityList, notes.TotalCount, notes.CurrentPage, notes.PageSize);
                return ProcessingResult<PageList<T>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ProcessingResult<PageList<T>>.FromException(ex);
            }
        }

        /// <summary>
        /// Creates a new entity. This is a common implementation that handles validation,
        /// serialization, and note creation with proper error handling.
        /// Override in derived classes for custom creation logic (e.g., GasLogManager).
        /// </summary>
        public virtual async Task<ProcessingResult<T>> CreateAsync(T entity)
        {
            if (entity == null)
            {
                return ProcessingResult<T>.Invalid("Entity cannot be null");
            }

            entity.AuthorId = DefaultAuthor.Id;

            var validationResult = await Validator.ValidateEntityAsync(entity);
            if (!validationResult.Success)
            {
                return ProcessingResult<T>.Invalid(validationResult.ErrorMessage);
            }

            var noteResult = await NoteSerializer.GetNote(entity);
            if (!noteResult.Success)
            {
                return ProcessingResult<T>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = DefaultAuthor;

            var createdNoteResult = await NoteManager.CreateAsync(note);
            if (!createdNoteResult.Success)
            {
                return ProcessingResult<T>.Fail(createdNoteResult.ErrorMessage, createdNoteResult.ErrorType);
            }

            return await GetEntityByIdAsync(createdNoteResult.Value.Id);
        }

        /// <summary>
        /// Updates an existing entity. Each entity type has different properties to copy,
        /// so this must be implemented by derived classes.
        /// </summary>
        public abstract Task<ProcessingResult<T>> UpdateAsync(T entity);

        /// <summary>
        /// Helper method for UpdateAsync implementations. Handles common update logic:
        /// retrieves existing entity, applies updates via callback, serializes, and persists.
        /// </summary>
        /// <param name="entity">The entity with updated values</param>
        /// <param name="notFoundMessage">Error message if entity not found</param>
        /// <param name="applyUpdates">Action to copy properties from entity to existing</param>
        protected async Task<ProcessingResult<T>> UpdateEntityAsync(
            T entity,
            string notFoundMessage,
            Action<T, T> applyUpdates)
        {
            if (entity == null)
            {
                return ProcessingResult<T>.Invalid("Entity cannot be null");
            }

            var existingResult = await GetEntityByIdAsync(entity.Id);
            if (!existingResult.Success)
            {
                return ProcessingResult<T>.NotFound(notFoundMessage);
            }

            var existing = existingResult.Value;
            applyUpdates(existing, entity);

            var noteResult = await NoteSerializer.GetNote(existing);
            if (!noteResult.Success)
            {
                return ProcessingResult<T>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = DefaultAuthor;

            var updatedNoteResult = await NoteManager.UpdateAsync(note);
            if (!updatedNoteResult.Success)
            {
                return ProcessingResult<T>.Fail(updatedNoteResult.ErrorMessage, updatedNoteResult.ErrorType);
            }

            return await GetEntityByIdAsync(existing.Id);
        }

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
