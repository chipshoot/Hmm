using Hmm.Core.DefaultManager.Validation;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Xunit;

namespace Hmm.Core.Tests
{
    public class AuthorValidatorTests : TestFixtureBase
    {
        private readonly AuthorValidator _validator;

        public AuthorValidatorTests()
        {
            InsertSeedRecords();
            _validator = new AuthorValidator(AuthorRepository);
        }

        [Theory]
        [InlineData("jfang", "AccountName : Duplicated account name")]
        public void Cannot_Add_Duplicated_AccountName(string accountName, string errorMessage)
        {
            // Arrange
            var author = new Author
            {
                AccountName = accountName,
                IsActivated = true,
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(author, processResult);

            // Assert
            Assert.False(result);
            Assert.Equal(processResult.MessageList[0].Message, errorMessage);
        }

        [Fact]
        public void AuthorAccountNameMustHasValidContentLength()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "",
                IsActivated = true,
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(author, processResult);

            // Assert
            Assert.False(result);
            Assert.NotEmpty(processResult.MessageList[0].Message);

            // Arrange
            author = new Author
            {
                AccountName = GetRandomString(154),
                IsActivated = true,
            };

            // Act

            processResult = new ProcessingResult();
            result = _validator.IsValidEntity(author, processResult);

            // Assert
            Assert.True(result);
            Assert.Empty(processResult.MessageList);

            // Arrange
            author = new Author
            {
                AccountName = GetRandomString(500),
                IsActivated = true,
            };

            // Act

            processResult = new ProcessingResult();
            result = _validator.IsValidEntity(author, processResult);

            // Assert
            Assert.False(result);
            Assert.NotEmpty(processResult.MessageList[0].Message);
        }
    }
}