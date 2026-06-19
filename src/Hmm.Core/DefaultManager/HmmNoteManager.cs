using AutoMapper;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager
{
    public class HmmNoteManager : IHmmNoteManager
    {
        #region private fields

        private readonly IVersionRepository<HmmNoteDao> _noteRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHmmValidator<HmmNote> _validator;
        private readonly IDateTimeProvider _dateProvider;
        private readonly IEntityLookup _lookup;

        #endregion private fields

        public HmmNoteManager(
            IVersionRepository<HmmNoteDao> noteRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IEntityLookup lookup,
            IDateTimeProvider dateProvider,
            IHmmValidator<HmmNote> validator)
        {
            ArgumentNullException.ThrowIfNull(noteRepository);
            ArgumentNullException.ThrowIfNull(unitOfWork);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(dateProvider);
            ArgumentNullException.ThrowIfNull(validator);

            _noteRepository = noteRepository;
            _unitOfWork = unitOfWork;
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

        public async Task<ProcessingResult<HmmNote>> GetNoteByUuidAsync(string uuid, bool includeDeleted = false)
        {
            if (string.IsNullOrWhiteSpace(uuid))
            {
                return ProcessingResult<HmmNote>.Invalid("Uuid is required.");
            }

            // Reuse the generic lookup repository's PageList query
            // shape — Uuid is indexed unique so this is a B-tree
            // probe even though we're going through the repository.
            var query = PredicateBuilder.True<HmmNoteDao>();
            query = query.And(n => n.Uuid == uuid);
            if (!includeDeleted) query = query.And(n => !n.IsDeleted);

            var page = await _noteRepository.GetEntitiesAsync(query, new ResourceCollectionParameters
            {
                PageSize = 1,
            });
            if (!page.Success)
            {
                return ProcessingResult<HmmNote>.Fail(page.ErrorMessage, page.ErrorType);
            }
            var dao = page.Value?.FirstOrDefault();
            if (dao == null)
            {
                return ProcessingResult<HmmNote>.NotFound($"Note with Uuid {uuid} not found");
            }
            return _mapper.MapWithNullCheck<HmmNoteDao, HmmNote>(dao);
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

            return _mapper.MapWithNullCheck<HmmNoteDao, HmmNote>(noteDao);
        }

        public async Task<ProcessingResult<HmmNote>> CreateAsync(HmmNote note, bool commitChanges = true)
        {
            try
            {
                var validationResult = await _validator.ValidateEntityAsync(note);
                if (!validationResult.Success)
                {
                    return ProcessingResult<HmmNote>.Invalid(validationResult.GetWholeMessage());
                }

                // Phase 15b: ensure every row gets a cross-device-stable
                // Uuid. Sync clients pass their own; legacy callers
                // omit it and the server assigns. We use Guid format
                // (8-4-4-4-12) verbatim — matches the Dart `uuid`
                // package's default v4 output.
                if (string.IsNullOrWhiteSpace(note.Uuid))
                {
                    note.Uuid = Guid.NewGuid().ToString();
                }

                note.CreateDate = _dateProvider.UtcNow;
                note.NoteDate = note.NoteDate == default ? _dateProvider.UtcNow : note.NoteDate;
                note.LastModifiedDate = _dateProvider.UtcNow;
                note.CreatedBy = note.Author?.AccountName;
                note.LastModifiedBy = note.Author?.AccountName;
                var noteDaoResult = _mapper.MapWithNullCheck<HmmNote, HmmNoteDao>(note, $"Cannot convert note {note.Subject} to NoteDao");
                if (!noteDaoResult.Success)
                {
                    return ProcessingResult<HmmNote>.Fail(noteDaoResult.ErrorMessage);
                }

                var addedNoteDaoResult = await _noteRepository.AddAsync(noteDaoResult.Value);
                if (!addedNoteDaoResult.Success)
                {
                    return ProcessingResult<HmmNote>.Fail(addedNoteDaoResult.ErrorMessage, addedNoteDaoResult.ErrorType);
                }

                if (commitChanges)
                {
                    await _unitOfWork.CommitAsync();
                }

                var createdNote = _mapper.Map<HmmNote>(addedNoteDaoResult.Value);
                return ProcessingResult<HmmNote>.Ok(createdNote);
            }
            catch (Exception ex)
            {
                return ProcessingResult<HmmNote>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<HmmNote>> UpdateAsync(HmmNote note, bool commitChanges = true)
        {
            try
            {
                var validationResult = await _validator.ValidateEntityAsync(note);
                if (!validationResult.Success)
                {
                    return ProcessingResult<HmmNote>.Invalid(validationResult.GetWholeMessage());
                }

                // Backfill on the first update of a legacy
                // (pre-Phase-15b) row so the Uuid eventually
                // populates across the table without a separate
                // migration pass.
                if (string.IsNullOrWhiteSpace(note.Uuid))
                {
                    note.Uuid = Guid.NewGuid().ToString();
                }

                var noteDaoResult = _mapper.MapWithNullCheck<HmmNote, HmmNoteDao>(note, $"Cannot convert note {note.Subject} to NoteDao");
                if (!noteDaoResult.Success)
                {
                    return ProcessingResult<HmmNote>.Fail(noteDaoResult.ErrorMessage);
                }

                var noteDao = noteDaoResult.Value;
                noteDao.LastModifiedDate = _dateProvider.UtcNow;
                noteDao.LastModifiedBy = noteDao.Author?.AccountName;
                var updatedNoteDaoResult = await _noteRepository.UpdateAsync(noteDao);
                if (!updatedNoteDaoResult.Success)
                {
                    return ProcessingResult<HmmNote>.Fail(updatedNoteDaoResult.ErrorMessage, updatedNoteDaoResult.ErrorType);
                }

                if (commitChanges)
                {
                    await _unitOfWork.CommitAsync();
                }

                return _mapper.MapWithNullCheck<HmmNoteDao, HmmNote>(updatedNoteDaoResult.Value);
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