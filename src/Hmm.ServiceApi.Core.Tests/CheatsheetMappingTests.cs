using AutoMapper;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Hmm.ServiceApi.Core.Tests
{
    public class CheatsheetMappingTests
    {
        private readonly IMapper _mapper;

        public CheatsheetMappingTests()
        {
            var config = new MapperConfiguration(
                cfg => cfg.AddProfile<CheatsheetMappingProfile>(),
                NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
        }

        private static System.Text.Json.JsonElement Raw(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static CheatsheetCard SampleCard()
        {
            var card = new CheatsheetCard
            {
                NoteId = 42,
                AuthorId = 9,
                Id = "c-1",
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

            card.ExtraFields["future"] = Raw("{\"a\":1}");
            card.Rows[0].ExtraFields["icon"] = Raw("\"star\"");
            card.Rows[0].Source.ExtraFields["revision"] = Raw("12");
            card.Rows.Add(new CheatsheetRow { RawJson = Raw("\"corrupt\"") });
            return card;
        }

        [Fact]
        public void Configuration_IsValid()
        {
            var config = new MapperConfiguration(
                cfg => cfg.AddProfile<CheatsheetMappingProfile>(),
                NullLoggerFactory.Instance);

            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void Card_MapsToApiCheatsheet()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(SampleCard());

            Assert.Equal("c-1", api.Id);
            Assert.Equal(1, api.SchemaVersion);
            Assert.Equal("Passport", api.Title);
            Assert.Equal("Travel", api.WalletGroup);
            Assert.Equal("blank", api.TemplateId);
            Assert.True(api.Protected);
            Assert.Equal(new[] { "trip" }, api.Tags);
            Assert.Equal(2, api.Rows.Count);
            Assert.Equal("Number", api.Rows[0].Label);
            Assert.Equal("u-1", api.Rows[0].Source!.NoteUuid);
        }

        [Fact]
        public void Card_CarriesExtrasIntoTheDto()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(SampleCard());

            Assert.Equal("{\"a\":1}", api.ExtraFields["future"].ToString(Formatting.None));
            Assert.Equal("\"star\"", api.Rows[0].ExtraFields["icon"].ToString(Formatting.None));
            Assert.Equal("12", api.Rows[0].Source!.ExtraFields["revision"].ToString(Formatting.None));
            Assert.Equal("\"corrupt\"", api.Rows[1].Raw!.ToString(Formatting.None));
        }

        [Fact]
        public void ApiCheatsheet_MapsBackToCardLosslessly()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(SampleCard());

            var card = _mapper.Map<ApiCheatsheet, CheatsheetCard>(api);

            Assert.Equal("c-1", card.Id);
            Assert.Equal("{\"a\":1}", card.ExtraFields["future"].GetRawText());
            Assert.Equal("\"star\"", card.Rows[0].ExtraFields["icon"].GetRawText());
            Assert.Equal("12", card.Rows[0].Source!.ExtraFields["revision"].GetRawText());
            Assert.True(card.Rows[1].IsUnreadable);
            Assert.Equal("\"corrupt\"", card.Rows[1].RawJson!.Value.GetRawText());
        }

        [Fact]
        public void ForCreate_MapsToCard()
        {
            var forCreate = new ApiCheatsheetForCreate
            {
                Id = "c-9",
                Title = "Alarm",
                WalletGroup = "Home",
                TemplateId = "blank",
                Tags = new List<string> { "security" }
            };
            forCreate.ExtraFields["future"] = JToken.Parse("7");

            var card = _mapper.Map<ApiCheatsheetForCreate, CheatsheetCard>(forCreate);

            Assert.Equal("c-9", card.Id);
            Assert.Equal("Alarm", card.Title);
            Assert.Equal("Home", card.WalletGroup);
            Assert.Equal("7", card.ExtraFields["future"].GetRawText());
        }

        [Fact]
        public void ForUpdate_LeavesIdentityFieldsAlone()
        {
            var existing = SampleCard();
            var forUpdate = new ApiCheatsheetForUpdate
            {
                Title = "Renewed",
                WalletGroup = "Documents",
                TemplateId = "blank",
                Tags = new List<string>()
            };

            _mapper.Map(forUpdate, existing);

            Assert.Equal("Renewed", existing.Title);
            Assert.Equal("Documents", existing.WalletGroup);
            Assert.Equal("c-1", existing.Id);
            Assert.Equal(42, existing.NoteId);
            Assert.Equal(9, existing.AuthorId);
        }

        [Fact]
        public void ApiWireFormat_InlinesExtrasAndEmitsRawRowsVerbatim()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(SampleCard());

            var json = JsonConvert.SerializeObject(api);
            var parsed = JObject.Parse(json);

            Assert.Equal("c-1", parsed.Value<string>("Id"));
            // Card extras are inlined, not nested under an "ExtraFields" object.
            Assert.Null(parsed["ExtraFields"]);
            Assert.Equal("{\"a\":1}", parsed["future"]!.ToString(Formatting.None));

            var rows = (JArray)parsed["Rows"]!;
            Assert.Equal("star", rows[0]["icon"]!.Value<string>());
            Assert.Equal(12, rows[0]["Source"]!["revision"]!.Value<int>());
            // The unmodellable row is the raw token itself, not an object wrapper.
            Assert.Equal(JTokenType.String, rows[1].Type);
            Assert.Equal("corrupt", rows[1].Value<string>());
        }

        [Fact]
        public void ApiWireFormat_RoundTripsThroughNewtonsoft()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(SampleCard());
            var json = JsonConvert.SerializeObject(api);

            var back = JsonConvert.DeserializeObject<ApiCheatsheet>(json)!;

            Assert.Equal("c-1", back.Id);
            Assert.Equal("{\"a\":1}", back.ExtraFields["future"].ToString(Formatting.None));
            Assert.Equal(2, back.Rows.Count);
            Assert.Equal("star", back.Rows[0].ExtraFields["icon"].Value<string>());
            Assert.Equal(12, back.Rows[0].Source!.ExtraFields["revision"].Value<int>());
            Assert.Equal("corrupt", back.Rows[1].Raw!.Value<string>());
        }
        [Fact]
        public void ForUpdate_WithNoExtras_DoesNotWipeStoredExtras()
        {
            using var doc = System.Text.Json.JsonDocument.Parse("{\"future\":42}");
            var stored = new CheatsheetCard
            {
                Id = "c-1",
                Title = "Passport",
                WalletGroup = "Travel",
                TemplateId = "blank",
                ExtraFields = new Dictionary<string, JsonElement>
                {
                    ["future"] = doc.RootElement.GetProperty("future").Clone()
                }
            };

            // A client that does not model card-level extras sends none.
            var request = new ApiCheatsheetForUpdate { Title = "Renewed" };

            _mapper.Map(request, stored);

            Assert.True(stored.ExtraFields.ContainsKey("future"),
                "stored extras were wiped by an update that simply did not mention them");
        }

    }
}
