using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Cheatsheet;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.NoteSerialize;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hmm.ServiceApi.Core.Tests
{
    /// <summary>
    /// A styling client that talked to a server predating typed Style had its
    /// "Style" (and row "Emoji") stored as unknown extras. Once typed Style
    /// exists, the wire must carry exactly one "Style" - never the typed one
    /// plus a stale extra that a last-key-wins decoder would pick - and the next
    /// write must drop the legacy key from note content.
    /// </summary>
    public class CheatsheetLegacyStyleWireTests
    {
        private readonly IMapper _mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<CheatsheetMappingProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        private static JsonElement Raw(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static CheatsheetJsonNoteSerialize CreateSerializer()
        {
            var catalogProvider = new Mock<ICheatsheetCatalogProvider>();
            catalogProvider
                .Setup(p => p.GetCatalogAsync())
                .ReturnsAsync(new NoteCatalog { Id = 7, Name = "cheatsheet", Schema = "{}" });
            return new CheatsheetJsonNoteSerialize(catalogProvider.Object, NullLogger<CheatsheetCard>.Instance);
        }

        private static async Task<CheatsheetCard> ReadStoredAsync(string cardJson)
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

        private static JsonElement StoredCardOf(string noteJson)
        {
            using var document = JsonDocument.Parse(noteJson);
            return document.RootElement
                .GetProperty("note").GetProperty("content").GetProperty("Cheatsheet").Clone();
        }

        private static int CountKey(string wire, string key)
            => Regex.Matches(wire, "\"" + key + "\"\\s*:").Count;

        [Fact]
        public async Task Get_StoredLegacyStyle_IsSentOnce_AsThePromotedValue()
        {
            var card = await ReadStoredAsync(
                "{\"id\":\"card-1\",\"rows\":[],\"Style\":{\"theme\":\"legacy\"}}");

            var wire = JsonConvert.SerializeObject(_mapper.Map<CheatsheetCard, ApiCheatsheet>(card));

            Assert.Equal(1, CountKey(wire, "Style"));
            Assert.Equal("legacy", JObject.Parse(wire)["Style"]!["theme"]!.Value<string>());
        }

        [Fact]
        public void Get_TypedStyleAndLegacyExtras_EmitsOnlyTheTypedStyle()
        {
            // Guards the mapping even if a domain card arrives with the legacy
            // keys still in its extras (any reader other than the serializer).
            var card = new CheatsheetCard
            {
                Id = "card-1",
                Style = Raw("{\"theme\":\"new\"}"),
                ExtraFields = new Dictionary<string, JsonElement>
                {
                    ["Style"] = Raw("{\"theme\":\"stale\"}"),
                    ["style"] = Raw("{\"theme\":\"stale2\"}")
                }
            };

            var wire = JsonConvert.SerializeObject(_mapper.Map<CheatsheetCard, ApiCheatsheet>(card));

            Assert.Equal(1, CountKey(wire, "Style"));
            Assert.Equal(0, CountKey(wire, "style"));
            Assert.Equal("new", JObject.Parse(wire)["Style"]!["theme"]!.Value<string>());
        }

        [Fact]
        public void Get_LegacyExtraOnlyInDomainCard_IsPromotedOnTheWire()
        {
            var card = new CheatsheetCard
            {
                Id = "card-1",
                ExtraFields = new Dictionary<string, JsonElement> { ["Style"] = Raw("{\"theme\":\"legacy\"}") }
            };

            var wire = JsonConvert.SerializeObject(_mapper.Map<CheatsheetCard, ApiCheatsheet>(card));

            Assert.Equal(1, CountKey(wire, "Style"));
            Assert.Equal("legacy", JObject.Parse(wire)["Style"]!["theme"]!.Value<string>());
        }

        [Fact]
        public async Task Put_NewStyle_ReplacesIt_AndTheLegacyExtraLeavesStoredContent()
        {
            var serializer = CreateSerializer();
            var stored = await ReadStoredAsync(
                "{\"id\":\"card-1\",\"rows\":[],\"Style\":{\"theme\":\"legacy\"}}");
            var dto = JsonConvert.DeserializeObject<ApiCheatsheetForUpdate>("{\"Style\":{\"theme\":\"red\"}}")!;

            _mapper.Map(dto, stored);
            var written = StoredCardOf(serializer.GetNoteSerializationText(stored));

            Assert.False(written.TryGetProperty("Style", out _));
            Assert.Equal("red", written.GetProperty("style").GetProperty("theme").GetString());
        }

        [Fact]
        public void Put_OntoACardStillCarryingTheLegacyExtra_DropsIt()
        {
            var serializer = CreateSerializer();
            var stored = new CheatsheetCard
            {
                Id = "card-1",
                ExtraFields = new Dictionary<string, JsonElement> { ["Style"] = Raw("{\"theme\":\"legacy\"}") }
            };
            var dto = JsonConvert.DeserializeObject<ApiCheatsheetForUpdate>("{\"Style\":{\"theme\":\"red\"}}")!;

            _mapper.Map(dto, stored);
            var written = StoredCardOf(serializer.GetNoteSerializationText(stored));

            Assert.False(stored.ExtraFields.ContainsKey("Style"));
            Assert.False(written.TryGetProperty("Style", out _));
            Assert.Equal("red", written.GetProperty("style").GetProperty("theme").GetString());
        }

        [Fact]
        public void Put_WithoutStyle_PromotesTheLegacyExtraInsteadOfKeepingIt()
        {
            var stored = new CheatsheetCard
            {
                Id = "card-1",
                ExtraFields = new Dictionary<string, JsonElement> { ["Style"] = Raw("{\"theme\":\"legacy\"}") }
            };
            var dto = JsonConvert.DeserializeObject<ApiCheatsheetForUpdate>("{\"Title\":\"T\"}")!;

            _mapper.Map(dto, stored);

            Assert.False(stored.ExtraFields.ContainsKey("Style"));
            Assert.Equal("legacy", stored.Style!.Value.GetProperty("theme").GetString());
        }

        [Fact]
        public async Task Get_StoredLegacyRowEmoji_IsSentOnce_AsThePromotedValue()
        {
            var card = await ReadStoredAsync(
                "{\"id\":\"card-1\",\"rows\":[{\"label\":\"A\",\"emoji\":\"new\",\"Emoji\":\"stale\"}," +
                "{\"label\":\"B\",\"Emoji\":\"legacy\"}]}");

            var wire = JsonConvert.SerializeObject(_mapper.Map<CheatsheetCard, ApiCheatsheet>(card));
            var rows = JObject.Parse(wire)["Rows"]!;

            Assert.Equal(2, CountKey(wire, "Emoji"));
            Assert.Equal("new", rows[0]!["Emoji"]!.Value<string>());
            Assert.Equal("legacy", rows[1]!["Emoji"]!.Value<string>());
        }

        [Fact]
        public void Get_TypedRowEmojiAndLegacyExtra_EmitsOnlyTheTypedEmoji()
        {
            var card = new CheatsheetCard
            {
                Id = "card-1",
                Rows = new List<CheatsheetRow>
                {
                    new()
                    {
                        Label = "A",
                        Emoji = "new",
                        ExtraFields = new Dictionary<string, JsonElement>
                        {
                            ["Emoji"] = Raw("\"stale\""),
                            ["emoji"] = Raw("\"stale2\"")
                        }
                    }
                }
            };

            var wire = JsonConvert.SerializeObject(_mapper.Map<CheatsheetCard, ApiCheatsheet>(card));

            Assert.Equal(1, CountKey(wire, "Emoji"));
            Assert.Equal(0, CountKey(wire, "emoji"));
            Assert.Equal("new", JObject.Parse(wire)["Rows"]![0]!["Emoji"]!.Value<string>());
        }

        [Fact]
        public async Task Put_RowEmoji_LegacyExtraLeavesStoredContent()
        {
            var serializer = CreateSerializer();
            var stored = await ReadStoredAsync(
                "{\"id\":\"card-1\",\"rows\":[{\"label\":\"A\",\"Emoji\":\"legacy\"}]}");
            var dto = JsonConvert.DeserializeObject<ApiCheatsheetForUpdate>(
                "{\"Rows\":[{\"Label\":\"A\",\"Emoji\":\"fresh\"}]}")!;

            _mapper.Map(dto, stored);
            var row = StoredCardOf(serializer.GetNoteSerializationText(stored)).GetProperty("rows")[0];

            Assert.False(row.TryGetProperty("Emoji", out _));
            Assert.Equal("fresh", row.GetProperty("emoji").GetString());
        }

        [Fact]
        public async Task NonObjectStyle_RoundTripsThroughGetAndPut()
        {
            var serializer = CreateSerializer();
            var stored = await ReadStoredAsync("{\"id\":\"card-1\",\"rows\":[],\"style\":\"retro\"}");

            var wire = JsonConvert.SerializeObject(_mapper.Map<CheatsheetCard, ApiCheatsheet>(stored));
            Assert.Equal(JTokenType.String, JObject.Parse(wire)["Style"]!.Type);
            Assert.Equal("retro", JObject.Parse(wire)["Style"]!.Value<string>());

            // The client echoes what it got back.
            var dto = JsonConvert.DeserializeObject<ApiCheatsheetForUpdate>(wire)!;
            _mapper.Map(dto, stored);
            var written = StoredCardOf(serializer.GetNoteSerializationText(stored));

            Assert.Equal(JsonValueKind.String, written.GetProperty("style").ValueKind);
            Assert.Equal("retro", written.GetProperty("style").GetString());
        }

        [Fact]
        public void DateLikeStringsInsideStyle_AreNotReformatted()
        {
            const string at = "2026-10-11T08:30:00.000+02:00";
            var styleJson = "{\"at\":\"" + at + "\",\"day\":\"2026-10-11\"}";

            var token = CheatsheetJsonInterop.ToJToken(Raw(styleJson));

            Assert.Equal(JTokenType.String, token["at"]!.Type);
            Assert.Equal(at, token["at"]!.Value<string>());
            Assert.Equal(styleJson, CheatsheetJsonInterop.ToJsonElement(token).GetRawText());

            var wire = JsonConvert.SerializeObject(_mapper.Map<CheatsheetCard, ApiCheatsheet>(
                new CheatsheetCard { Id = "card-1", Style = Raw(styleJson) }));
            Assert.Contains("\"at\":\"" + at + "\"", wire);
        }
    }
}
