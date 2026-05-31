using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;

namespace Hmm.Core
{
    /// <summary>
    /// Per-author user-settings store for the cloudApi tier. Backs the
    /// <c>/v1/profile/settings</c> endpoints (Phase P2). See
    /// <c>docs/user-profile-settings-sync.md</c>.
    /// </summary>
    public interface IUserSettingsManager
    {
        /// <summary>
        /// The author's stored settings, or a successful result with a
        /// <c>null</c> value when none exists yet (maps to HTTP 204).
        /// </summary>
        Task<ProcessingResult<AuthorSettings>> GetByAuthorIdAsync(int authorId);

        /// <summary>
        /// Insert or update the author's settings. Last-writer-wins by
        /// <see cref="AuthorSettings.LastModified"/>: a write whose
        /// stamp is not strictly newer than the stored row is a no-op
        /// that returns the stored (newer-or-equal) settings, so a
        /// racing stale device can't clobber a fresher one.
        /// </summary>
        Task<ProcessingResult<AuthorSettings>> UpsertAsync(AuthorSettings settings);
    }
}
