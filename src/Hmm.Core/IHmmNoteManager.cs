using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface IHmmNoteManager
    {
        HmmNote GetNoteById(int id, bool includeDelete = false);

        Task<HmmNote> GetNoteByIdAsync(int id, bool includeDelete = false);

        PageList<HmmNote> GetNotes(Expression<Func<HmmNote, bool>> query = null, bool includeDeleted = false, ResourceCollectionParameters resourceCollectionParameters = null);

        Task<PageList<HmmNote>> GetNotesAsync(Expression<Func<HmmNote, bool>> query = null, bool includeDeleted = false, ResourceCollectionParameters resourceCollectionParameters = null);

        HmmNote Create(HmmNote note);

        Task<HmmNote> CreateAsync(HmmNote note);

        HmmNote Update(HmmNote note);

        Task<HmmNote> UpdateAsync(HmmNote note);

        bool Delete(int noteId);

        Task<bool> DeleteAsync(int noteId);

        ProcessingResult ProcessResult { get; }
    }
}