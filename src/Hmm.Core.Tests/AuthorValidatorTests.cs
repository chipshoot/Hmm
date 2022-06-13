using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using System.Linq;
using Xunit;

namespace Hmm.Core.Tests
{
    public class AuthorValidatorTests : TestFixtureBase
    {
        private AuthorValidator _validator;
        private IAuthorManager _manager;

        public AuthorValidatorTests()
        {
            SetupTestEnv();
        }

        [Theory]
        [InlineData("jfang", false, "AccountName : Duplicated account name")]
        [InlineData("luck", true, "")]
        public void Cannot_Add_Duplicated_AccountName(string accountName, bool expectValid, string errorMessage)
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
            Assert.Equal(result, expectValid);
            if (!expectValid)
            {
                Assert.Equal(processResult.MessageList[0].Message, errorMessage);
            }
        }

        [Theory]
        [InlineData("awang", "jfang", false, "AccountName : Duplicated account name")]
        [InlineData("awang", "luck", true, "")]
        public void Cannot_Update_Current_Author_With_Duplicated_AccountName(string curAccName, string newAccName, bool expectValid, string errorMessage)
        {
            // Arrange
            var curAuthor = _manager.GetEntities(a => a.AccountName == curAccName).FirstOrDefault();
            Assert.NotNull(curAuthor);

            // Act
            curAuthor.AccountName = newAccName;
            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(curAuthor, processResult);

            // Assert
            Assert.Equal(result, expectValid);
            if (!expectValid)
            {
                Assert.Equal(processResult.MessageList[0].Message, errorMessage);
            }
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

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new AuthorValidator(AuthorRepository);
            _manager = new AuthorManager(AuthorRepository, _validator);
        }
    }
}