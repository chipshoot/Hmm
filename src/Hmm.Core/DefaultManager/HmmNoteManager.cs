using AutoMapper;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
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

        private readonly IVersionRepository<HmmNoteDao> _noteRepository;
        private readonly IMapper _mapper;
        private readonly IHmmValidator<HmmNote> _validator;
        private readonly IDateTimeProvider _dateProvider;
        private readonly ITagManager _tagManager;
        private readonly IEntityLookup _lookup;

        #endregion private fields

        public HmmNoteManager(IVersionRepository<HmmNoteDao> noteRepository, IMapper mapper, ITagManager tagManager, IEntityLookup lookup, IDateTimeProvider dateProvider)
        {
            Guard.Against<ArgumentNullException>(noteRepository == null, nameof(noteRepository));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            Guard.Against<ArgumentNullException>(tagManager == null, nameof(tagManager));
            Guard.Against<ArgumentNullException>(dateProvider == null, nameof(dateProvider));

            _noteRepository = noteRepository;
            _mapper = mapper;
            _dateProvider = dateProvider;
            _tagManager = tagManager;
            _lookup = lookup;
            _validator = new NoteValidator(_lookup);
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

            if (!noteDaoResult.Success)
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

        public async Task<ProcessingResult<List<Tag>>> ApplyTag(HmmNote note, Tag tag)
        {
            try
            {
                // Retrieve the HmmNote entity
                var hmmNoteResult = await GetNoteByIdAsync(note.Id);
                if (!hmmNoteResult.Success)
                {
                    return ProcessingResult<List<Tag>>.Fail($"Cannot find note {note.Id}: {hmmNoteResult.ErrorMessage}", hmmNoteResult.ErrorType);
                }
                var hmmNote = hmmNoteResult.Value;

                // Check if the tag exists in system
                Tag retrievedTag = null;
                if (tag.Id > 0)
                {
                    var tagResult = await _tagManager.GetTagByIdAsync(tag.Id);
                    if (tagResult.Success)
                    {
                        retrievedTag = tagResult.Value;
                    }
                }
                else
                {
                    var tagByNameResult = await _tagManager.GetTagByNameAsync(tag.Name);
                    if (tagByNameResult.Success)
                    {
                        retrievedTag = tagByNameResult.Value;
                    }
                    else
                    {
                        var createdTagResult = await _tagManager.CreateAsync(tag);
                        if (createdTagResult.Success)
                        {
                            retrievedTag = createdTagResult.Value;
                        }
                    }
                }

                if (retrievedTag == null)
                {
                    return ProcessingResult<List<Tag>>.Fail($"Cannot create or find tag {tag.Name}");
                }

                if (!retrievedTag.IsActivated)
                {
                    return ProcessingResult<List<Tag>>.Invalid($"Cannot apply deactivated tag {tag.Name}");
                }

                // Check if the tag is already associated with the HmmNote entity
                if (hmmNote.Tags.Any(t => t.Id == retrievedTag.Id))
                {
                    return ProcessingResult<List<Tag>>.Ok(hmmNote.Tags, $"Tag {tag.Name} is already associated with note {note.Id}");
                }

                // Apply the tag to the HmmNote entity
                hmmNote.Tags.Add(retrievedTag);

                // Update the HmmNote entity in the repository
                var updatedHmmNoteResult = await UpdateAsync(hmmNote);
                if (!updatedHmmNoteResult.Success)
                {
                    return ProcessingResult<List<Tag>>.Fail(updatedHmmNoteResult.ErrorMessage, updatedHmmNoteResult.ErrorType);
                }

                var updatedHmmNote = updatedHmmNoteResult.Value;
                note.Tags = updatedHmmNote.Tags;

                // Return the list of tags associated with the HmmNote entity
                return ProcessingResult<List<Tag>>.Ok(updatedHmmNote.Tags);
            }
            catch (Exception ex)
            {
                return ProcessingResult<List<Tag>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<List<Tag>>> RemoveTag(HmmNote note, int tagId)
        {
            try
            {
                // Retrieve the HmmNote entity
                var hmmNoteResult = await GetNoteByIdAsync(note.Id);
                if (!hmmNoteResult.Success)
                {
                    return ProcessingResult<List<Tag>>.Fail($"Cannot find note {note.Id}: {hmmNoteResult.ErrorMessage}", hmmNoteResult.ErrorType);
                }
                var hmmNote = hmmNoteResult.Value;

                // Check if the tag is associated with the HmmNote entity
                var tag = hmmNote.Tags.FirstOrDefault(t => t.Id == tagId);
                if (tag == null)
                {
                    return ProcessingResult<List<Tag>>.Ok(hmmNote.Tags, $"Tag {tagId} is not associated with note {note.Id}");
                }

                // Remove the tag from the HmmNote entity
                hmmNote.Tags.Remove(tag);

                // Update the HmmNote entity in the repository
                var updatedHmmNoteResult = await UpdateAsync(hmmNote);
                if (!updatedHmmNoteResult.Success)
                {
                    return ProcessingResult<List<Tag>>.Fail(updatedHmmNoteResult.ErrorMessage, updatedHmmNoteResult.ErrorType);
                }

                var updatedHmmNote = updatedHmmNoteResult.Value;
                note.Tags = updatedHmmNote.Tags;

                // Return the list of tags associated with the HmmNote entity
                return ProcessingResult<List<Tag>>.Ok(updatedHmmNote.Tags);
            }
            catch (Exception ex)
            {
                return ProcessingResult<List<Tag>>.FromException(ex);
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