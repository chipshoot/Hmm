using System.Collections.Generic;
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
    public class CheatsheetJsonNoteSerializeWriteTests
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

        private static JsonElement CardElementOf(string json)
        {
            using var document = JsonDocument.Parse(json);
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

        private static CheatsheetCard SampleCard() => new()
        {
            NoteId = 42,
            Id = "card-1",
            Title = "Passport",
            WalletGroup = "Travel",
            TemplateId = "blank",
            Protected = true,
            Tags = new List<string> { "trip" },
            Rows = new List<CheatsheetRow>
            {
                new()
                {
                    Label = "Number",
                    ValueAction = "call",
                    OpenSource = false,
                    Source = new CheatsheetSource
                    {
                        NoteUuid = "u-1",
                        Kind = "field",
                        Locator = "Passport.number"
                    }
                }
            }
        };

        [Fact]
        public void GetNoteSerializationText_NullEntity_ReturnsEmptyString()
        {
            Assert.Empty(CreateSerializer().GetNoteSerializationText(null));
        }

        [Fact]
        public void GetNoteSerializationText_UsesTheClientEnvelopeAndCamelCaseKeys()
        {
            var json = CreateSerializer().GetNoteSerializationText(SampleCard());

            var card = CardElementOf(json);
            Assert.Equal(1, card.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("card-1", card.GetProperty("id").GetString());
            Assert.Equal("Passport", card.GetProperty("title").GetString());
            Assert.Equal("Travel", card.GetProperty("walletGroup").GetString());
            Assert.Equal("blank", card.GetProperty("templateId").GetString());
            Assert.True(card.GetProperty("protected").GetBoolean());
            Assert.Equal(JsonValueKind.Array, card.GetProperty("tags").ValueKind);
            Assert.Equal(JsonValueKind.Array, card.GetProperty("rows").ValueKind);
        }

        [Fact]
        public void GetNoteSerializationText_WritesRowAndSourceKeys()
        {
            var json = CreateSerializer().GetNoteSerializationText(SampleCard());

            var row = CardElementOf(json).GetProperty("rows")[0];
            Assert.Equal("Number", row.GetProperty("label").GetString());
            Assert.Equal("call", row.GetProperty("valueAction").GetString());
            Assert.False(row.GetProperty("openSource").GetBoolean());

            var source = row.GetProperty("source");
            Assert.Equal("u-1", source.GetProperty("noteUuid").GetString());
            Assert.Equal("field", source.GetProperty("kind").GetString());
            Assert.Equal("Passport.number", source.GetProperty("locator").GetString());
        }

        [Fact]
        public void GetNoteSerializationText_OmitsLocator_WhenNull()
        {
            var card = SampleCard();
            card.Rows[0].Source.Locator = null;

            var json = CreateSerializer().GetNoteSerializationText(card);

            var source = CardElementOf(json).GetProperty("rows")[0].GetProperty("source");
            Assert.False(source.TryGetProperty("locator", out _));
        }

        [Fact]
        public void GetNoteSerializationText_OmitsSource_WhenRowIsUnbound()
        {
            var card = SampleCard();
            card.Rows[0].Source = null;

            var json = CreateSerializer().GetNoteSerializationText(card);

            var row = CardElementOf(json).GetProperty("rows")[0];
            Assert.False(row.TryGetProperty("source", out _));
        }

        [Fact]
        public void GetNoteSerializationText_EmitsUnreadableRowVerbatimInPlace()
        {
            var card = SampleCard();
            card.Rows.Insert(0, new CheatsheetRow { RawJson = Raw("\"i am not a row\"") });

            var json = CreateSerializer().GetNoteSerializationText(card);

            var rows = CardElementOf(json).GetProperty("rows");
            Assert.Equal(2, rows.GetArrayLength());
            Assert.Equal("i am not a row", rows[0].GetString());
            Assert.Equal("Number", rows[1].GetProperty("label").GetString());
        }

        [Fact]
        public void GetNoteSerializationText_EmitsExtrasAtEveryLevel()
        {
            var card = SampleCard();
            card.ExtraFields["futureFlag"] = Raw("true");
            card.Rows[0].ExtraFields["futureRowFlag"] = Raw("3");
            card.Rows[0].Source.ExtraFields["futureSourceFlag"] = Raw("\"x\"");

            var json = CreateSerializer().GetNoteSerializationText(card);

            var cardJson = CardElementOf(json);
            Assert.True(cardJson.GetProperty("futureFlag").GetBoolean());
            var row = cardJson.GetProperty("rows")[0];
            Assert.Equal(3, row.GetProperty("futureRowFlag").GetInt32());
            Assert.Equal("x", row.GetProperty("source").GetProperty("futureSourceFlag").GetString());
        }

        [Fact]
        public void GetNoteSerializationText_ExtrasWinOverFabricatedDefaults()
        {
            // A mistyped "title" landed in ExtraFields on read; the fabricated
            // empty-string default must not overwrite the original value.
            var card = SampleCard();
            card.Title = string.Empty;
            card.ExtraFields["title"] = Raw("17");

            var json = CreateSerializer().GetNoteSerializationText(card);

            Assert.Equal(17, CardElementOf(json).GetProperty("title").GetInt32());
        }

        [Fact]
        public async Task GetNote_BuildsNoteWithSubjectContentAndCatalog()
        {
            var result = await CreateSerializer().GetNote(SampleCard());

            Assert.True(result.Success);
            Assert.Equal(42, result.Value.Id);
            Assert.Equal("Cheatsheet:card-1", result.Value.Subject);
            Assert.Contains("\"walletGroup\":\"Travel\"", result.Value.Content);
            Assert.NotNull(result.Value.Catalog);
            Assert.Equal(CheatsheetConstant.CheatsheetCatalogName, result.Value.Catalog.Name);
        }

        [Fact]
        public async Task GetNote_NullEntity_Fails()
        {
            var result = await CreateSerializer().GetNote(null);

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task GetNote_EmptyCardId_Fails()
        {
            var card = SampleCard();
            card.Id = string.Empty;

            var result = await CreateSerializer().GetNote(card);

            Assert.False(result.Success);
            Assert.Contains("id is required", result.ErrorMessage);
        }
    }
}
