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
    /// The client's codec deliberately preserves rows it cannot parse and
    /// re-saves them untouched. A backend that validated strictly and dropped
    /// what it did not understand would silently delete the very data the
    /// client is protecting. These tests are the contract.
    /// </summary>
    public class CheatsheetRoundTripTests
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

        private static HmmNote NoteWith(string cardJson)
            => new()
            {
                Id = 42,
                Subject = CheatsheetCard.GetNoteSubject("card-1"),
                Content = "{\"note\":{\"content\":{\"Cheatsheet\":" + cardJson + "}}}",
                Author = new Author { Id = 9 }
            };

        /// <summary>
        /// Order-insensitive structural equality: JSON object key order is not
        /// semantic, array order is.
        /// </summary>
        private static bool JsonEquals(JsonElement left, JsonElement right)
        {
            if (left.ValueKind != right.ValueKind)
            {
                return false;
            }

            switch (left.ValueKind)
            {
                case JsonValueKind.Object:
                    var leftProps = left.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                    var rightProps = right.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                    if (leftProps.Count != rightProps.Count)
                    {
                        return false;
                    }

                    foreach (var pair in leftProps)
                    {
                        if (!rightProps.TryGetValue(pair.Key, out var other) ||
                            !JsonEquals(pair.Value, other))
                        {
                            return false;
                        }
                    }

                    return true;

                case JsonValueKind.Array:
                    var leftItems = left.EnumerateArray().ToList();
                    var rightItems = right.EnumerateArray().ToList();
                    if (leftItems.Count != rightItems.Count)
                    {
                        return false;
                    }

                    return !leftItems.Where((t, i) => !JsonEquals(t, rightItems[i])).Any();

                case JsonValueKind.String:
                    return left.GetString() == right.GetString();

                case JsonValueKind.Number:
                    return left.GetDouble().Equals(right.GetDouble());

                default:
                    return true; // True / False / Null / Undefined - kind match is enough.
            }
        }

        private static JsonElement CardElementOf(string noteJson)
        {
            using var document = JsonDocument.Parse(noteJson);
            return document.RootElement
                .GetProperty("note")
                .GetProperty("content")
                .GetProperty("Cheatsheet")
                .Clone();
        }

        private static async Task AssertLosslessAsync(string cardJson)
        {
            // Arrange
            var serializer = CreateSerializer();
            var note = NoteWith(cardJson);

            // Act - read, then write back out.
            var readResult = await serializer.GetEntity(note);
            Assert.True(readResult.Success, readResult.ErrorMessage);
            var rewritten = serializer.GetNoteSerializationText(readResult.Value);

            // Assert
            using var originalDocument = JsonDocument.Parse(cardJson);
            var actual = CardElementOf(rewritten);
            Assert.True(
                JsonEquals(originalDocument.RootElement, actual),
                $"Round-trip lost data.\nExpected: {originalDocument.RootElement.GetRawText()}\nActual:   {actual.GetRawText()}");
        }

        [Fact]
        public Task RoundTrip_FullyKnownCard_IsLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"Passport\"," +
                "\"walletGroup\":\"Travel\",\"tags\":[\"trip\",\"id\"],\"templateId\":\"blank\"," +
                "\"protected\":true,\"rows\":[{\"label\":\"Number\",\"valueAction\":\"call\"," +
                "\"openSource\":false,\"source\":{\"noteUuid\":\"u-1\",\"kind\":\"field\"," +
                "\"locator\":\"Passport.number\"}}]}");

        [Fact]
        public Task RoundTrip_UnknownCardFields_AreLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":2,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[],\"templateId\":\"blank\",\"protected\":false,\"rows\":[]," +
                "\"quickAccess\":true,\"sortOrder\":5,\"future\":{\"nested\":[1,2,3]}}");

        [Fact]
        public Task RoundTrip_UnknownRowAndSourceFields_AreLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[],\"templateId\":\"blank\",\"protected\":false,\"rows\":[" +
                "{\"label\":\"L\",\"valueAction\":\"none\",\"openSource\":true," +
                "\"icon\":\"star\",\"copyOnTap\":true," +
                "\"source\":{\"noteUuid\":\"u\",\"kind\":\"whole\",\"revision\":12}}]}");

        [Fact]
        public Task RoundTrip_UnknownValueActionToken_IsLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[],\"templateId\":\"blank\",\"protected\":false,\"rows\":[" +
                "{\"label\":\"L\",\"valueAction\":\"sms\",\"openSource\":true," +
                "\"source\":{\"noteUuid\":\"u\",\"kind\":\"paragraph\"}}]}");

        [Fact]
        public Task RoundTrip_NonObjectRows_AreLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[],\"templateId\":\"blank\",\"protected\":false," +
                "\"rows\":[\"corrupt\",42,null,{\"label\":\"L\",\"valueAction\":\"none\",\"openSource\":true}]}");

        [Fact]
        public Task RoundTrip_RowWithNonObjectSource_IsLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[],\"templateId\":\"blank\",\"protected\":false," +
                "\"rows\":[{\"label\":\"L\",\"source\":\"oops\",\"keepMe\":9}]}");

        [Fact]
        public Task RoundTrip_MistypedKnownFields_AreLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":\"one\",\"id\":\"card-1\",\"title\":17,\"walletGroup\":null," +
                "\"tags\":[\"ok\",7],\"templateId\":\"blank\",\"protected\":\"yes\",\"rows\":[]}");

        [Fact]
        public async Task RoundTrip_IsStableAcrossRepeatedSaves()
        {
            // A second save must not drift: extras carried on the first pass
            // have to survive the next read unchanged.
            const string cardJson =
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[\"a\"],\"templateId\":\"blank\",\"protected\":false," +
                "\"rows\":[\"corrupt\",{\"label\":\"L\",\"valueAction\":\"none\"," +
                "\"openSource\":true,\"icon\":\"star\"}],\"future\":{\"x\":1}}";

            var serializer = CreateSerializer();

            var firstRead = await serializer.GetEntity(NoteWith(cardJson));
            Assert.True(firstRead.Success);
            var firstWrite = serializer.GetNoteSerializationText(firstRead.Value);

            var secondRead = await serializer.GetEntity(NoteWith(CardElementOf(firstWrite).GetRawText()));
            Assert.True(secondRead.Success);
            var secondWrite = serializer.GetNoteSerializationText(secondRead.Value);

            using var firstDocument = JsonDocument.Parse(firstWrite);
            using var secondDocument = JsonDocument.Parse(secondWrite);
            Assert.True(
                JsonEquals(firstDocument.RootElement, secondDocument.RootElement),
                $"Second save drifted.\nFirst:  {firstWrite}\nSecond: {secondWrite}");
        }

        [Fact]
        public async Task RoundTrip_PreservesRowOrder()
        {
            var serializer = CreateSerializer();
            var note = NoteWith(
                "{\"id\":\"card-1\",\"rows\":[{\"label\":\"one\"},\"corrupt\",{\"label\":\"three\"}]}");

            var readResult = await serializer.GetEntity(note);
            Assert.True(readResult.Success);
            var rewritten = serializer.GetNoteSerializationText(readResult.Value);

            var rows = CardElementOf(rewritten).GetProperty("rows");
            Assert.Equal(3, rows.GetArrayLength());
            Assert.Equal("one", rows[0].GetProperty("label").GetString());
            Assert.Equal("corrupt", rows[1].GetString());
            Assert.Equal("three", rows[2].GetProperty("label").GetString());
        }
    }
}
