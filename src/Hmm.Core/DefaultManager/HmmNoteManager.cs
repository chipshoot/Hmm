using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Core.DefaultManager
{
    public class HmmNoteManager : IHmmNoteManager
    {
        #region private fields

        private readonly IVersionRepository<HmmNote> _noteNoteRepo;
        private readonly IHmmValidator<HmmNote> _validator;
        private readonly IDateTimeProvider _dateProvider;

        #endregion private fields

        public HmmNoteManager(IVersionRepository<HmmNote> noteRepo, IHmmValidator<HmmNote> validator, IDateTimeProvider dateProvider)
        {
            Guard.Against<ArgumentNullException>(noteRepo == null, nameof(noteRepo));
            Guard.Against<ArgumentNullException>(validator == null, nameof(validator));
            Guard.Against<ArgumentNullException>(dateProvider == null, nameof(dateProvider));
            _noteNoteRepo = noteRepo;
            _validator = validator;
            _dateProvider = dateProvider;
        }

        public HmmNote Create(HmmNote note)
        {
            if (!_validator.IsValidEntity(note, ProcessResult))
            {
                return null;
            }

            note.CreateDate = _dateProvider.UtcNow;
            note.LastModifiedDate = _dateProvider.UtcNow;
            var ret = _noteNoteRepo.Add(note);
            if (ret == null)
            {
                ProcessResult.PropagandaResult(_noteNoteRepo.ProcessMessage);
            }
            return ret;
        }

        public HmmNote Update(HmmNote note)
        {
            if (!_validator.IsValidEntity(note, ProcessResult))
            {
                return null;
            }

            // make sure not update note which get cached in current session
            note.LastModifiedDate = _dateProvider.UtcNow;
            var ret = _noteNoteRepo.Update(note);
            if (ret == null)
            {
                ProcessResult.PropagandaResult(_noteNoteRepo.ProcessMessage);
            }

            return ret;
        }

        public bool Delete(int noteId)
        {
            var note = GetNoteById(noteId);

            if (note == null)
            {
                ProcessResult.AddErrorMessage($"Cannot find note with id {noteId}", true);
                return false;
            }

            note.IsDeleted = true;
            var deletedNote = Update(note);
            if (deletedNote != null)
            {
                return true;
            }

            ProcessResult.PropagandaResult(_noteNoteRepo.ProcessMessage);
            return false;
        }

        public HmmNote GetNoteById(int id, bool includeDeleted = false)
        {
            var note = GetNotes(includeDeleted).FirstOrDefault(n => n.Id == id);
            return note;
        }

        public IEnumerable<HmmNote> GetNotes(bool includeDeleted = false)
        {
            var notes = includeDeleted ? _noteNoteRepo.GetEntities() :
                _noteNoteRepo.GetEntities().Where(n => !n.IsDeleted);

            return notes;
        }

        public ProcessingResult ProcessResult { get; } = new ProcessingResult();
    }
}