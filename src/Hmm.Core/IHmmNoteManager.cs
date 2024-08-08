using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface IHmmNoteManager
    {
        Task<HmmNote> GetNoteByIdAsync(int id, bool includeDelete = false);

        Task<PageList<HmmNote>> GetNotesAsync(Expression<Func<HmmNote, bool>> query = null, bool includeDeleted = false, ResourceCollectionParameters resourceCollectionParameters = null);

        Task<HmmNote> CreateAsync(HmmNote note);

        Task<HmmNote> UpdateAsync(HmmNote note);

        Task<List<Tag>> ApplyTag(HmmNote note, Tag tag);

        Task<List<Tag>> RemoveTag(HmmNote note, int tagId);

        Task<bool> DeleteAsync(int noteId);

        ProcessingResult ProcessResult { get; }
    }
}