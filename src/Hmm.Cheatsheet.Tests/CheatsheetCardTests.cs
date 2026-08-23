using Hmm.Cheatsheet.DomainEntity;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetCardTests
    {
        [Fact]
        public void GetNoteSubject_PrefixesCardId()
        {
            // Arrange
            const string cardId = "6f1c9a1e-0000-4000-8000-abcdefabcdef";

            // Act
            var subject = CheatsheetCard.GetNoteSubject(cardId);

            // Assert
            Assert.Equal("Cheatsheet:6f1c9a1e-0000-4000-8000-abcdefabcdef", subject);
        }

        [Fact]
        public void NewCard_UsesClientDefaults()
        {
            // Arrange & Act
            var card = new CheatsheetCard();

            // Assert
            Assert.Equal(1, card.SchemaVersion);
            Assert.Equal("Ungrouped", card.WalletGroup);
            Assert.Equal("blank", card.TemplateId);
            Assert.False(card.Protected);
            Assert.Empty(card.Tags);
            Assert.Empty(card.Rows);
            Assert.Empty(card.ExtraFields);
        }

        [Fact]
        public void NewRow_DefaultsToUnboundOpenSourceNoAction()
        {
            // Arrange & Act
            var row = new CheatsheetRow();

            // Assert
            Assert.Equal(string.Empty, row.Label);
            Assert.Equal("none", row.ValueAction);
            Assert.True(row.OpenSource);
            Assert.Null(row.Source);
            Assert.False(row.IsUnreadable);
        }

        [Fact]
        public void NewSource_DefaultsToWholeNoteGranularity()
        {
            // Arrange & Act
            var source = new CheatsheetSource();

            // Assert
            Assert.Equal(string.Empty, source.NoteUuid);
            Assert.Equal("whole", source.Kind);
            Assert.Null(source.Locator);
        }

        [Fact]
        public void Constants_MatchTheClientContract()
        {
            Assert.Equal("Hmm.CheatsheetMan.Cheatsheet", CheatsheetConstant.CheatsheetCatalogName);
            Assert.Equal("Cheatsheet:", CheatsheetConstant.CheatsheetSubjectPrefix);
            Assert.Equal("Cheatsheet", CheatsheetConstant.CheatsheetContentKey);
            Assert.Equal(1, CheatsheetConstant.CurrentSchemaVersion);
        }
    }
}
