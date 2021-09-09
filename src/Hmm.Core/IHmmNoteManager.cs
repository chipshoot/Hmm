using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using System.Collections.Generic;

namespace Hmm.Core
{
    public interface IHmmNoteManager
    {
        HmmNote GetNoteById(int id, bool includeDelete = false);

        IEnumerable<HmmNote> GetNotes(bool includeDeleted = false);

        HmmNote Create(HmmNote note);

        HmmNote Update(HmmNote note);

        bool Delete(int noteId);

        ProcessingResult ProcessResult { get; }
    }
}