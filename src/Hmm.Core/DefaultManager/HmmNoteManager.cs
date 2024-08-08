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
        private readonly NoteValidator _validator;
        private readonly IDateTimeProvider _dateProvider;
        private readonly ITagManager _tagManager;

        #endregion private fields

        public HmmNoteManager(IVersionRepository<HmmNoteDao> noteRepository, IMapper mapper, ITagManager tagManager, IDateTimeProvider dateProvider)
        {
            Guard.Against<ArgumentNullException>(noteRepository == null, nameof(noteRepository));
            Guard.Against<ArgumentNullException>(mapper == null, nameof(mapper));
            Guard.Against<ArgumentNullException>(tagManager == null, nameof(tagManager));
            Guard.Against<ArgumentNullException>(dateProvider == null, nameof(dateProvider));

            _noteRepository = noteRepository;
            _mapper = mapper;
            _dateProvider = dateProvider;
            _tagManager = tagManager;
            _validator = new NoteValidator(this);
        }

        public async Task<HmmNote> CreateAsync(HmmNote note)
        {
            try
            {
                ProcessResult.Rest();
                if (!await _validator.IsValidEntityAsync(note, ProcessResult))
                {
                    return null;
                }

                note.CreateDate = _dateProvider.UtcNow;
                note.LastModifiedDate = _dateProvider.UtcNow;
                var noteDao = _mapper.Map<HmmNoteDao>(note);
                if (noteDao == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot map note {note.Subject} to NoteDao");
                    return null;
                }
                var ret = await _noteRepository.AddAsync(noteDao);
                if (ret == null)
                {
                    ProcessResult.PropagandaResult(_noteRepository.ProcessMessage);
                    return null;
                }

                note.Id = ret.Id;
                note.Version = ret.Version;
                return note;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<HmmNote> UpdateAsync(HmmNote note)
        {
            try
            {
                ProcessResult.Rest();
                var isValid = await _validator.IsValidEntityAsync(note, ProcessResult);
                if (!isValid)
                {
                    return null;
                }

                // make sure not update note which get cached in current session
                var curNoteDao = await GetNoteByIdAsync(note.Id);
                if (curNoteDao == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot find note {note.Id} ");
                    return null;
                }

                var noteDao = _mapper.Map<HmmNoteDao>(note);
                if (noteDao == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot map note {note.Subject} to NoteDao");
                    return null;
                }

                noteDao.LastModifiedDate = _dateProvider.UtcNow;
                var updatedNoteDao = await _noteRepository.UpdateAsync(noteDao);
                if (updatedNoteDao == null)
                {
                    ProcessResult.PropagandaResult(_noteRepository.ProcessMessage);
                    return null;
                }

                var updatedNote = _mapper.Map<HmmNote>(updatedNoteDao);
                if (updatedNote == null)
                {
                    ProcessResult.AddErrorMessage("Cannot map NoteDao to note");
                    return null;
                }

                return updatedNote;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<List<Tag>> ApplyTag(HmmNote note, Tag tag)
        {
            try
            {
                ProcessResult.Rest();

                // Retrieve the HmmNote entity based on the HmmNoteDao
                var hmmNote = await GetNoteByIdAsync(note.Id);
                if (hmmNote == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot find note {note.Id}");
                    return null;
                }

                // Check if the tag exits in system
                var retrievedTag = (tag.Id > 0
                    ? await _tagManager.GetTagByIdAsync(tag.Id)
                    : await _tagManager.GetTagByNameAsync(tag.Name)) ?? await _tagManager.CreateAsync(tag);

                if (retrievedTag == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot create tag {tag.Name}");
                    return null;
                }

                // Check if the tag is already associated with the HmmNote entity
                if (hmmNote.Tags.Any(t => t.Id == retrievedTag.Id))
                {
                    ProcessResult.AddInfoMessage($"Tag {tag.Name} is already associated with note {note.Id}");
                    return hmmNote.Tags;
                }

                // Apply the tag to the HmmNote entity
                hmmNote.Tags.Add(retrievedTag);

                // Update the HmmNote entity in the repository
                var updatedHmmNote = await UpdateAsync(hmmNote);
                note.Tags = updatedHmmNote.Tags;

                // Return the list of tags associated with the HmmNote entity
                return updatedHmmNote.Tags;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<List<Tag>> RemoveTag(HmmNote note, int tagId)
        {
            try
            {
                ProcessResult.Rest();

                // Retrieve the HmmNote entity based on the HmmNoteDao
                var hmmNote = await GetNoteByIdAsync(note.Id);
                if (hmmNote == null)
                {
                    ProcessResult.AddErrorMessage($"Cannot find note {note.Id}");
                    return null;
                }

                // Check if the tag is already associated with the HmmNote entity
                var tag = hmmNote.Tags.FirstOrDefault(t => t.Id == tagId);
                if (tag == null)
                {
                    ProcessResult.AddInfoMessage($"Tag {tagId} is does not associated with note {note.Id}");
                    return hmmNote.Tags;
                }

                // Apply the tag to the HmmNote entity
                hmmNote.Tags.Remove(tag);

                // Update the HmmNote entity in the repository
                var updatedHmmNote = await UpdateAsync(hmmNote);
                note.Tags = updatedHmmNote.Tags;

                // Return the list of tags associated with the HmmNote entity
                return updatedHmmNote.Tags;
            }
            catch (Exception ex)
            {
                ProcessResult.WrapException(ex);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var noteDao = await GetNoteByIdAsync(id);

            if (noteDao == null)
            {
                ProcessResult.AddErrorMessage($"Cannot find note with id {id}", true);
                return false;
            }

            noteDao.IsDeleted = true;
            var deletedNoteDao = await UpdateAsync(noteDao);
            if (deletedNoteDao != null)
            {
                return true;
            }

            ProcessResult.PropagandaResult(_noteRepository.ProcessMessage);
            return false;
        }

        public async Task<HmmNote> GetNoteByIdAsync(int id, bool includeDelete = false)
        {
            var noteDao = await _noteRepository.GetEntityAsync(id);
            switch (noteDao)
            {
                case null:
                case { IsDeleted: true } when !includeDelete:
                    return null;
            }

            var note = _mapper.Map<HmmNote>(noteDao);
            if (note == null)
            {
                ProcessResult.AddErrorMessage("Cannot map NoteDao to Note");
                return null;
            }

            return note;
        }

        public async Task<PageList<HmmNote>> GetNotesAsync(Expression<Func<HmmNote, bool>> query = null, bool includeDeleted = false, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            PageList<HmmNoteDao> noteDaos;
            if (query != null)
            {
                var daoQuery = ExpressionMapper<HmmNote, HmmNoteDao>.MapExpression(query);
                var predicate = PredicateBuilder.True<HmmNoteDao>();
                predicate = predicate.And(daoQuery);
                predicate = includeDeleted ? predicate : predicate.And(n => !n.IsDeleted);
                noteDaos = await _noteRepository.GetEntitiesAsync(predicate, resourceCollectionParameters);
            }
            else
            {
                noteDaos = await _noteRepository.GetEntitiesAsync(null, resourceCollectionParameters);
            }

            var notes = _mapper.Map<PageList<HmmNote>>(noteDaos);
            return notes;
        }

        public ProcessingResult ProcessResult { get; } = new();
    }
}