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
    /// <summary>
    /// A styling client that talked to a server predating typed Style stored the
    /// look as an unknown, PascalCase "Style" key (and a row's emoji as "Emoji")
    /// in note content. Those legacy keys must be promoted into the typed member
    /// when the camelCase one is absent, dropped when it is present, and never
    /// written back - otherwise they live forever and shadow the real look.
    /// </summary>
    public class CheatsheetLegacyStyleKeyTests
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

        private static async Task<CheatsheetCard> ReadAsync(string cardJson)
        {
            var note = new HmmNote
            {
                Id = 42,
                Subject = CheatsheetCard.GetNoteSubject("card-1"),
                Content = "{\"note\":{\"content\":{\"Cheatsheet\":" + cardJson + "}}}",
                Author = new Author { Id = 9 }
            };
            var result = await CreateSerializer().GetEntity(note);
            Assert.True(result.Success, result.ErrorMessage);
            return result.Value;
        }

        private static JsonElement CardElementOf(string noteJson)
        {
            using var document = JsonDocument.Parse(noteJson);
            return document.RootElement
                .GetProperty("note").GetProperty("content").GetProperty("Cheatsheet").Clone();
        }

        [Fact]
        public async Task LegacyStyle_WithoutTypedStyle_IsPromoted()
        {
            var card = await ReadAsync(
                "{\"id\":\"card-1\",\"rows\":[],\"Style\":{\"theme\":\"legacy\"}}");

            Assert.True(card.Style.HasValue);
            Assert.Equal("legacy", card.Style.Value.GetProperty("theme").GetString());
            Assert.False(card.ExtraFields.ContainsKey("Style"));
        }

        [Fact]
        public async Task LegacyStyle_AlongsideTypedStyle_IsDropped()
        {
            var card = await ReadAsync(
                "{\"id\":\"card-1\",\"rows\":[],\"style\":{\"theme\":\"new\"},\"Style\":{\"theme\":\"stale\"}}");

            Assert.Equal("new", card.Style!.Value.GetProperty("theme").GetString());
            Assert.False(card.ExtraFields.ContainsKey("Style"));
        }

        [Fact]
        public async Task LegacyStyle_IsNotWrittenBack()
        {
            var serializer = CreateSerializer();
            var card = await ReadAsync(
                "{\"id\":\"card-1\",\"rows\":[],\"Style\":{\"theme\":\"legacy\"},\"future\":1}");

            var written = CardElementOf(serializer.GetNoteSerializationText(card));
            var names = written.EnumerateObject().Select(p => p.Name).ToList();

            Assert.DoesNotContain("Style", names);
            Assert.Equal("legacy", written.GetProperty("style").GetProperty("theme").GetString());
            Assert.Contains("future", names);
        }

        [Fact]
        public async Task LegacyRowEmoji_WithoutTypedEmoji_IsPromoted()
        {
            var card = await ReadAsync(
                "{\"id\":\"card-1\",\"rows\":[{\"label\":\"A\",\"Emoji\":\"x\"}]}");

            Assert.Equal("x", card.Rows[0].Emoji);
            Assert.False(card.Rows[0].ExtraFields.ContainsKey("Emoji"));
        }

        [Fact]
        public async Task LegacyRowEmoji_AlongsideTypedEmoji_IsDroppedAndNotWritten()
        {
            var serializer = CreateSerializer();
            var card = await ReadAsync(
                "{\"id\":\"card-1\",\"rows\":[{\"label\":\"A\",\"emoji\":\"new\",\"Emoji\":\"stale\"}]}");

            Assert.Equal("new", card.Rows[0].Emoji);
            Assert.False(card.Rows[0].ExtraFields.ContainsKey("Emoji"));
            var row = CardElementOf(serializer.GetNoteSerializationText(card)).GetProperty("rows")[0];
            Assert.False(row.TryGetProperty("Emoji", out _));
            Assert.Equal("new", row.GetProperty("emoji").GetString());
        }
    }
}
