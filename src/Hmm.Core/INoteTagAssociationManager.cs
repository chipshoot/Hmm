using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface INoteTagAssociationManager
    {
        /// <summary>
        /// Applies a tag to a note. Creates tag if it doesn't exist.
        /// </summary>
        Task<ProcessingResult<List<Tag>>> ApplyTagToNoteAsync(int noteId, Tag tag);

        /// <summary>
        /// Removes a tag association from a note.
        /// </summary>
        Task<ProcessingResult<List<Tag>>> RemoveTagFromNoteAsync(int noteId, int tagId);

        /// <summary>
        /// Applies multiple tags to a note in a single operation (optimized).
        /// </summary>
        Task<ProcessingResult<List<Tag>>> ApplyMultipleTagsAsync(int noteId, IEnumerable<Tag> tags);

        /// <summary>
        /// Gets all tags associated with a note.
        /// </summary>
        Task<ProcessingResult<List<Tag>>> GetNoteTagsAsync(int noteId);
    }
}