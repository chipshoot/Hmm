using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <returns>The notes which belongs to entity type</returns>
        protected IEnumerable<HmmNote> GetNotes(T entity)
        {
            var catId = entity.GetCatalogId(LookupRepo);

            var hasValidAuthor = DefaultAuthor != null && LookupRepo.GetEntity<Author>(DefaultAuthor.Id) != null;
            switch (hasValidAuthor)
            {
                case false:
                    ProcessResult.AddErrorMessage("Cannot find default author", true);
                    return null;

                default:

                    var notes = NoteManager.GetNotes().ToList().Where(n => n.Author.Id == DefaultAuthor.Id && n.Catalog.Id == catId && !n.IsDeleted);
                    return notes;
            }
        }

        protected async Task<IEnumerable<HmmNote>> GetNotesAsync(T entity)
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

                    var notes = await NoteManager.GetNotesAsync(n => n.Author.Id == DefaultAuthor.Id && n.Catalog.Id == catId && !n.IsDeleted);
                    var noteList = notes.ToList();
                    return noteList;
            }
        }

        protected IHmmNoteManager NoteManager { get; }

        protected IEntityLookup LookupRepo { get; }

        #region method of interface IAutoEntityManager

        public abstract Task<T> GetEntityByIdAsync(int id);

        public abstract IEnumerable<T> GetEntities();

        public abstract Task<IEnumerable<T>> GetEntitiesAsync();

        public abstract INoteSerializer<T> NoteSerializer { get; }

        public Author DefaultAuthor { get; }

        public abstract T GetEntityById(int id);

        public abstract T Create(T entity);

        public abstract Task<T> CreateAsync(T entity);

        public abstract T Update(T entity);

        public abstract Task<T> UpdateAsync(T entity);

        public bool IsEntityOwner(int id)
        {
            var hasEntity = GetEntities().Any(e => e.Id == id && e.AuthorId == DefaultAuthor.Id);
            return hasEntity;
        }

        public ProcessingResult ProcessResult { get; } = new ProcessingResult();

        #endregion method of interface IAutoEntityManager
    }
}