using System.Collections.Generic;
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
    /// The card look is a client concern the server must carry without
    /// interpreting: "style" and row "emoji" in note content map to typed
    /// members so the API can expose them as Style / Emoji, and everything
    /// inside them is passed through verbatim.
    /// </summary>
    public class CheatsheetStylePassThroughTests
    {
        private const string Eyes = "👀";

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

        private static HmmNote NoteWith(string cardJson)
            => new()
            {
                Id = 42,
                Subject = CheatsheetCard.GetNoteSubject("card-1"),
                Content = "{\"note\":{\"content\":{\"Cheatsheet\":" + cardJson + "}}}",
                Author = new Author { Id = 9 }
            };

        private static JsonElement CardElementOf(string noteJson)
        {
            using var document = JsonDocument.Parse(noteJson);
            return document.RootElement
                .GetProperty("note")
                .GetProperty("content")
                .GetProperty("Cheatsheet")
                .Clone();
        }

        private static JsonElement Raw(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static async Task<CheatsheetCard> ReadAsync(string cardJson)
        {
            var result = await CreateSerializer().GetEntity(NoteWith(cardJson));
            Assert.True(result.Success, result.ErrorMessage);
            return result.Value;
        }

        private const string StyledCard =
            "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"Vim\",\"walletGroup\":\"Reference\"," +
            "\"tags\":[],\"templateId\":\"document\",\"protected\":false," +
            "\"rows\":[{\"label\":\"Motions\",\"valueAction\":\"none\",\"openSource\":true,\"emoji\":\"" + Eyes + "\"}]," +
            "\"style\":{\"theme\":\"turquoise\",\"pattern\":\"dots\",\"sparkle\":{\"a\":[1,2]}}," +
            "\"future\":1}";

        [Fact]
        public async Task Read_Style_IsTypedNotAnExtra()
        {
            var card = await ReadAsync(StyledCard);

            Assert.True(card.Style.HasValue);
            Assert.Equal("turquoise", card.Style.Value.GetProperty("theme").GetString());
            Assert.False(card.ExtraFields.ContainsKey("style"));
            Assert.True(card.ExtraFields.ContainsKey("future"));
        }

        [Fact]
        public async Task Write_Style_IsEmittedVerbatim_IncludingUnknownKeys()
        {
            var serializer = CreateSerializer();
            var card = await ReadAsync(StyledCard);

            var style = CardElementOf(serializer.GetNoteSerializationText(card)).GetProperty("style");

            Assert.Equal("turquoise", style.GetProperty("theme").GetString());
            Assert.Equal("dots", style.GetProperty("pattern").GetString());
            Assert.Equal(2, style.GetProperty("sparkle").GetProperty("a")[1].GetInt32());
        }

        [Fact]
        public async Task Write_KeyOrder_IsRowsThenStyleThenExtras()
        {
            var serializer = CreateSerializer();
            var card = await ReadAsync(StyledCard);

            var names = CardElementOf(serializer.GetNoteSerializationText(card))
                .EnumerateObject().Select(p => p.Name).ToList();

            Assert.Equal(
                new[] { "schemaVersion", "id", "title", "walletGroup", "tags", "templateId", "protected", "rows", "style", "future" },
                names);
        }

        [Fact]
        public async Task Write_RowKeyOrder_IsEmojiAfterSourceThenExtras()
        {
            var serializer = CreateSerializer();
            var card = await ReadAsync(
                "{\"id\":\"card-1\",\"rows\":[{\"label\":\"A\",\"valueAction\":\"none\",\"openSource\":true," +
                "\"source\":{\"noteUuid\":\"n-1\",\"kind\":\"whole\"},\"emoji\":\"" + Eyes + "\",\"later\":2}]}");

            var names = CardElementOf(serializer.GetNoteSerializationText(card))
                .GetProperty("rows")[0].EnumerateObject().Select(p => p.Name).ToList();

            Assert.Equal(
                new[] { "label", "valueAction", "openSource", "source", "emoji", "later" },
                names);
        }

        [Fact]
        public void Write_AbsentStyle_IsNotWritten()
        {
            var card = new CheatsheetCard { Id = "card-1", Title = "Plain" };

            var json = CardElementOf(CreateSerializer().GetNoteSerializationText(card));

            Assert.False(json.TryGetProperty("style", out _));
        }

        [Fact]
        public async Task ExplicitNullStyle_IsKeptVerbatimAsAnExtra()
        {
            var serializer = CreateSerializer();
            var card = await ReadAsync(
                "{\"id\":\"card-1\",\"title\":\"T\",\"rows\":[],\"style\":null}");

            Assert.False(card.Style.HasValue);
            var written = CardElementOf(serializer.GetNoteSerializationText(card));
            Assert.Equal(JsonValueKind.Null, written.GetProperty("style").ValueKind);
        }

        [Fact]
        public void ATypedStyle_BeatsAPreservedNullStyle()
        {
            var card = new CheatsheetCard
            {
                Id = "card-1",
                Style = Raw("{\"theme\":\"red\"}"),
                ExtraFields = new Dictionary<string, JsonElement> { ["style"] = Raw("null") }
            };

            var json = CardElementOf(CreateSerializer().GetNoteSerializationText(card));

            Assert.Equal("red", json.GetProperty("style").GetProperty("theme").GetString());
        }

        [Fact]
        public async Task RowEmoji_RoundTrips_AndIsNotAnExtra()
        {
            var serializer = CreateSerializer();
            var card = await ReadAsync(StyledCard);

            Assert.Equal(Eyes, card.Rows[0].Emoji);
            Assert.False(card.Rows[0].ExtraFields.ContainsKey("emoji"));
            var row = CardElementOf(serializer.GetNoteSerializationText(card)).GetProperty("rows")[0];
            Assert.Equal(Eyes, row.GetProperty("emoji").GetString());
        }

        [Fact]
        public void AbsentRowEmoji_IsNotWritten()
        {
            var card = new CheatsheetCard
            {
                Id = "card-1",
                Rows = new List<CheatsheetRow> { new() { Label = "A" } }
            };

            var row = CardElementOf(CreateSerializer().GetNoteSerializationText(card)).GetProperty("rows")[0];

            Assert.False(row.TryGetProperty("emoji", out _));
        }

        [Fact]
        public async Task NonStringRowEmoji_IsPreservedAsAnExtra()
        {
            var serializer = CreateSerializer();
            var card = await ReadAsync(
                "{\"id\":\"card-1\",\"rows\":[{\"label\":\"A\",\"emoji\":5}]}");

            Assert.Null(card.Rows[0].Emoji);
            var row = CardElementOf(serializer.GetNoteSerializationText(card)).GetProperty("rows")[0];
            Assert.Equal(5, row.GetProperty("emoji").GetInt32());
        }
    }
}
