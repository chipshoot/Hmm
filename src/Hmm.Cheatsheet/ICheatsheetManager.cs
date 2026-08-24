using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;

namespace Hmm.Cheatsheet
{
    /// <summary>
    /// CRUD over cheatsheet cards. Every card is one HmmNote under the
    /// Hmm.CheatsheetMan.Cheatsheet catalog, addressed by the note subject
    /// "Cheatsheet:{cardId}".
    /// </summary>
    public interface ICheatsheetManager
    {
        /// <summary>
        /// Returns the current author's cards, optionally narrowed by wallet
        /// group and/or tag (both case-insensitive), ordered by title then id,
        /// and paged.
        /// </summary>
        Task<ProcessingResult<PageList<CheatsheetCard>>> GetCardsAsync(
            string walletGroup = null,
            string tag = null,
            ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Returns one card by its stable card id (not the note's int id).
        /// </summary>
        Task<ProcessingResult<CheatsheetCard>> GetCardByIdAsync(string cardId);

        /// <summary>
        /// Stores a new card. A blank <see cref="CheatsheetCard.Id"/> is filled
        /// with a fresh v4 GUID; a card id that already exists conflicts rather
        /// than creating a second note under the same subject.
        /// </summary>
        Task<ProcessingResult<CheatsheetCard>> CreateAsync(CheatsheetCard card, bool commitChanges = true);

        /// <summary>
        /// Replaces the stored card. The backing note's cross-device identity
        /// (Uuid), audit dates and concurrency token are carried forward.
        /// </summary>
        Task<ProcessingResult<CheatsheetCard>> UpdateAsync(CheatsheetCard card, bool commitChanges = true);

        /// <summary>Soft-deletes the card's backing note.</summary>
        Task<ProcessingResult<Unit>> DeleteAsync(string cardId);
    }
}
