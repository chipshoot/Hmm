using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hmm.ServiceApi.Core.Tests
{
    /// <summary>
    /// The losslessness contract at the API write boundary. The note-level
    /// serializer already guards these cases; these tests exist because the
    /// DTO layer - which is where client writes actually enter - did not.
    /// </summary>
    public class ApiCheatsheetRowConverterTests
    {
        private static ApiCheatsheetRow Read(string json)
            => JsonConvert.DeserializeObject<ApiCheatsheetRow>(json);

        private static string Write(ApiCheatsheetRow row)
            => JsonConvert.SerializeObject(row);

        [Theory]
        [InlineData("\"garbage\"")]
        [InlineData("42")]
        [InlineData("[1,2]")]
        [InlineData("true")]
        public void MistypedSource_IsPreserved_NotDropped(string sourceJson)
        {
            // A non-object Source used to vanish twice over: it failed the
            // `is JObject` test, and the extras loop skipped it as a known key.
            var json = "{\"Label\":\"L\",\"Source\":" + sourceJson + "}";

            var row = Read(json);

            Assert.Null(row.Source);
            Assert.True(row.ExtraFields.ContainsKey("Source"));
            Assert.True(JToken.DeepEquals(JToken.Parse(sourceJson), row.ExtraFields["Source"]));
            // The converter always emits the three known scalars, so absent ones
            // come back as defaults. That adds data rather than losing it; what
            // matters is that the mistyped value itself survives the write.
            Assert.True(JToken.DeepEquals(
                JToken.Parse(sourceJson), JToken.Parse(Write(row))["Source"]));
        }

        [Theory]
        [InlineData("Label", "{\"rich\":\"text\"}")]
        [InlineData("Label", "[1,2]")]
        [InlineData("Label", "17")]
        [InlineData("ValueAction", "{\"a\":1}")]
        [InlineData("ValueAction", "5")]
        [InlineData("OpenSource", "\"maybe\"")]
        [InlineData("OpenSource", "{\"a\":1}")]
        public void MistypedKnownField_IsPreserved_NeverThrowsOrCoerces(string field, string valueJson)
        {
            // Objects/arrays threw InvalidCastException out of Value<T>();
            // "maybe" threw FormatException; a number was silently coerced
            // to its string form. All three lose or reject client data.
            var json = "{\"" + field + "\":" + valueJson + "}";

            var row = Read(json);

            Assert.True(row.ExtraFields.ContainsKey(field));
            Assert.True(JToken.DeepEquals(JToken.Parse(valueJson), row.ExtraFields[field]));
            Assert.True(JToken.DeepEquals(
                JToken.Parse(valueJson), JToken.Parse(Write(row))[field]));
        }

        [Fact]
        public void WellTypedFields_AreStillConsumed_NotTreatedAsExtras()
        {
            var row = Read("{\"Label\":\"L\",\"ValueAction\":\"call\",\"OpenSource\":false," +
                           "\"Source\":{\"NoteId\":7}}");

            Assert.Equal("L", row.Label);
            Assert.Equal("call", row.ValueAction);
            Assert.False(row.OpenSource);
            Assert.NotNull(row.Source);
            Assert.Empty(row.ExtraFields);
        }
    }
}
