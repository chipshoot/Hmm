using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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

        public async Task<HmmNote> CreateAsync(HmmNote note)
        {
            if (!_validator.IsValidEntity(note, ProcessResult))
            {
                return null;
            }

            note.CreateDate = _dateProvider.UtcNow;
            note.LastModifiedDate = _dateProvider.UtcNow;
            var ret = await _noteNoteRepo.AddAsync(note);
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
            var curNote = GetNoteById(note.Id);
            if (curNote == null)
            {
                ProcessResult.AddErrorMessage($"Cannot find note {note.Id} ");
                return null;
            }

            curNote.Subject = note.Subject;
            curNote.Content = note.Content;
            curNote.IsDeleted = note.IsDeleted;
            curNote.Description = note.Description;
            curNote.LastModifiedDate = _dateProvider.UtcNow;
            var ret = _noteNoteRepo.Update(curNote);
            if (ret == null)
            {
                ProcessResult.PropagandaResult(_noteNoteRepo.ProcessMessage);
            }

            return ret;
        }

        public async Task<HmmNote> UpdateAsync(HmmNote note)
        {
            if (!_validator.IsValidEntity(note, ProcessResult))
            {
                return null;
            }

            // make sure not update note which get cached in current session
            var curNote = await GetNoteByIdAsync(note.Id);
            if (curNote == null)
            {
                ProcessResult.AddErrorMessage($"Cannot find note {note.Id} ");
                return null;
            }

            curNote.Subject = note.Subject;
            curNote.Content = note.Content;
            curNote.IsDeleted = note.IsDeleted;
            curNote.Description = note.Description;
            curNote.LastModifiedDate = _dateProvider.UtcNow;
            var ret = await _noteNoteRepo.UpdateAsync(curNote);
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

        public async Task<bool> DeleteAsync(int noteId)
        {
            var note = await GetNoteByIdAsync(noteId);

            if (note == null)
            {
                ProcessResult.AddErrorMessage($"Cannot find note with id {noteId}", true);
                return false;
            }

            note.IsDeleted = true;
            var deletedNote = await UpdateAsync(note);
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

        public async Task<HmmNote> GetNoteByIdAsync(int id, bool includeDelete = false)
        {
            var note = await _noteNoteRepo.GetEntityAsync(id);
            if (note is { IsDeleted: true } && !includeDelete)
            {
                return null;
            }

            return note;
        }

        public IEnumerable<HmmNote> GetNotes(bool includeDeleted = false)
        {
            var notes = includeDeleted ? _noteNoteRepo.GetEntities() :
                _noteNoteRepo.GetEntities().Where(n => !n.IsDeleted);

            return notes;
        }

        public async Task<IEnumerable<HmmNote>> GetNotesAsync(Expression<Func<HmmNote, bool>> query = null, bool includeDeleted = false)
        {
            if (!includeDeleted)
            {
                if (query != null)
                {
                    var compiled = query.Compile();
                    query = n => compiled(n) && !n.IsDeleted;
                }
                else
                {
                    query = n => !n.IsDeleted;
                }
            }
            var notes = await _noteNoteRepo.GetEntitiesAsync(query);
            return notes;
        }

        public ProcessingResult ProcessResult { get; } = new();
    }
}