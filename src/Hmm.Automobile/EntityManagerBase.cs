using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.DomainEntity;
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
            Guard.Against<ArgumentNullException>(validator == null, nameof(validator));
            Guard.Against<ArgumentNullException>(noteManager == null, nameof(noteManager));
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));

            Validator = validator;
            NoteManager = noteManager;
            LookupRepo = lookupRepo;
            DefaultAuthor = ApplicationRegister.DefaultAuthor;
        }

        public IHmmValidator<T> Validator { get; }

        /// <summary>
        /// Get notes for specific entity
        /// </summary>
        /// <param name="entity">The entity which used to get type and figure out the catalog</param>
        /// <param name="query">Query for note</param>
        /// <param name="resourceCollectionParameters">The page information of the resource collection</param>
        /// <returns>The notes which belongs to entity type</returns>
        protected PageList<HmmNote> GetNotes(T entity, Expression<Func<HmmNote, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var catId = entity.GetCatalogId(LookupRepo);

            var hasValidAuthor = DefaultAuthor != null && LookupRepo.GetEntity<Author>(DefaultAuthor.Id) != null;
            switch (hasValidAuthor)
            {
                case false:
                    ProcessResult.AddErrorMessage("Cannot find default author", true);
                    return null;

                default:
                    PageList<HmmNote> notes;
                    if (query != null)
                    {
                        var finalExp = query.And(n => n.Author.Id == DefaultAuthor.Id && n.Catalog.Id == catId);
                        notes = NoteManager.GetNotes(finalExp, false, resourceCollectionParameters);
                    }
                    else
                    {
                        notes = NoteManager.GetNotes(n => n.Author.Id == DefaultAuthor.Id && n.Catalog.Id == catId, false, resourceCollectionParameters);
                    }

                    return notes;
            }
        }

        /// <summary>
        /// The asynchronous version of <see cref="GetNotes"/>
        /// </summary>
        /// <param name="entity">The entity which used to get type and figure out the catalog</param>
        /// <param name="resourceCollectionParameters">The page information of the resource collection</param>
        /// <returns>The notes which belongs to entity type</returns>
        protected async Task<PageList<HmmNote>> GetNotesAsync(T entity, Expression<Func<HmmNote, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var catId = await entity.GetCatalogIdAsync(LookupRepo);
            var author = await LookupRepo.GetEntityAsync<Author>(DefaultAuthor.Id);

            var hasValidAuthor = DefaultAuthor != null && author != null;
            switch (hasValidAuthor)
            {
                case false:
                    ProcessResult.AddErrorMessage("Cannot find default author", true);
                    return null;

                default:

                    PageList<HmmNote> notes;
                    if (query != null)
                    {
                        var finalExp = query.And(n => n.Author.Id == DefaultAuthor.Id && n.Catalog.Id == catId);
                        notes = await NoteManager.GetNotesAsync(finalExp, false, resourceCollectionParameters);
                    }
                    else
                    {
                        notes = await NoteManager.GetNotesAsync(n => n.Author.Id == DefaultAuthor.Id && n.Catalog.Id == catId, false, resourceCollectionParameters);
                    }

                    return notes;
            }
        }

        /// <summary>
        /// Get notes for specific entity
        /// </summary>
        /// <param name="id">The id of entity to get type and figure out the catalog</param>
        /// <param name="entity">The entity by id which used to get type and figure out the catalog</param>
        /// <returns>The notes which belongs to entity type</returns>
        protected HmmNote GetNote(int id, T entity)
        {
            var catId = entity.GetCatalogId(LookupRepo);

            var hasValidAuthor = DefaultAuthor != null && LookupRepo.GetEntity<Author>(DefaultAuthor.Id) != null;
            switch (hasValidAuthor)
            {
                case false:
                    ProcessResult.AddErrorMessage("Cannot find default author", true);
                    return null;

                default:

                    var note = NoteManager.GetNoteById(id);
                    if (note == null)
                    {
                        return null;
                    }

                    if (note.Author.Id == DefaultAuthor.Id && note.Catalog.Id == catId)
                    {
                        return note;
                    }

                    return null;
            }
        }

        /// <summary>
        /// Get notes for specific entity
        /// </summary>
        /// <param name="id">The id of entity to get type and figure out the catalog</param>
        /// <param name="entity">The entity by id which used to get type and figure out the catalog</param>
        /// <returns>The notes which belongs to entity type</returns>
        protected async Task<HmmNote> GetNoteAsync(int id, T entity)
        {
            var catId = await entity.GetCatalogIdAsync(LookupRepo);

            var hasValidAuthor = DefaultAuthor != null && LookupRepo.GetEntity<Author>(DefaultAuthor.Id) != null;
            switch (hasValidAuthor)
            {
                case false:
                    ProcessResult.AddErrorMessage("Cannot find default author", true);
                    return null;

                default:

                    var note = await NoteManager.GetNoteByIdAsync(id);
                    if (note == null)
                    {
                        return null;
                    }

                    if (note.Author.Id == DefaultAuthor.Id && note.Catalog.Id == catId)
                    {
                        return note;
                    }

                    return null;
            }
        }

        protected IHmmNoteManager NoteManager { get; }

        protected IEntityLookup LookupRepo { get; }

        #region method of interface IAutoEntityManager

        public abstract Task<T> GetEntityByIdAsync(int id);

        public abstract PageList<T> GetEntities(ResourceCollectionParameters resourceCollectionParameters = null);

        public abstract Task<PageList<T>> GetEntitiesAsync(ResourceCollectionParameters resourceCollectionParameters = null);

        public abstract INoteSerializer<T> NoteSerializer { get; }

        public Author DefaultAuthor { get; }

        public abstract T GetEntityById(int id);

        public abstract T Create(T entity);

        public abstract Task<T> CreateAsync(T entity);

        public abstract T Update(T entity);

        public abstract Task<T> UpdateAsync(T entity);

        public bool IsEntityOwner(int id)
        {
            var entity = GetEntityById(id);
            var hasEntity = entity != null && entity.AuthorId == DefaultAuthor.Id;
            return hasEntity;
        }

        public ProcessingResult ProcessResult { get; } = new ProcessingResult();

        #endregion method of interface IAutoEntityManager
    }
}