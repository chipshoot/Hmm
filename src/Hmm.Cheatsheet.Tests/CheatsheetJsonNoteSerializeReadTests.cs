using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.NoteSerialize;
using Hmm.Core.Map.DomainEntity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetJsonNoteSerializeReadTests
    {
        private static CheatsheetJsonNoteSerialize CreateSerializer()
        {
            var catalogProvider = new Mock<ICheatsheetCatalogProvider>();
            catalogProvider
                .Setup(p => p.GetCatalogAsync())
                .ReturnsAsync(new NoteCatalog
                {
                    Id = 7,
                    Name = CheatsheetConstant.CheatsheetCatalogName,
                    Schema = "{}"
                });

            return new CheatsheetJsonNoteSerialize(
                catalogProvider.Object,
                NullLogger<CheatsheetCard>.Instance);
        }

        private static HmmNote NoteWith(string cardJson, string cardId = "card-1")
            => new()
            {
                Id = 42,
                Subject = CheatsheetCard.GetNoteSubject(cardId),
                Content = "{\"note\":{\"content\":{\"Cheatsheet\":" + cardJson + "}}}",
                Author = new Author { Id = 9 }
            };

        [Fact]
        public async Task GetEntity_NullNote_Fails()
        {
            var result = await CreateSerializer().GetEntity(null);

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task GetEntity_EmptyContent_Fails()
        {
            var note = new HmmNote { Id = 1, Subject = "Cheatsheet:x", Content = string.Empty };

            var result = await CreateSerializer().GetEntity(note);

            Assert.False(result.Success);
            Assert.Contains("Empty note content", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEntity_MalformedJson_FailsWithoutThrowing()
        {
            var note = new HmmNote { Id = 1, Subject = "Cheatsheet:x", Content = "{not json" };

            var result = await CreateSerializer().GetEntity(note);

            Assert.False(result.Success);
            Assert.Contains("Invalid JSON format", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEntity_MissingCheatsheetPayload_Fails()
        {
            var note = new HmmNote
            {
                Id = 1,
                Subject = "Cheatsheet:x",
                Content = "{\"note\":{\"content\":{\"GasLog\":{}}}}"
            };

            var result = await CreateSerializer().GetEntity(note);

            Assert.False(result.Success);
            Assert.Contains("Cheatsheet", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEntity_ReadsEveryKnownField()
        {
            var note = NoteWith(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"Passport\"," +
                "\"walletGroup\":\"Travel\",\"tags\":[\"trip\",\"id\"]," +
                "\"templateId\":\"blank\",\"protected\":true," +
                "\"rows\":[{\"label\":\"Number\",\"valueAction\":\"call\",\"openSource\":false," +
                "\"source\":{\"noteUuid\":\"u-1\",\"kind\":\"field\",\"locator\":\"Passport.number\"}}]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            var card = result.Value;
            Assert.Equal(42, card.NoteId);
            Assert.Equal(9, card.AuthorId);
            Assert.Equal(1, card.SchemaVersion);
            Assert.Equal("card-1", card.Id);
            Assert.Equal("Passport", card.Title);
            Assert.Equal("Travel", card.WalletGroup);
            Assert.Equal(new[] { "trip", "id" }, card.Tags);
            Assert.Equal("blank", card.TemplateId);
            Assert.True(card.Protected);

            var row = Assert.Single(card.Rows);
            Assert.Equal("Number", row.Label);
            Assert.Equal("call", row.ValueAction);
            Assert.False(row.OpenSource);
            Assert.NotNull(row.Source);
            Assert.Equal("u-1", row.Source.NoteUuid);
            Assert.Equal("field", row.Source.Kind);
            Assert.Equal("Passport.number", row.Source.Locator);
        }

        [Fact]
        public async Task GetEntity_AppliesClientDefaults_WhenFieldsAbsent()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"rows\":[{\"label\":\"L\"}]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal("Ungrouped", result.Value.WalletGroup);
            Assert.Equal("blank", result.Value.TemplateId);
            Assert.False(result.Value.Protected);
            var row = Assert.Single(result.Value.Rows);
            Assert.Equal("none", row.ValueAction);
            Assert.True(row.OpenSource);
            Assert.Null(row.Source);
        }

        [Fact]
        public async Task GetEntity_FallsBackToSubject_WhenIdMissing()
        {
            var note = NoteWith("{\"title\":\"No id\"}", "subject-card");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal("subject-card", result.Value.Id);
        }

        [Fact]
        public async Task GetEntity_KeepsUnknownCardFields()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"futureFlag\":true,\"futureBag\":{\"a\":1}}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.True(result.Value.ExtraFields.ContainsKey("futureFlag"));
            Assert.Equal(JsonValueKind.True, result.Value.ExtraFields["futureFlag"].ValueKind);
            Assert.Equal("{\"a\":1}", result.Value.ExtraFields["futureBag"].GetRawText());
        }

        [Fact]
        public async Task GetEntity_KeepsMistypedKnownCardFields()
        {
            // "title" is a number, not a string: it must not be silently dropped.
            var note = NoteWith("{\"id\":\"card-1\",\"title\":17}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(string.Empty, result.Value.Title);
            Assert.Equal("17", result.Value.ExtraFields["title"].GetRawText());
        }

        [Fact]
        public async Task GetEntity_KeepsUnknownRowAndSourceFields()
        {
            var note = NoteWith(
                "{\"id\":\"card-1\",\"rows\":[{\"label\":\"L\",\"futureRowFlag\":3," +
                "\"source\":{\"noteUuid\":\"u\",\"kind\":\"whole\",\"futureSourceFlag\":\"x\"}}]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            var row = Assert.Single(result.Value.Rows);
            Assert.Equal("3", row.ExtraFields["futureRowFlag"].GetRawText());
            Assert.Equal("\"x\"", row.Source.ExtraFields["futureSourceFlag"].GetRawText());
        }

        [Fact]
        public async Task GetEntity_KeepsNonObjectRowVerbatim()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"rows\":[\"i am not a row\",{\"label\":\"L\"}]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(2, result.Value.Rows.Count);
            Assert.True(result.Value.Rows[0].IsUnreadable);
            Assert.Equal("\"i am not a row\"", result.Value.Rows[0].RawJson.Value.GetRawText());
            Assert.False(result.Value.Rows[1].IsUnreadable);
        }

        [Fact]
        public async Task GetEntity_KeepsRowWithNonObjectSourceVerbatim()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"rows\":[{\"label\":\"L\",\"source\":\"oops\"}]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            var row = Assert.Single(result.Value.Rows);
            Assert.True(row.IsUnreadable);
            Assert.Contains("oops", row.RawJson.Value.GetRawText());
        }

        [Fact]
        public async Task GetEntity_KeepsNonArrayRowsVerbatim()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"rows\":\"nope\"}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Empty(result.Value.Rows);
            Assert.Equal("\"nope\"", result.Value.ExtraFields["rows"].GetRawText());
        }

        [Fact]
        public async Task GetEntity_KeepsTagsVerbatim_WhenNotAllStrings()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"tags\":[\"ok\",7]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Empty(result.Value.Tags);
            Assert.Equal("[\"ok\",7]", result.Value.ExtraFields["tags"].GetRawText());
        }

        [Fact]
        public async Task GetEntity_PreservedElementsSurviveDocumentDisposal()
        {
            // Regression guard: JsonElements must be cloned, or reading them
            // after the JsonDocument is disposed throws ObjectDisposedException.
            var note = NoteWith("{\"id\":\"card-1\",\"futureBag\":{\"a\":1},\"rows\":[42]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal("{\"a\":1}", result.Value.ExtraFields["futureBag"].GetRawText());
            Assert.Equal("42", result.Value.Rows.Single().RawJson.Value.GetRawText());
        }
    }
}
