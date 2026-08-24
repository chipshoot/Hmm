using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.Validator;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetValidatorTests
    {
        private static CheatsheetValidator CreateValidator(bool authorExists = true)
        {
            var lookup = new Mock<IEntityLookup>();
            // Scoped to id 9, not It.IsAny<int>(). Answering identically for every
            // id meant a validator that checked a hardcoded author instead of
            // card.AuthorId would pass both tests below unchanged.
            lookup
                .Setup(l => l.GetEntityAsync<Author>(9))
                .ReturnsAsync(authorExists
                    ? ProcessingResult<Author>.Ok(new Author { Id = 9 })
                    : ProcessingResult<Author>.NotFound());
            lookup
                .Setup(l => l.GetEntityAsync<Author>(It.Is<int>(id => id != 9)))
                .ReturnsAsync(ProcessingResult<Author>.NotFound());

            return new CheatsheetValidator(lookup.Object);
        }

        private static CheatsheetCard ValidCard() => new()
        {
            AuthorId = 9,
            Id = "card-1",
            Title = "Passport",
            WalletGroup = "Travel",
            TemplateId = "blank"
        };

        [Fact]
        public async Task ValidateEntityAsync_ValidCard_Succeeds()
        {
            var result = await CreateValidator().ValidateEntityAsync(ValidCard());

            Assert.True(result.Success);
        }

        [Fact]
        public async Task ValidateEntityAsync_MissingAuthor_Fails()
        {
            var result = await CreateValidator(authorExists: false).ValidateEntityAsync(ValidCard());

            Assert.False(result.Success);
            Assert.Contains("author", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateEntityAsync_EmptyId_Fails()
        {
            var card = ValidCard();
            card.Id = string.Empty;

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
            Assert.Contains("card id is required", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateEntityAsync_EmptyTitle_Fails()
        {
            var card = ValidCard();
            card.Title = string.Empty;

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
            Assert.Contains("title", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateEntityAsync_OverlongTitle_Fails()
        {
            var card = ValidCard();
            card.Title = new string('x', 201);

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
            Assert.Contains("title", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateEntityAsync_EmptyWalletGroup_Fails()
        {
            var card = ValidCard();
            card.WalletGroup = string.Empty;

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
            Assert.Contains("wallet group", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateEntityAsync_EmptyTemplateId_Fails()
        {
            var card = ValidCard();
            card.TemplateId = string.Empty;

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
            Assert.Contains("template", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateEntityAsync_UnreadableRowsAndExtras_StillSucceeds()
        {
            // The whole point: rows this version cannot model must not make the
            // card unsaveable, or the API becomes the thing that deletes them.
            using var document = JsonDocument.Parse("\"corrupt\"");
            var card = ValidCard();
            card.Rows = new List<CheatsheetRow>
            {
                new() { RawJson = document.RootElement.Clone() },
                new() { Label = string.Empty, ValueAction = "sms" }
            };
            card.ExtraFields["future"] = document.RootElement.Clone();

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.True(result.Success);
        }

        [Fact]
        public void Constructor_Throws_WhenLookupIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new CheatsheetValidator(null));
        }

        [Fact]
        public async Task ValidateEntityAsync_ChecksTheCardsOwnAuthor_NotAFixedOne()
        {
            var card = ValidCard();
            card.AuthorId = 999;

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
            Assert.Contains("author", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
        }

    }
}
