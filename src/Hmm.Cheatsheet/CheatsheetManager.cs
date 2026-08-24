using System;
using Microsoft.Extensions.Logging;
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
        private readonly ICheatsheetCatalogProvider _catalogProvider;
        private readonly ILogger<CheatsheetManager> _logger;
        private readonly IAuthorProvider _authorProvider;

        public CheatsheetManager(
            INoteSerializer<CheatsheetCard> noteSerializer,
            IHmmValidator<CheatsheetCard> validator,
            IHmmNoteManager noteManager,
            ICheatsheetCatalogProvider catalogProvider,
            IAuthorProvider authorProvider,
            ILogger<CheatsheetManager> logger = null)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(noteManager);
            ArgumentNullException.ThrowIfNull(catalogProvider);
            ArgumentNullException.ThrowIfNull(authorProvider);

            _noteSerializer = noteSerializer;
            _validator = validator;
            _noteManager = noteManager;
            _catalogProvider = catalogProvider;
            _logger = logger;
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
        public async Task<ProcessingResult<CheatsheetCard>> CreateAsync(
            CheatsheetCard card,
            bool commitChanges = true)
        {
            if (card == null)
            {
                return ProcessingResult<CheatsheetCard>.Invalid("Cheatsheet card cannot be null");
            }

            var authorResult = await _authorProvider.GetAuthorAsync();
            if (!authorResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(
                    authorResult.ErrorMessage, authorResult.ErrorType);
            }

            if (string.IsNullOrWhiteSpace(card.Id))
            {
                // Guid format (8-4-4-4-12) matches the Dart uuid package's v4
                // output, so client- and server-minted ids are indistinguishable.
                card.Id = Guid.NewGuid().ToString();
            }

            card.AuthorId = authorResult.Value.Id;

            var validationResult = await _validator.ValidateEntityAsync(card);
            if (!validationResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Invalid(validationResult.GetWholeMessage());
            }

            var existingResult = await FindNoteForCardAsync(card.Id);
            if (existingResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Conflict(
                    $"Cheatsheet card '{card.Id}' already exists");
            }

            // Only "no such card" means the id is free. Any other failure means
            // the uniqueness check never ran, and creating anyway would put a
            // second note under the same subject - the exact duplicate this
            // check exists to prevent.
            if (!existingResult.IsNotFound)
            {
                return ProcessingResult<CheatsheetCard>.Fail(
                    existingResult.ErrorMessage, existingResult.ErrorType);
            }

            var noteResult = await _noteSerializer.GetNote(card);
            if (!noteResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Id = 0;
            note.Author = authorResult.Value;

            var createdResult = await _noteManager.CreateAsync(note, commitChanges);
            if (!createdResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(
                    createdResult.ErrorMessage, createdResult.ErrorType);
            }

            return await ReadBackAsync(createdResult.Value, authorResult.Value.Id);
        }

        public async Task<ProcessingResult<CheatsheetCard>> UpdateAsync(
            CheatsheetCard card,
            bool commitChanges = true)
        {
            if (card == null)
            {
                return ProcessingResult<CheatsheetCard>.Invalid("Cheatsheet card cannot be null");
            }

            if (string.IsNullOrWhiteSpace(card.Id))
            {
                return ProcessingResult<CheatsheetCard>.Invalid("Cheatsheet card id cannot be empty");
            }

            var authorResult = await _authorProvider.GetAuthorAsync();
            if (!authorResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(
                    authorResult.ErrorMessage, authorResult.ErrorType);
            }

            var existingNoteResult = await FindNoteForCardAsync(card.Id);
            if (!existingNoteResult.Success)
            {
                // Preserve the real cause. Collapsing every failure into 404 told
                // a user their card no longer existed when the truth was a
                // transient read fault, and pointed operators at the wrong thing.
                return existingNoteResult.IsNotFound
                    ? ProcessingResult<CheatsheetCard>.NotFound(
                        $"Cannot find cheatsheet card '{card.Id}'")
                    : ProcessingResult<CheatsheetCard>.Fail(
                        existingNoteResult.ErrorMessage, existingNoteResult.ErrorType);
            }

            var existingNote = existingNoteResult.Value;
            card.AuthorId = authorResult.Value.Id;
            card.NoteId = existingNote.Id;

            var validationResult = await _validator.ValidateEntityAsync(card);
            if (!validationResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Invalid(validationResult.GetWholeMessage());
            }

            var noteResult = await _noteSerializer.GetNote(card);
            if (!noteResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = authorResult.Value;

            // The serializer builds a FRESH note, so the stored row's identity
            // has to be carried across by hand. HmmNoteManager.UpdateAsync mints
            // a new Uuid whenever the incoming note has none - which would
            // silently re-identify the card on every save.
            note.Uuid = existingNote.Uuid;
            note.CreateDate = existingNote.CreateDate;
            note.NoteDate = existingNote.NoteDate;
            note.Version = existingNote.Version;
            note.Tags = existingNote.Tags;
            note.Catalog ??= existingNote.Catalog;

            var updatedResult = await _noteManager.UpdateAsync(note, commitChanges);
            if (!updatedResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(
                    updatedResult.ErrorMessage, updatedResult.ErrorType);
            }

            return await ReadBackAsync(updatedResult.Value, authorResult.Value.Id);
        }

        public async Task<ProcessingResult<Unit>> DeleteAsync(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return ProcessingResult<Unit>.Invalid("Cheatsheet card id cannot be empty");
            }

            var noteResult = await FindNoteForCardAsync(cardId);
            if (!noteResult.Success)
            {
                // Same reasoning as UpdateAsync: only a genuine miss is a 404.
                return noteResult.IsNotFound
                    ? ProcessingResult<Unit>.NotFound($"Cannot find cheatsheet card '{cardId}'")
                    : ProcessingResult<Unit>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            return await _noteManager.DeleteAsync(noteResult.Value.Id);
        }

        /// <summary>
        /// Re-reads the persisted note so callers always get exactly what was
        /// stored, and stamps the author id the persisted note may not carry.
        /// </summary>
        private async Task<ProcessingResult<CheatsheetCard>> ReadBackAsync(HmmNote note, int authorId)
        {
            var cardResult = await _noteSerializer.GetEntity(note);
            if (cardResult.Success && cardResult.Value != null)
            {
                cardResult.Value.AuthorId = authorId;
            }

            return cardResult;
        }

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
                else
                {
                    // The card vanishes from the user's wallet here, so say so
                    // with the note's identity attached. The serializer logs the
                    // parse error, but nothing recorded which note was dropped
                    // or that a list had silently shrunk.
                    _logger?.LogWarning(
                        "Dropping unreadable cheatsheet note {NoteId} ({Subject}) from list results: {Error}",
                        note.Id,
                        note.Subject,
                        cardResult.ErrorMessage);
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
                // ServerError, not NotFound: the catalog is seeded at boot, so its
                // absence is a server fault. Reporting NotFound made it
                // indistinguishable from "this card does not exist", which let
                // CreateAsync below mistake a failed uniqueness check for a
                // clear field and create a duplicate note.
                return ProcessingResult<IList<HmmNote>>.Fail(
                    $"Cannot find note catalog '{CheatsheetConstant.CheatsheetCatalogName}'",
                    ErrorCategory.ServerError);
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
            // Reuse the shared provider rather than re-querying. This lookup ran
            // uncached on every read and write, alongside the serializer's own
            // (blocking) catalog fetch - two round-trips per call for one value.
            // The provider caches on success only, so a transient failure still
            // retries rather than being remembered as "missing".
            var catalog = await _catalogProvider.GetCatalogAsync();
            return catalog?.Id ?? 0;
        }
    }
}
