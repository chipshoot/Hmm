using AutoMapper;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
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

        private readonly IVersionRepository<HmmNoteDao> _noteRepository;
        private readonly IMapper _mapper;
        private readonly IHmmValidator<HmmNote> _validator;
        private readonly IDateTimeProvider _dateProvider;
        private readonly IEntityLookup _lookup;

        #endregion private fields

        public HmmNoteManager(IVersionRepository<HmmNoteDao> noteRepository, IMapper mapper, IEntityLookup lookup, IDateTimeProvider dateProvider, IHmmValidator<HmmNote> validator)
        {
            ArgumentNullException.ThrowIfNull(noteRepository);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(dateProvider);
            ArgumentNullException.ThrowIfNull(validator);

            _noteRepository = noteRepository;
            _mapper = mapper;
            _dateProvider = dateProvider;
            _lookup = lookup;
            _validator = validator;
        }

        public async Task<ProcessingResult<PageList<HmmNote>>> GetNotesAsync(Expression<Func<HmmNote, bool>> query = null, bool includeDeleted = false, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            ProcessingResult<PageList<HmmNoteDao>> noteDaosResult;
            if (query != null)
            {
                var daoQuery = ExpressionMapper<HmmNote, HmmNoteDao>.MapExpression(query);
                var predicate = PredicateBuilder.True<HmmNoteDao>();
                predicate = predicate.And(daoQuery);
                predicate = includeDeleted ? predicate : predicate.And(n => !n.IsDeleted);
                noteDaosResult = await _noteRepository.GetEntitiesAsync(predicate, resourceCollectionParameters);
            }
            else
            {
                noteDaosResult = includeDeleted
                    ? await _noteRepository.GetEntitiesAsync(null, resourceCollectionParameters)
                    : await _noteRepository.GetEntitiesAsync(n => !n.IsDeleted, resourceCollectionParameters);
            }

            if (!noteDaosResult.Success)
            {
                return ProcessingResult<PageList<HmmNote>>.Fail(noteDaosResult.ErrorMessage, noteDaosResult.ErrorType);
            }

            var notes = _mapper.Map<PageList<HmmNote>>(noteDaosResult.Value);
            return ProcessingResult<PageList<HmmNote>>.Ok(notes);
        }

        public async Task<ProcessingResult<HmmNote>> GetNoteByIdAsync(int id, bool includeDelete = false)
        {
            var noteDaoResult = await _lookup.GetEntityAsync<HmmNoteDao>(id);

            if (!noteDaoResult.Success || noteDaoResult.IsNotFound)
            {
                return ProcessingResult<HmmNote>.Fail(noteDaoResult.ErrorMessage, noteDaoResult.ErrorType);
            }

            var noteDao = noteDaoResult.Value;
            if (noteDao.IsDeleted && !includeDelete)
            {
                return ProcessingResult<HmmNote>.Deleted($"Note with ID {id} has been deleted");
            }

            var note = _mapper.Map<HmmNote>(noteDao);
            if (note == null)
            {
                return ProcessingResult<HmmNote>.Fail("Cannot convert HmmNoteDao to HmmNote");
            }

            return ProcessingResult<HmmNote>.Ok(note);
        }

        public async Task<ProcessingResult<HmmNote>> CreateAsync(HmmNote note)
        {
            try
            {
                var validationResult = await _validator.ValidateEntityAsync(note);
                if (!validationResult.Success)
                {
                    return ProcessingResult<HmmNote>.Invalid(validationResult.GetWholeMessage());
                }

                note.CreateDate = _dateProvider.UtcNow;
                note.LastModifiedDate = _dateProvider.UtcNow;
                var noteDao = _mapper.Map<HmmNoteDao>(note);
                if (noteDao == null)
                {
                    return ProcessingResult<HmmNote>.Fail($"Cannot convert note {note.Subject} to NoteDao");
                }

                var addedNoteDaoResult = await _noteRepository.AddAsync(noteDao);
                if (!addedNoteDaoResult.Success)
                {
                    return ProcessingResult<HmmNote>.Fail(addedNoteDaoResult.ErrorMessage, addedNoteDaoResult.ErrorType);
                }

                var createdNote = _mapper.Map<HmmNote>(addedNoteDaoResult.Value);
                return ProcessingResult<HmmNote>.Ok(createdNote);
            }
            catch (Exception ex)
            {
                return ProcessingResult<HmmNote>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<HmmNote>> UpdateAsync(HmmNote note)
        {
            try
            {
                var validationResult = await _validator.ValidateEntityAsync(note);
                if (!validationResult.Success)
                {
                    return ProcessingResult<HmmNote>.Invalid(validationResult.GetWholeMessage());
                }

                // Make sure not to update note which is cached in current session
                var curNoteResult = await GetNoteByIdAsync(note.Id);
                if (!curNoteResult.Success)
                {
                    return ProcessingResult<HmmNote>.NotFound($"Cannot update note: {note.Id}, because system cannot find it in data source");
                }

                var noteDao = _mapper.Map<HmmNoteDao>(note);
                if (noteDao == null)
                {
                    return ProcessingResult<HmmNote>.Fail($"Cannot convert note {note.Subject} to NoteDao");
                }

                noteDao.LastModifiedDate = _dateProvider.UtcNow;
                var updatedNoteDaoResult = await _noteRepository.UpdateAsync(noteDao);
                if (!updatedNoteDaoResult.Success)
                {
                    return ProcessingResult<HmmNote>.Fail(updatedNoteDaoResult.ErrorMessage, updatedNoteDaoResult.ErrorType);
                }

                var updatedNote = _mapper.Map<HmmNote>(updatedNoteDaoResult.Value);
                if (updatedNote == null)
                {
                    return ProcessingResult<HmmNote>.Fail("Cannot convert NoteDao to HmmNote");
                }

                return ProcessingResult<HmmNote>.Ok(updatedNote);
            }
            catch (Exception ex)
            {
                return ProcessingResult<HmmNote>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<Unit>> DeleteAsync(int id)
        {
            var noteResult = await GetNoteByIdAsync(id);

            if (!noteResult.Success)
            {
                return ProcessingResult<Unit>.NotFound($"Cannot find note with id {id}");
            }

            var note = noteResult.Value;
            note.IsDeleted = true;
            var deletedNoteResult = await UpdateAsync(note);

            if (!deletedNoteResult.Success)
            {
                return ProcessingResult<Unit>.Fail(deletedNoteResult.ErrorMessage, deletedNoteResult.ErrorType);
            }

            return ProcessingResult<Unit>.Ok(Unit.Value, $"Note with id {id} has been deleted");
        }
    }
}