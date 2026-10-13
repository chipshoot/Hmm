using System.Collections.Generic;
using System.Text.Json;
using AutoMapper;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hmm.ServiceApi.Core.Tests
{
    /// <summary>
    /// Style and Emoji cross the API untouched in every direction. The PUT
    /// rule is the subtle one: an older client that has never heard of Style
    /// must not wipe it, while an explicit null is a deliberate reset.
    /// </summary>
    public class CheatsheetStyleMappingTests
    {
        private const string Eyes = "👀";

        private readonly IMapper _mapper = new MapperConfiguration(
                cfg => cfg.AddProfile<CheatsheetMappingProfile>(),
                NullLoggerFactory.Instance)
            .CreateMapper();

        private static JsonElement Raw(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static CheatsheetCard StyledCard() => new()
        {
            NoteId = 42,
            AuthorId = 9,
            Id = "c-1",
            Title = "Vim",
            TemplateId = "document",
            Style = Raw("{\"theme\":\"turquoise\",\"sparkle\":{\"a\":1}}"),
            Rows = new List<CheatsheetRow> { new() { Label = "Motions", Emoji = Eyes } }
        };

        [Fact]
        public void Configuration_IsStillValid()
        {
            new MapperConfiguration(
                    cfg => cfg.AddProfile<CheatsheetMappingProfile>(),
                    NullLoggerFactory.Instance)
                .AssertConfigurationIsValid();
        }

        [Fact]
        public void Card_StyleAndEmoji_ReachTheWireVerbatim()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(StyledCard());

            var wire = JObject.Parse(JsonConvert.SerializeObject(api));

            Assert.Equal("turquoise", wire["Style"]!["theme"]!.Value<string>());
            Assert.Equal(1, wire["Style"]!["sparkle"]!["a"]!.Value<int>());
            Assert.Equal(Eyes, wire["Rows"]![0]!["Emoji"]!.Value<string>());
        }

        [Fact]
        public void UnstyledCard_OmitsStyleAndEmojiOnTheWire()
        {
            var card = new CheatsheetCard
            {
                Id = "c-2",
                Rows = new List<CheatsheetRow> { new() { Label = "A" } }
            };

            var wire = JObject.Parse(JsonConvert.SerializeObject(
                _mapper.Map<CheatsheetCard, ApiCheatsheet>(card)));

            Assert.Null(wire["Style"]);
            Assert.Null(wire["Rows"]![0]!["Emoji"]);
        }

        [Fact]
        public void ForCreate_FromTheWire_CarriesStyleAndEmoji()
        {
            const string body =
                "{\"Id\":\"c-3\",\"Title\":\"T\",\"Rows\":[{\"Label\":\"A\",\"Emoji\":\"\\uD83D\\uDC40\"}]," +
                "\"Style\":{\"pattern\":\"dots\",\"future\":true}}";
            var dto = JsonConvert.DeserializeObject<ApiCheatsheetForCreate>(body)!;

            var card = _mapper.Map<ApiCheatsheetForCreate, CheatsheetCard>(dto);

            Assert.Equal("dots", card.Style!.Value.GetProperty("pattern").GetString());
            Assert.True(card.Style.Value.GetProperty("future").GetBoolean());
            Assert.Equal(Eyes, card.Rows[0].Emoji);
            Assert.False(card.ExtraFields.ContainsKey("Style"));
        }

        [Fact]
        public void ForUpdate_WithoutStyleKey_KeepsTheStoredStyle()
        {
            var stored = StyledCard();
            var dto = JsonConvert.DeserializeObject<ApiCheatsheetForUpdate>("{\"Title\":\"Renamed\"}")!;

            Assert.False(dto.StyleSpecified);
            _mapper.Map(dto, stored);

            Assert.Equal("Renamed", stored.Title);
            Assert.Equal("turquoise", stored.Style!.Value.GetProperty("theme").GetString());
        }

        [Fact]
        public void ForUpdate_WithExplicitNullStyle_ClearsIt()
        {
            var stored = StyledCard();
            var dto = JsonConvert.DeserializeObject<ApiCheatsheetForUpdate>("{\"Title\":\"T\",\"Style\":null}")!;

            Assert.True(dto.StyleSpecified);
            _mapper.Map(dto, stored);

            Assert.False(stored.Style.HasValue);
        }

        [Fact]
        public void ForUpdate_WithAStyle_ReplacesIt()
        {
            var stored = StyledCard();
            var dto = JsonConvert.DeserializeObject<ApiCheatsheetForUpdate>("{\"Style\":{\"theme\":\"red\"}}")!;

            _mapper.Map(dto, stored);

            Assert.Equal("red", stored.Style!.Value.GetProperty("theme").GetString());
            Assert.False(stored.Style.Value.TryGetProperty("sparkle", out _));
        }

        [Fact]
        public void RowConverter_ReadsAndWritesEmoji()
        {
            var row = JsonConvert.DeserializeObject<ApiCheatsheetRow>("{\"Label\":\"A\",\"Emoji\":\"x\"}")!;

            Assert.Equal("x", row.Emoji);
            Assert.False(row.ExtraFields.ContainsKey("Emoji"));
            Assert.Equal("x", JObject.Parse(JsonConvert.SerializeObject(row))["Emoji"]!.Value<string>());
            Assert.Null(JObject.Parse(JsonConvert.SerializeObject(new ApiCheatsheetRow { Label = "B" }))["Emoji"]);
        }

        [Fact]
        public void RowConverter_KeepsANonStringEmojiAsAnExtra()
        {
            var row = JsonConvert.DeserializeObject<ApiCheatsheetRow>("{\"Label\":\"A\",\"Emoji\":5}")!;

            Assert.Null(row.Emoji);
            Assert.Equal(5, row.ExtraFields["Emoji"].Value<int>());
            Assert.Equal(5, JObject.Parse(JsonConvert.SerializeObject(row))["Emoji"]!.Value<int>());
        }
    }
}
