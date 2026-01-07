using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface IHmmNoteManager
    {
        Task<ProcessingResult<HmmNote>> GetNoteByIdAsync(int id, bool includeDelete = false);

        Task<ProcessingResult<PageList<HmmNote>>> GetNotesAsync(Expression<Func<HmmNote, bool>> query = null, bool includeDeleted = false, ResourceCollectionParameters resourceCollectionParameters = null);

        Task<ProcessingResult<HmmNote>> CreateAsync(HmmNote note);

        Task<ProcessingResult<HmmNote>> UpdateAsync(HmmNote note);

        Task<ProcessingResult<Unit>> DeleteAsync(int noteId);
    }
}