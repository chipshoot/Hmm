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
        protected EntityManagerBase(IHmmValidator<T> validator, IHmmNoteManager noteManager, IEntityLookup lookupRepo, Author defaultAuthor)
        {
            Guard.Against<ArgumentNullException>(validator == null, nameof(validator));
            Guard.Against<ArgumentNullException>(noteManager == null, nameof(noteManager));
            Guard.Against<ArgumentNullException>(lookupRepo == null, nameof(lookupRepo));
            Guard.Against<ArgumentNullException>(defaultAuthor == null, nameof(defaultAuthor));

            Validator = validator;
            NoteManager = noteManager;
            LookupRepo = lookupRepo;
            DefaultAuthor = defaultAuthor;
        }

        public IHmmValidator<T> Validator { get; }

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

                    var notes = NoteManager.GetNotes().Where(n => n.Author.Id == DefaultAuthor.Id && n.Catalog.Id == catId);
                    return notes;
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