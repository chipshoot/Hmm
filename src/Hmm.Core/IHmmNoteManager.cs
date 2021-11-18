using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface IHmmNoteManager
    {
        HmmNote GetNoteById(int id, bool includeDelete = false);

        Task<HmmNote> GetNoteByIdAsync(int id, bool includeDelete = false);

        IEnumerable<HmmNote> GetNotes(bool includeDeleted = false);

        Task<IEnumerable<HmmNote>> GetNotesAsync(Expression<Func<HmmNote, bool>> query = null, bool includeDeleted = false);

        HmmNote Create(HmmNote note);

        Task<HmmNote> CreateAsync(HmmNote note);

        HmmNote Update(HmmNote note);

        Task<HmmNote> UpdateAsync(HmmNote note);

        bool Delete(int noteId);

        Task<bool> DeleteAsync(int noteId);

        ProcessingResult ProcessResult { get; }
    }
}