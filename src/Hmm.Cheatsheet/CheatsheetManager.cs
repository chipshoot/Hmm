using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;

namespace Hmm.Cheatsheet
{
    /// <summary>
    /// Cheatsheet CRUD over the note store.
    ///
    /// Card content is opaque JSON in a text column, so wallet-group and tag
    /// filtering cannot be pushed into SQL, and paginating notes before
    /// deserializing them would page the wrong population. Every read therefore
    /// loads the author's cards for the catalog and filters/pages in memory.
    /// Wallets hold tens of cards; correctness wins over the round trip.
    /// </summary>
    public class CheatsheetManager : ICheatsheetManager
    {
        /// <summary>Note-store page size for the internal read-everything loop.</summary>
        private const int NotePageSize = 100;

        private readonly INoteSerializer<CheatsheetCard> _noteSerializer;
        private readonly IHmmValidator<CheatsheetCard> _validator;
        private readonly IHmmNoteManager _noteManager;
        private readonly IEntityLookup _lookupRepo;
        private readonly IAuthorProvider _authorProvider;

        public CheatsheetManager(
            INoteSerializer<CheatsheetCard> noteSerializer,
            IHmmValidator<CheatsheetCard> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo,
            IAuthorProvider authorProvider)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(noteManager);
            ArgumentNullException.ThrowIfNull(lookupRepo);
            ArgumentNullException.ThrowIfNull(authorProvider);

            _noteSerializer = noteSerializer;
            _validator = validator;
            _noteManager = noteManager;
            _lookupRepo = lookupRepo;
            _authorProvider = authorProvider;
        }

        public async Task<ProcessingResult<PageList<CheatsheetCard>>> GetCardsAsync(
            string walletGroup = null,
            string tag = null,
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var cardsResult = await LoadCardsAsync();
            if (!cardsResult.Success)
            {
                return ProcessingResult<PageList<CheatsheetCard>>.Fail(
                    cardsResult.ErrorMessage, cardsResult.ErrorType);
            }

            IEnumerable<CheatsheetCard> cards = cardsResult.Value;

            if (!string.IsNullOrWhiteSpace(walletGroup))
            {
                cards = cards.Where(c =>
                    string.Equals(c.WalletGroup, walletGroup, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                cards = cards.Where(c => c.Tags != null && c.Tags.Any(t =>
                    string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)));
            }

            // Deterministic order: the wallet has no user reordering, so title
            // then id is the whole ordering contract.
            var ordered = cards
                .OrderBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Id, StringComparer.Ordinal)
                .ToList();

            var (pageIndex, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var pageItems = ordered.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            return ProcessingResult<PageList<CheatsheetCard>>.Ok(
                new PageList<CheatsheetCard>(pageItems, ordered.Count, pageIndex, pageSize));
        }

        public async Task<ProcessingResult<CheatsheetCard>> GetCardByIdAsync(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return ProcessingResult<CheatsheetCard>.Invalid("Cheatsheet card id cannot be empty");
            }

            var noteResult = await FindNoteForCardAsync(cardId);
            if (!noteResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            return await _noteSerializer.GetEntity(noteResult.Value);
        }

        /// <summary>
        /// Finds a card's note by SUBJECT, never by decoding its content. The
        /// subject is the card's identity and stays readable when the content
        /// does not - matching on a decoded id would make a card with broken
        /// JSON invisible, so a save would create a duplicate note under the
        /// same subject and a delete could never reach the original.
        /// </summary>
        private async Task<ProcessingResult<HmmNote>> FindNoteForCardAsync(string cardId)
        {
            var notesResult = await GetAllNotesAsync();
            if (!notesResult.Success)
            {
                return ProcessingResult<HmmNote>.Fail(notesResult.ErrorMessage, notesResult.ErrorType);
            }

            var subject = CheatsheetCard.GetNoteSubject(cardId);
            var note = notesResult.Value.FirstOrDefault(n =>
                string.Equals(n.Subject, subject, StringComparison.Ordinal));

            return note != null
                ? ProcessingResult<HmmNote>.Ok(note)
                : ProcessingResult<HmmNote>.NotFound($"Cannot find cheatsheet card '{cardId}'");
        }

        private async Task<ProcessingResult<IList<CheatsheetCard>>> LoadCardsAsync()
        {
            var notesResult = await GetAllNotesAsync();
            if (!notesResult.Success)
            {
                return ProcessingResult<IList<CheatsheetCard>>.Fail(
                    notesResult.ErrorMessage, notesResult.ErrorType);
            }

            var cards = new List<CheatsheetCard>();
            foreach (var note in notesResult.Value)
            {
                var cardResult = await _noteSerializer.GetEntity(note);
                if (cardResult.Success && cardResult.Value != null)
                {
                    // A single unreadable note must not take the wallet down
                    // with it; it stays reachable by id for repair or delete.
                    cards.Add(cardResult.Value);
                }
            }

            return ProcessingResult<IList<CheatsheetCard>>.Ok(cards);
        }

        /// <summary>
        /// Pages until exhausted. A fixed ceiling here would silently hide
        /// cards - the user would simply never see them again.
        /// </summary>
        private async Task<ProcessingResult<IList<HmmNote>>> GetAllNotesAsync()
        {
            var authorResult = await _authorProvider.GetAuthorAsync();
            if (!authorResult.Success)
            {
                return ProcessingResult<IList<HmmNote>>.Fail(
                    authorResult.ErrorMessage, authorResult.ErrorType);
            }

            var author = authorResult.Value;
            var catalogId = await GetCatalogIdAsync();
            if (catalogId <= 0)
            {
                return ProcessingResult<IList<HmmNote>>.Fail(
                    $"Cannot find note catalog '{CheatsheetConstant.CheatsheetCatalogName}'",
                    ErrorCategory.NotFound);
            }

            var notes = new List<HmmNote>();
            var page = 1;

            while (true)
            {
                var parameters = new ResourceCollectionParameters
                {
                    PageNumber = page,
                    PageSize = NotePageSize
                };

                var pageResult = await _noteManager.GetNotesAsync(
                    n => n.Author.Id == author.Id && n.Catalog.Id == catalogId,
                    false,
                    parameters);

                if (!pageResult.Success)
                {
                    return ProcessingResult<IList<HmmNote>>.Fail(
                        pageResult.ErrorMessage, pageResult.ErrorType);
                }

                var pageList = pageResult.Value;
                if (pageList == null || pageList.Count == 0)
                {
                    break;
                }

                notes.AddRange(pageList);

                if (page >= pageList.TotalPages)
                {
                    break;
                }

                page++;
            }

            return ProcessingResult<IList<HmmNote>>.Ok(notes);
        }

        private async Task<int> GetCatalogIdAsync()
        {
            var catalogsResult = await _lookupRepo.GetEntitiesAsync<NoteCatalog>(
                c => c.Name == CheatsheetConstant.CheatsheetCatalogName);

            if (!catalogsResult.Success || catalogsResult.Value == null)
            {
                return 0;
            }

            return catalogsResult.Value.FirstOrDefault()?.Id ?? 0;
        }
    }
}
