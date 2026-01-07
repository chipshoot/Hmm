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
    public class NoteTagAssociationManager : INoteTagAssociationManager
    {
        #region private fields

        private readonly IHmmNoteManager _noteManager;
        private readonly ITagManager _tagManager;

        #endregion private fields

        public NoteTagAssociationManager(IHmmNoteManager noteManager, ITagManager tagManager)
        {
            ArgumentNullException.ThrowIfNull(noteManager);
            ArgumentNullException.ThrowIfNull(tagManager);

            _noteManager = noteManager;
            _tagManager = tagManager;
        }

        public async Task<ProcessingResult<List<Tag>>> GetNoteTagsAsync(int noteId)
        {
            try
            {
                // Retrieve the HmmNote entity
                var hmmNoteResult = await _noteManager.GetNoteByIdAsync(noteId);
                if (!hmmNoteResult.Success)
                {
                    return ProcessingResult<List<Tag>>.Fail($"Cannot find note {noteId}: {hmmNoteResult.ErrorMessage}", hmmNoteResult.ErrorType);
                }

                var hmmNote = hmmNoteResult.Value;
                return ProcessingResult<List<Tag>>.Ok(hmmNote.Tags);
            }
            catch (Exception ex)
            {
                return ProcessingResult<List<Tag>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<List<Tag>>> ApplyTagToNoteAsync(int noteId, Tag tag)
        {
            try
            {
                // Retrieve the HmmNote entity
                var hmmNoteResult = await _noteManager.GetNoteByIdAsync(noteId);
                if (!hmmNoteResult.Success)
                {
                    return ProcessingResult<List<Tag>>.Fail($"Cannot find note {noteId}: {hmmNoteResult.ErrorMessage}", hmmNoteResult.ErrorType);
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
                    return ProcessingResult<List<Tag>>.Ok(hmmNote.Tags, $"Tag {tag.Name} is already associated with note {noteId}");
                }

                // Apply the tag to the HmmNote entity
                hmmNote.Tags.Add(retrievedTag);

                // Update the HmmNote entity in the repository
                var updatedHmmNoteResult = await _noteManager.UpdateAsync(hmmNote);
                if (!updatedHmmNoteResult.Success)
                {
                    return ProcessingResult<List<Tag>>.Fail(updatedHmmNoteResult.ErrorMessage, updatedHmmNoteResult.ErrorType);
                }

                var updatedHmmNote = updatedHmmNoteResult.Value;
                hmmNote.Tags = updatedHmmNote.Tags;

                // Return the list of tags associated with the HmmNote entity
                return ProcessingResult<List<Tag>>.Ok(updatedHmmNote.Tags);
            }
            catch (Exception ex)
            {
                return ProcessingResult<List<Tag>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<List<Tag>>> RemoveTagFromNoteAsync(int noteId, int tagId)
        {
            try
            {
                // Retrieve the HmmNote entity
                var hmmNoteResult = await _noteManager.GetNoteByIdAsync(noteId);
                if (!hmmNoteResult.Success)
                {
                    return ProcessingResult<List<Tag>>.Fail($"Cannot find note {noteId}: {hmmNoteResult.ErrorMessage}", hmmNoteResult.ErrorType);
                }
                var hmmNote = hmmNoteResult.Value;

                // Check if the tag is associated with the HmmNote entity
                var tag = hmmNote.Tags.FirstOrDefault(t => t.Id == tagId);
                if (tag == null)
                {
                    return ProcessingResult<List<Tag>>.Ok(hmmNote.Tags, $"Tag {tagId} is not associated with note {noteId}");
                }

                // Remove the tag from the HmmNote entity
                hmmNote.Tags.Remove(tag);

                // Update the HmmNote entity in the repository
                var updatedHmmNoteResult = await _noteManager.UpdateAsync(hmmNote);
                if (!updatedHmmNoteResult.Success)
                {
                    return ProcessingResult<List<Tag>>.Fail(updatedHmmNoteResult.ErrorMessage, updatedHmmNoteResult.ErrorType);
                }

                var updatedHmmNote = updatedHmmNoteResult.Value;
                hmmNote.Tags = updatedHmmNote.Tags;

                // Return the list of tags associated with the HmmNote entity
                return ProcessingResult<List<Tag>>.Ok(updatedHmmNote.Tags);
            }
            catch (Exception ex)
            {
                return ProcessingResult<List<Tag>>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<List<Tag>>> ApplyMultipleTagsAsync(int noteId, IEnumerable<Tag> tags)
        {
            try
            {
                if (tags == null || !tags.Any())
                {
                    return ProcessingResult<List<Tag>>.Invalid("Tags collection cannot be null or empty");
                }

                // Retrieve the HmmNote entity
                var hmmNoteResult = await _noteManager.GetNoteByIdAsync(noteId);
                if (!hmmNoteResult.Success)
                {
                    return ProcessingResult<List<Tag>>.Fail($"Cannot find note {noteId}: {hmmNoteResult.ErrorMessage}", hmmNoteResult.ErrorType);
                }
                var hmmNote = hmmNoteResult.Value;

                var tagsToApply = new List<Tag>();
                var errors = new List<string>();

                // Process all tags - lookup or create each one
                foreach (var tag in tags)
                {
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
                        errors.Add($"Cannot create or find tag {tag.Name}");
                        continue;
                    }

                    if (!retrievedTag.IsActivated)
                    {
                        errors.Add($"Tag {tag.Name} is deactivated");
                        continue;
                    }

                    // Check if tag is already associated
                    if (hmmNote.Tags.Any(t => t.Id == retrievedTag.Id))
                    {
                        continue; // Skip duplicates silently
                    }

                    tagsToApply.Add(retrievedTag);
                }

                // Apply all valid tags at once
                if (tagsToApply.Any())
                {
                    foreach (var tag in tagsToApply)
                    {
                        hmmNote.Tags.Add(tag);
                    }

                    // Single update operation for all tags
                    var updatedHmmNoteResult = await _noteManager.UpdateAsync(hmmNote);
                    if (!updatedHmmNoteResult.Success)
                    {
                        return ProcessingResult<List<Tag>>.Fail(updatedHmmNoteResult.ErrorMessage, updatedHmmNoteResult.ErrorType);
                    }

                    hmmNote.Tags = updatedHmmNoteResult.Value.Tags;
                }

                // Return result with warnings if any tags failed
                if (errors.Any())
                {
                    var errorMessage = $"Applied {tagsToApply.Count} tags. Errors: {string.Join("; ", errors)}";
                    return ProcessingResult<List<Tag>>.Ok(hmmNote.Tags, errorMessage);
                }

                return ProcessingResult<List<Tag>>.Ok(hmmNote.Tags, $"Successfully applied {tagsToApply.Count} tags");
            }
            catch (Exception ex)
            {
                return ProcessingResult<List<Tag>>.FromException(ex);
            }
        }
    }
}