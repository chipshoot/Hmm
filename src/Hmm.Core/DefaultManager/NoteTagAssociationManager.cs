using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
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
                ProcessingResult<Tag> tagResult = null;
                if (tag.Id > 0)
                {
                    tagResult = await _tagManager.GetTagByIdAsync(tag.Id);
                    if (tagResult.Success)
                    {
                        retrievedTag = tagResult.Value;
                    }
                }
                else
                {
                    tagResult = await _tagManager.GetTagByNameAsync(tag.Name);
                    if (tagResult.Success && !tagResult.IsNotFound)
                    {
                        retrievedTag = tagResult.Value;
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
                    if (tagResult.ErrorType == ErrorCategory.Deleted)
                    {
                        return ProcessingResult<List<Tag>>.Deleted($"Cannot apply deactivated tag {tag.Name}");
                    }

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

                var tagsList = tags.ToList();
                var tagsToApply = new List<Tag>();
                var errors = new List<string>();

                // Separate tags by ID vs name lookup
                var tagsWithIds = tagsList.Where(t => t.Id > 0).ToList();
                var tagsWithNames = tagsList.Where(t => t.Id <= 0 && !string.IsNullOrWhiteSpace(t.Name)).ToList();

                // Batch retrieve tags by IDs (single query)
                if (tagsWithIds.Any())
                {
                    var ids = tagsWithIds.Select(t => t.Id).Distinct().ToList();
                    var tagsByIdsResult = await _tagManager.GetTagsByIdsAsync(ids);
                    if (tagsByIdsResult.Success && tagsByIdsResult.Value != null)
                    {
                        foreach (var tag in tagsWithIds)
                        {
                            if (tagsByIdsResult.Value.TryGetValue(tag.Id, out var retrievedTag))
                            {
                                if (!hmmNote.Tags.Any(t => t.Id == retrievedTag.Id) &&
                                    !tagsToApply.Any(t => t.Id == retrievedTag.Id))
                                {
                                    tagsToApply.Add(retrievedTag);
                                }
                            }
                            else
                            {
                                errors.Add($"Tag with ID {tag.Id} not found or deactivated");
                            }
                        }
                    }
                    else if (!tagsByIdsResult.Success)
                    {
                        errors.Add(tagsByIdsResult.ErrorMessage);
                    }
                }

                // Batch retrieve tags by names (single query)
                if (tagsWithNames.Any())
                {
                    var names = tagsWithNames.Select(t => t.Name).Distinct().ToList();
                    var tagsByNamesResult = await _tagManager.GetTagsByNamesAsync(names);
                    var foundTagsByName = tagsByNamesResult.Success && tagsByNamesResult.Value != null
                        ? tagsByNamesResult.Value
                        : new Dictionary<string, Tag>();

                    foreach (var tag in tagsWithNames)
                    {
                        var tagNameLower = tag.Name.Trim().ToLower();
                        if (foundTagsByName.TryGetValue(tagNameLower, out var retrievedTag))
                        {
                            // Tag exists, use it
                            if (!hmmNote.Tags.Any(t => t.Id == retrievedTag.Id) &&
                                !tagsToApply.Any(t => t.Id == retrievedTag.Id))
                            {
                                tagsToApply.Add(retrievedTag);
                            }
                        }
                        else
                        {
                            // Tag doesn't exist, create it
                            var createdTagResult = await _tagManager.CreateAsync(tag);
                            if (createdTagResult.Success && createdTagResult.Value != null)
                            {
                                if (!tagsToApply.Any(t => t.Id == createdTagResult.Value.Id))
                                {
                                    tagsToApply.Add(createdTagResult.Value);
                                }
                            }
                            else
                            {
                                errors.Add($"Failed to create tag '{tag.Name}': {createdTagResult.ErrorMessage}");
                            }
                        }
                    }
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