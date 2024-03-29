﻿using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager
{
    public class HmmNoteManager : IHmmNoteManager
    {
        #region private fields

        private readonly IVersionRepository<HmmNote> _noteRepo;
        private readonly IHmmValidator<HmmNote> _validator;
        private readonly IDateTimeProvider _dateProvider;

        #endregion private fields

        public HmmNoteManager(IVersionRepository<HmmNote> noteRepo, IHmmValidator<HmmNote> validator, IDateTimeProvider dateProvider)
        {
            Guard.Against<ArgumentNullException>(noteRepo == null, nameof(noteRepo));
            Guard.Against<ArgumentNullException>(validator == null, nameof(validator));
            Guard.Against<ArgumentNullException>(dateProvider == null, nameof(dateProvider));
            _noteRepo = noteRepo;
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
            var ret = _noteRepo.Add(note);
            if (ret == null)
            {
                ProcessResult.PropagandaResult(_noteRepo.ProcessMessage);
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
            var ret = await _noteRepo.AddAsync(note);
            if (ret == null)
            {
                ProcessResult.PropagandaResult(_noteRepo.ProcessMessage);
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
            var ret = _noteRepo.Update(curNote);
            if (ret == null)
            {
                ProcessResult.PropagandaResult(_noteRepo.ProcessMessage);
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
            var ret = await _noteRepo.UpdateAsync(curNote);
            if (ret == null)
            {
                ProcessResult.PropagandaResult(_noteRepo.ProcessMessage);
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

            ProcessResult.PropagandaResult(_noteRepo.ProcessMessage);
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

            ProcessResult.PropagandaResult(_noteRepo.ProcessMessage);
            return false;
        }

        public HmmNote GetNoteById(int id, bool includeDeleted = false)
        {
            var note = _noteRepo.GetEntity(id);
            if (includeDeleted)
            {
                return note;
            }

            if (note != null)
            {
                return note.IsDeleted ? null : note;
            }

            return null;
        }

        public async Task<HmmNote> GetNoteByIdAsync(int id, bool includeDelete = false)
        {
            var note = await _noteRepo.GetEntityAsync(id);
            if (note is { IsDeleted: true } && !includeDelete)
            {
                return null;
            }

            return note;
        }

        public PageList<HmmNote> GetNotes(Expression<Func<HmmNote, bool>> query, bool includeDeleted = false, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var predicate = PredicateBuilder.True<HmmNote>();
            predicate = query == null ? predicate : predicate.And(query);
            predicate = includeDeleted ? predicate : predicate.And(n => !n.IsDeleted);
            var notes = _noteRepo.GetEntities(predicate, resourceCollectionParameters);
            return notes;
        }

        public async Task<PageList<HmmNote>> GetNotesAsync(Expression<Func<HmmNote, bool>> query = null, bool includeDeleted = false, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var predicate = PredicateBuilder.True<HmmNote>();
            predicate = query == null ? predicate : predicate.And(query);
            predicate = includeDeleted ? predicate : predicate.And(n => !n.IsDeleted);
            var notes = await _noteRepo.GetEntitiesAsync(predicate, resourceCollectionParameters);
            return notes;
        }

        public ProcessingResult ProcessResult { get; } = new();
    }
}