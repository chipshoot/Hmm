using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.NoteSerialize;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetManagerReadTests
    {
        private static readonly Author TestAuthor = new() { Id = 9, AccountName = "tester" };

        private static readonly NoteCatalog TestCatalog = new()
        {
            Id = 7,
            Name = CheatsheetConstant.CheatsheetCatalogName,
            Schema = "{}"
        };

        private static string ContentFor(
            string cardId,
            string title,
            string walletGroup,
            params string[] tags)
        {
            var tagJson = string.Join(",", tags.Select(t => "\"" + t + "\""));
            return "{\"note\":{\"content\":{\"Cheatsheet\":{" +
                   "\"schemaVersion\":1,\"id\":\"" + cardId + "\",\"title\":\"" + title + "\"," +
                   "\"walletGroup\":\"" + walletGroup + "\",\"tags\":[" + tagJson + "]," +
                   "\"templateId\":\"blank\",\"protected\":false,\"rows\":[]}}}}";
        }

        private static HmmNote NoteFor(int id, string cardId, string title, string walletGroup, params string[] tags)
            => new()
            {
                Id = id,
                Uuid = "uuid-" + id,
                Subject = CheatsheetCard.GetNoteSubject(cardId),
                Content = ContentFor(cardId, title, walletGroup, tags),
                Author = TestAuthor,
                Catalog = TestCatalog
            };

        private static CheatsheetJsonNoteSerialize CreateSerializer()
        {
            var catalogProvider = new Mock<ICheatsheetCatalogProvider>();
            catalogProvider.Setup(p => p.GetCatalogAsync()).ReturnsAsync(TestCatalog);
            return new CheatsheetJsonNoteSerialize(catalogProvider.Object, NullLogger<CheatsheetCard>.Instance);
        }

        private static Mock<IEntityLookup> CreateLookup()
        {
            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(
                    new PageList<NoteCatalog>(new[] { TestCatalog }, 1, 1, 10)));
            lookup
                .Setup(l => l.GetEntityAsync<Author>(It.IsAny<int>()))
                .ReturnsAsync(ProcessingResult<Author>.Ok(TestAuthor));
            return lookup;
        }

        private static Mock<IAuthorProvider> CreateAuthorProvider()
        {
            var provider = new Mock<IAuthorProvider>();
            provider.Setup(p => p.GetAuthorAsync()).ReturnsAsync(ProcessingResult<Author>.Ok(TestAuthor));
            provider.Setup(p => p.CachedAuthor).Returns(TestAuthor);
            return provider;
        }

        /// <summary>
        /// Serves the given notes through the paging loop the manager drives,
        /// honouring PageNumber / PageSize so pagination bugs surface.
        /// </summary>
        private static Mock<IHmmNoteManager> CreateNoteManager(IList<HmmNote> notes)
        {
            var noteManager = new Mock<IHmmNoteManager>();
            noteManager
                .Setup(m => m.GetNotesAsync(
                    It.IsAny<Expression<Func<HmmNote, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync((Expression<Func<HmmNote, bool>> query, bool __, ResourceCollectionParameters parameters) =>
                {
                    // Apply the predicate the manager supplied. Discarding it made
                    // the author + catalog scoping unobservable: dropping that
                    // filter entirely left every test green.
                    IList<HmmNote> matched = query == null
                        ? notes
                        : notes.Where(query.Compile()).ToList();
                    var (pageIndex, pageSize) = parameters.GetPaginationTuple();
                    var items = matched.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                    return ProcessingResult<PageList<HmmNote>>.Ok(
                        new PageList<HmmNote>(items, matched.Count, pageIndex, pageSize));
                });
            return noteManager;
        }

        private static CheatsheetManager CreateManager(IList<HmmNote> notes)
            => new(
                CreateSerializer(),
                Mock.Of<IHmmValidator<CheatsheetCard>>(),
                CreateNoteManager(notes).Object,
                CreateLookup().Object,
                CreateAuthorProvider().Object);

        private static IList<HmmNote> SampleNotes() =>
        [
            NoteFor(1, "c-1", "Passport", "Travel", "trip", "id"),
            NoteFor(2, "c-2", "Alarm code", "Home", "security"),
            NoteFor(3, "c-3", "Bike lock", "Home", "trip")
        ];

        [Fact]
        public async Task GetCardsAsync_ReturnsEveryCard()
        {
            var result = await CreateManager(SampleNotes()).GetCardsAsync();

            Assert.True(result.Success);
            Assert.Equal(3, result.Value.TotalCount);
            Assert.Equal(3, result.Value.Count);
        }

        [Fact]
        public async Task GetCardsAsync_OrdersByTitleThenId()
        {
            var result = await CreateManager(SampleNotes()).GetCardsAsync();

            Assert.True(result.Success);
            Assert.Equal(new[] { "Alarm code", "Bike lock", "Passport" }, result.Value.Select(c => c.Title));
        }

        [Fact]
        public async Task GetCardsAsync_FiltersByWalletGroup_CaseInsensitively()
        {
            var result = await CreateManager(SampleNotes()).GetCardsAsync(walletGroup: "home");

            Assert.True(result.Success);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.All(result.Value, c => Assert.Equal("Home", c.WalletGroup));
        }

        [Fact]
        public async Task GetCardsAsync_FiltersByTag_CaseInsensitively()
        {
            var result = await CreateManager(SampleNotes()).GetCardsAsync(tag: "TRIP");

            Assert.True(result.Success);
            Assert.Equal(new[] { "Bike lock", "Passport" }, result.Value.Select(c => c.Title));
        }

        [Fact]
        public async Task GetCardsAsync_CombinesBothFilters()
        {
            var result = await CreateManager(SampleNotes()).GetCardsAsync(walletGroup: "Home", tag: "trip");

            Assert.True(result.Success);
            Assert.Equal("Bike lock", Assert.Single(result.Value).Title);
        }

        [Fact]
        public async Task GetCardsAsync_PagesTheFilteredSet()
        {
            var parameters = new ResourceCollectionParameters { PageNumber = 2, PageSize = 2 };

            var result = await CreateManager(SampleNotes()).GetCardsAsync(resourceCollectionParameters: parameters);

            Assert.True(result.Success);
            Assert.Equal(3, result.Value.TotalCount);
            Assert.Equal(2, result.Value.CurrentPage);
            Assert.Equal("Passport", Assert.Single(result.Value).Title);
        }

        [Fact]
        public async Task GetCardsAsync_ReadsEveryNotePage()
        {
            // 250 notes with a 100-note page size means the manager must loop
            // three times; a single-page read would silently hide cards.
            var notes = Enumerable.Range(1, 250)
                .Select(i => NoteFor(i, "c-" + i, "Card " + i.ToString("D3"), "Home"))
                .ToList();

            var result = await CreateManager(notes).GetCardsAsync(
                resourceCollectionParameters: new ResourceCollectionParameters { PageNumber = 1, PageSize = 100 });

            Assert.True(result.Success);
            Assert.Equal(250, result.Value.TotalCount);
        }

        [Fact]
        public async Task GetCardsAsync_SkipsUndeserializableNotes_WithoutFailing()
        {
            var notes = SampleNotes();
            notes.Add(new HmmNote
            {
                Id = 99,
                Subject = "Cheatsheet:broken",
                Content = "{not json",
                Author = TestAuthor,
                Catalog = TestCatalog
            });

            var result = await CreateManager(notes).GetCardsAsync();

            Assert.True(result.Success);
            Assert.Equal(3, result.Value.TotalCount);
        }

        [Fact]
        public async Task GetCardsAsync_Fails_WhenCatalogMissing()
        {
            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(
                    new PageList<NoteCatalog>(Array.Empty<NoteCatalog>(), 0, 1, 10)));

            var manager = new CheatsheetManager(
                CreateSerializer(),
                Mock.Of<IHmmValidator<CheatsheetCard>>(),
                CreateNoteManager(SampleNotes()).Object,
                lookup.Object,
                CreateAuthorProvider().Object);

            var result = await manager.GetCardsAsync();

            Assert.False(result.Success);
            Assert.Contains(CheatsheetConstant.CheatsheetCatalogName, result.ErrorMessage);
        }

        [Fact]
        public async Task GetCardByIdAsync_ReturnsTheCard()
        {
            var result = await CreateManager(SampleNotes()).GetCardByIdAsync("c-2");

            Assert.True(result.Success);
            Assert.Equal("Alarm code", result.Value.Title);
            Assert.Equal(2, result.Value.NoteId);
        }

        [Fact]
        public async Task GetCardByIdAsync_MatchesOnSubject_NotOnDecodedContent()
        {
            // The content is unreadable, so the card can only be found by
            // subject - which is exactly what keeps it deletable and fixable.
            var notes = SampleNotes();
            notes.Add(new HmmNote
            {
                Id = 99,
                Subject = CheatsheetCard.GetNoteSubject("c-broken"),
                Content = "{not json",
                Author = TestAuthor,
                Catalog = TestCatalog
            });

            var result = await CreateManager(notes).GetCardByIdAsync("c-broken");

            // The note was found; only deserialization failed.
            Assert.False(result.Success);
            Assert.Contains("Invalid JSON format", result.ErrorMessage);
        }

        [Fact]
        public async Task GetCardByIdAsync_NotFound_WhenNoSubjectMatches()
        {
            var result = await CreateManager(SampleNotes()).GetCardByIdAsync("nope");

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task GetCardByIdAsync_EmptyId_IsInvalid()
        {
            var result = await CreateManager(SampleNotes()).GetCardByIdAsync("  ");

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task GetCardsAsync_ExcludesOtherAuthorsAndOtherCatalogs()
        {
            var mine = NoteFor(1, "card-mine", "Mine", "Home");

            var theirs = NoteFor(2, "card-theirs", "Theirs", "Home");
            theirs.Author = new Author { Id = 42, AccountName = "someone-else" };

            var elsewhere = NoteFor(3, "card-elsewhere", "Elsewhere", "Home");
            elsewhere.Catalog = new NoteCatalog
            {
                Id = 99,
                Name = "Hmm.AutomobileMan.Automobile",
                Schema = "{}"
            };

            var result = await CreateManager(new[] { mine, theirs, elsewhere }).GetCardsAsync();

            Assert.True(result.Success);
            Assert.Equal(1, result.Value.TotalCount);
            Assert.Equal("card-mine", result.Value.Single().Id);
        }

    }
}
