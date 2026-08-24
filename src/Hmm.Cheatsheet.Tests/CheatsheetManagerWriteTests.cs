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
    public class CheatsheetManagerWriteTests
    {
        private static readonly Author TestAuthor = new() { Id = 9, AccountName = "tester" };

        private static readonly NoteCatalog TestCatalog = new()
        {
            Id = 7,
            Name = CheatsheetConstant.CheatsheetCatalogName,
            Schema = "{}"
        };

        private readonly List<HmmNote> _notes = [];
        private readonly Mock<IHmmNoteManager> _noteManager = new();
        private readonly Mock<IHmmValidator<CheatsheetCard>> _validator = new();
        private readonly CheatsheetManager _manager;

        public CheatsheetManagerWriteTests()
        {
            _noteManager
                .Setup(m => m.GetNotesAsync(
                    It.IsAny<Expression<Func<HmmNote, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync((Expression<Func<HmmNote, bool>> query, bool __, ResourceCollectionParameters parameters) =>
                {
                    // Apply the predicate. Discarding it made the author + catalog
                    // scoping on the WRITE path unobservable: deleting that filter
                    // left all 16 write tests green, so a refactor giving the write
                    // path its own unscoped query could have shipped cross-author
                    // UPDATE and DELETE without a single failure.
                    IList<HmmNote> matched = query == null
                        ? _notes
                        : _notes.Where(query.Compile()).ToList();
                    var (pageIndex, pageSize) = parameters.GetPaginationTuple();
                    var items = matched.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                    return ProcessingResult<PageList<HmmNote>>.Ok(
                        new PageList<HmmNote>(items, matched.Count, pageIndex, pageSize));
                });

            _noteManager
                .Setup(m => m.CreateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()))
                .ReturnsAsync((HmmNote note, bool _) =>
                {
                    note.Id = _notes.Count + 1;
                    note.Uuid ??= "uuid-" + note.Id;
                    note.Author = TestAuthor;
                    _notes.Add(note);
                    return ProcessingResult<HmmNote>.Ok(note);
                });

            _noteManager
                .Setup(m => m.UpdateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()))
                .ReturnsAsync((HmmNote note, bool _) =>
                {
                    var index = _notes.FindIndex(n => n.Id == note.Id);
                    if (index >= 0)
                    {
                        _notes[index] = note;
                    }

                    return ProcessingResult<HmmNote>.Ok(note);
                });

            _noteManager
                .Setup(m => m.DeleteAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    _notes.RemoveAll(n => n.Id == id);
                    return ProcessingResult<Unit>.Ok(Unit.Value);
                });

            _validator
                .Setup(v => v.ValidateEntityAsync(It.IsAny<CheatsheetCard>()))
                .ReturnsAsync((CheatsheetCard card) => ProcessingResult<CheatsheetCard>.Ok(card));

            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(
                    new PageList<NoteCatalog>(new[] { TestCatalog }, 1, 1, 10)));

            var authorProvider = new Mock<IAuthorProvider>();
            authorProvider.Setup(p => p.GetAuthorAsync()).ReturnsAsync(ProcessingResult<Author>.Ok(TestAuthor));
            authorProvider.Setup(p => p.CachedAuthor).Returns(TestAuthor);

            var catalogProvider = new Mock<ICheatsheetCatalogProvider>();
            catalogProvider.Setup(p => p.GetCatalogAsync()).ReturnsAsync(TestCatalog);

            _manager = new CheatsheetManager(
                new CheatsheetJsonNoteSerialize(catalogProvider.Object, NullLogger<CheatsheetCard>.Instance),
                _validator.Object,
                _noteManager.Object,
                lookup.Object,
                authorProvider.Object);
        }

        private static CheatsheetCard NewCard(string id = "c-1") => new()
        {
            Id = id,
            Title = "Passport",
            WalletGroup = "Travel",
            TemplateId = "blank",
            Tags = new List<string> { "trip" }
        };

        [Fact]
        public async Task CreateAsync_StoresTheCardUnderTheSubjectIdentity()
        {
            var result = await _manager.CreateAsync(NewCard());

            Assert.True(result.Success);
            Assert.Equal("c-1", result.Value.Id);
            Assert.Equal("Passport", result.Value.Title);
            Assert.Equal(9, result.Value.AuthorId);
            var note = Assert.Single(_notes);
            Assert.Equal("Cheatsheet:c-1", note.Subject);
            Assert.Equal(TestAuthor, note.Author);
            Assert.Equal(TestCatalog, note.Catalog);
        }

        [Fact]
        public async Task CreateAsync_MintsAnId_WhenTheClientOmitsOne()
        {
            var card = NewCard();
            card.Id = string.Empty;

            var result = await _manager.CreateAsync(card);

            Assert.True(result.Success);
            Assert.True(Guid.TryParse(result.Value.Id, out _));
        }

        [Fact]
        public async Task CreateAsync_NullCard_IsInvalid()
        {
            var result = await _manager.CreateAsync(null);

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task CreateAsync_ValidationFailure_IsReported()
        {
            _validator
                .Setup(v => v.ValidateEntityAsync(It.IsAny<CheatsheetCard>()))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.Invalid("Title is required"));

            var result = await _manager.CreateAsync(NewCard());

            Assert.False(result.Success);
            Assert.Contains("Title is required", result.GetWholeMessage());
            Assert.Empty(_notes);
        }

        [Fact]
        public async Task CreateAsync_DuplicateCardId_Conflicts()
        {
            await _manager.CreateAsync(NewCard());

            var result = await _manager.CreateAsync(NewCard());

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.Conflict, result.ErrorType);
            Assert.Single(_notes);
        }

        [Fact]
        public async Task UpdateAsync_RewritesTheCardContent()
        {
            await _manager.CreateAsync(NewCard());
            var updated = NewCard();
            updated.Title = "Renewed passport";
            updated.WalletGroup = "Documents";

            var result = await _manager.UpdateAsync(updated);

            Assert.True(result.Success);
            Assert.Equal("Renewed passport", result.Value.Title);
            Assert.Equal("Documents", result.Value.WalletGroup);
            Assert.Single(_notes);
        }

        [Fact]
        public async Task UpdateAsync_CarriesTheStoredNoteIdentityForward()
        {
            // HmmNoteManager.UpdateAsync mints a fresh Uuid when the incoming
            // note has none, and the serializer builds a brand new note - so
            // without an explicit carry-forward the card would lose its
            // cross-device identity on every single save.
            await _manager.CreateAsync(NewCard());
            var stored = _notes.Single();
            stored.Uuid = "stable-uuid";
            stored.CreateDate = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            stored.NoteDate = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            stored.Version = [1, 2, 3];

            var updated = NewCard();
            updated.Title = "Changed";
            await _manager.UpdateAsync(updated);

            var note = _notes.Single();
            Assert.Equal("stable-uuid", note.Uuid);
            Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), note.CreateDate);
            Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), note.NoteDate);
            Assert.Equal(new byte[] { 1, 2, 3 }, note.Version);
        }

        [Fact]
        public async Task UpdateAsync_UnknownCard_IsNotFound()
        {
            var result = await _manager.UpdateAsync(NewCard("missing"));

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task UpdateAsync_NullCard_IsInvalid()
        {
            var result = await _manager.UpdateAsync(null);

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task UpdateAsync_EmptyCardId_IsInvalid()
        {
            var card = NewCard();
            card.Id = "  ";

            var result = await _manager.UpdateAsync(card);

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task DeleteAsync_RemovesTheBackingNote()
        {
            await _manager.CreateAsync(NewCard());

            var result = await _manager.DeleteAsync("c-1");

            Assert.True(result.Success);
            Assert.Empty(_notes);
            _noteManager.Verify(m => m.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_UnknownCard_IsNotFound()
        {
            var result = await _manager.DeleteAsync("missing");

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task DeleteAsync_EmptyCardId_IsInvalid()
        {
            var result = await _manager.DeleteAsync(" ");

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task CreateThenReadBack_PreservesUnknownData()
        {
            using var document = System.Text.Json.JsonDocument.Parse("{\"nested\":[1,2]}");
            var card = NewCard();
            card.ExtraFields["future"] = document.RootElement.Clone();
            card.Rows = new List<CheatsheetRow>
            {
                new() { RawJson = System.Text.Json.JsonDocument.Parse("\"corrupt\"").RootElement.Clone() }
            };

            await _manager.CreateAsync(card);
            var result = await _manager.GetCardByIdAsync("c-1");

            Assert.True(result.Success);
            Assert.Equal("{\"nested\":[1,2]}", result.Value.ExtraFields["future"].GetRawText());
            Assert.Equal("\"corrupt\"", Assert.Single(result.Value.Rows).RawJson.Value.GetRawText());
        }

        [Fact]
        public async Task CreateAsync_PassesCommitChangesThrough()
        {
            // commitChanges: false is how a caller batches several writes into
            // one transaction. Committing regardless would break that silently,
            // so the flag must reach the note manager rather than be dropped.
            await _manager.CreateAsync(NewCard(), commitChanges: false);

            _noteManager.Verify(m => m.CreateAsync(It.IsAny<HmmNote>(), false), Times.Once);
            _noteManager.Verify(m => m.CreateAsync(It.IsAny<HmmNote>(), true), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_PassesCommitChangesThrough()
        {
            await _manager.CreateAsync(NewCard());

            var edit = NewCard();
            edit.Title = "Renewed passport";
            await _manager.UpdateAsync(edit, commitChanges: false);

            _noteManager.Verify(m => m.UpdateAsync(It.IsAny<HmmNote>(), false), Times.Once);
            _noteManager.Verify(m => m.UpdateAsync(It.IsAny<HmmNote>(), true), Times.Never);
        }


        private static readonly Author OtherAuthor = new() { Id = 42, AccountName = "someone-else" };

        private static HmmNote ForeignNote(string cardId) => new()
        {
            Id = 900,
            Uuid = "uuid-900",
            Subject = CheatsheetCard.GetNoteSubject(cardId),
            Content = "{\"note\":{\"content\":{\"Cheatsheet\":{\"schemaVersion\":1,\"id\":\""
                      + cardId + "\",\"title\":\"Theirs\",\"walletGroup\":\"Travel\",\"tags\":[],"
                      + "\"templateId\":\"blank\",\"protected\":false,\"rows\":[]}}}}",
            Author = OtherAuthor,
            Catalog = TestCatalog
        };

        /// <summary>
        /// A manager whose catalog lookup comes back empty, so every note query
        /// fails before it reaches the store. Models an infrastructure fault
        /// rather than a missing card.
        /// </summary>
        private CheatsheetManager CreateManagerWithoutCatalog()
        {
            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(
                    new PageList<NoteCatalog>(new List<NoteCatalog>(), 0, 1, 10)));

            var authorProvider = new Mock<IAuthorProvider>();
            authorProvider.Setup(p => p.GetAuthorAsync()).ReturnsAsync(ProcessingResult<Author>.Ok(TestAuthor));
            authorProvider.Setup(p => p.CachedAuthor).Returns(TestAuthor);

            var catalogProvider = new Mock<ICheatsheetCatalogProvider>();
            catalogProvider.Setup(p => p.GetCatalogAsync()).ReturnsAsync(TestCatalog);

            return new CheatsheetManager(
                new CheatsheetJsonNoteSerialize(catalogProvider.Object, NullLogger<CheatsheetCard>.Instance),
                _validator.Object,
                _noteManager.Object,
                lookup.Object,
                authorProvider.Object);
        }

        [Fact]
        public async Task UpdateAsync_OtherAuthorsCard_IsNotFound_AndLeavesItAlone()
        {
            var theirs = ForeignNote("c-1");
            _notes.Add(theirs);
            var before = theirs.Content;

            var result = await _manager.UpdateAsync(NewCard("c-1"));

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
            Assert.Equal(before, theirs.Content);
            _noteManager.Verify(m => m.UpdateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_OtherAuthorsCard_IsNotFound_AndLeavesItAlone()
        {
            _notes.Add(ForeignNote("c-1"));

            var result = await _manager.DeleteAsync("c-1");

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
            Assert.Single(_notes);
            _noteManager.Verify(m => m.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_SameCardId_DifferentAuthor_DoesNotConflict()
        {
            // Card ids are namespaced per author, so another author holding this
            // id must not block this one. Unprovable while the fake ignored the
            // author filter - it would have reported a false Conflict.
            _notes.Add(ForeignNote("c-1"));

            var result = await _manager.CreateAsync(NewCard("c-1"));

            Assert.True(result.Success);
        }

        [Fact]
        public async Task CreateAsync_WhenUniquenessCheckFails_DoesNotCreate()
        {
            // The check never ran, so the id is not known to be free. Creating
            // anyway would put a second note under the same subject.
            var result = await CreateManagerWithoutCatalog().CreateAsync(NewCard());

            Assert.False(result.Success);
            Assert.False(result.IsNotFound);
            Assert.Contains("catalog", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
            _noteManager.Verify(m => m.CreateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenLookupFails_ReportsTheRealCause_NotNotFound()
        {
            var result = await CreateManagerWithoutCatalog().UpdateAsync(NewCard());

            Assert.False(result.Success);
            Assert.False(result.IsNotFound);
            Assert.Contains("catalog", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DeleteAsync_WhenLookupFails_ReportsTheRealCause_NotNotFound()
        {
            var result = await CreateManagerWithoutCatalog().DeleteAsync("c-1");

            Assert.False(result.Success);
            Assert.False(result.IsNotFound);
            _noteManager.Verify(m => m.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

    }
}
