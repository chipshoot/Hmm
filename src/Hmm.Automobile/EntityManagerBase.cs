using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Automobile
{
    public abstract class EntityManagerBase<T> : IAutoEntityManager<T> where T : AutomobileBase
    {
        protected EntityManagerBase(IHmmNoteManager noteManager, IEntityLookup lookupRepo, Author defaultAuthor)
        {
            Guard.Against<ArgumentNullException>(noteManager == null, nameof(noteManager));
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));
            Guard.Against<ArgumentNullException>(defaultAuthor == null, nameof(defaultAuthor));

            NoteManager = noteManager;
            LookupRepo = lookupRepo;
            DefaultAuthor = defaultAuthor;
        }

        protected IEnumerable<HmmNote> GetNotes(T entity)
        {
            var catId = entity.GetCatalogId(LookupRepo);

            switch (AuthorValid())
            {
                case false:
                    return null;

                default:

                    var notes = NoteManager.GetNotes().Where(n => n.Author.Id == DefaultAuthor.Id && n.Catalog.Id == catId);
                    return notes;
            }
        }

        // ToDo: Add validating object to valid new gas log
        protected bool AuthorValid()
        {
            if (DefaultAuthor == null)
            {
                ProcessResult.AddErrorMessage("Cannot find default author", true);
                return false;
            }

            var author = LookupRepo.GetEntity<Author>(DefaultAuthor.Id);
            switch (author)
            {
                case null:
                    ProcessResult.AddErrorMessage("Cannot find default author from database", true);
                    return false;

                default:
                    return true;
            }
        }

        protected IHmmNoteManager NoteManager { get; }

        protected IEntityLookup LookupRepo { get; }

        #region method of interface IAutoEntityManager

        public abstract IEnumerable<T> GetEntities();

        public abstract INoteSerializer<T> NoteSerializer { get; }

        public Author DefaultAuthor { get; }

        public abstract T GetEntityById(int id);

        public abstract T Create(T entity);

        public abstract T Update(T entity);

        public bool IsEntityOwner(int id)
        {
            var hasEntity = GetEntities().Any(e => e.Id == id && e.AuthorId == DefaultAuthor.Id);
            return hasEntity;
        }

        public ProcessingResult ProcessResult { get; } = new ProcessingResult();

        #endregion method of interface IAutoEntityManager
    }
}