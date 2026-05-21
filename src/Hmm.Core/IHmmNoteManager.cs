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

        /// <summary>
        /// Look up a note by its cross-device-stable Uuid. Used by
        /// the cloudApi sync provider — the wire identity is the
        /// Uuid, not the local int Id. Returns
        /// <see cref="ProcessingResult.NotFound"/> when no row
        /// carries that Uuid; never silently returns a different
        /// note.
        /// </summary>
        Task<ProcessingResult<HmmNote>> GetNoteByUuidAsync(string uuid, bool includeDeleted = false);

        Task<ProcessingResult<PageList<HmmNote>>> GetNotesAsync(Expression<Func<HmmNote, bool>> query = null, bool includeDeleted = false, ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Creates a new note in the data source.
        /// </summary>
        /// <param name="note">The note to create.</param>
        /// <param name="commitChanges">
        /// If true (default), changes are committed immediately.
        /// If false, changes are tracked but not committed - caller must use IUnitOfWork.CommitAsync() to persist.
        /// Set to false when this operation is part of a larger transaction (e.g., GasLog creation).
        /// </param>
        Task<ProcessingResult<HmmNote>> CreateAsync(HmmNote note, bool commitChanges = true);

        /// <summary>
        /// Updates an existing note in the data source.
        /// </summary>
        /// <param name="note">The note with updated values.</param>
        /// <param name="commitChanges">
        /// If true (default), changes are committed immediately.
        /// If false, changes are tracked but not committed - caller must use IUnitOfWork.CommitAsync() to persist.
        /// Set to false when this operation is part of a larger transaction.
        /// </param>
        Task<ProcessingResult<HmmNote>> UpdateAsync(HmmNote note, bool commitChanges = true);

        Task<ProcessingResult<Unit>> DeleteAsync(int noteId);
    }
}