using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests
{
    public class AuthorValidatorTests : CoreTestFixtureBase
    {
        private AuthorValidator _validator;

        public AuthorValidatorTests()
        {
            SetupTestEnv();
        }

        [Theory]
        [InlineData("jfang", false, "AccountName : Duplicated account name")]
        [InlineData("luck", true, "")]
        public async Task Cannot_Add_Duplicated_AccountName(string accountName, bool expectValid, string errorMessage)
        {
            // Arrange
            var author = new Author
            {
                AccountName = accountName,
                IsActivated = true,
            };

            // Act

            var result = await _validator.ValidateEntityAsync(author);

            // Assert
            Assert.Equal(result.Success, expectValid);
            if (!expectValid)
            {
                Assert.Equal(result.Messages[0].Message, errorMessage);
            }
        }

        //[Theory]
        //[InlineData("awang", "jfang", false, "AccountName : Duplicated account name")]
        //[InlineData("awang", "luck", true, "")]
        //public async Task Cannot_Update_Current_Author_With_Duplicated_AccountName(string curAccName, string newAccName, bool expectValid, string errorMessage)
        //{
        //    // Arrange
        //    var curAuthorResult = await _manager.GetEntitiesAsync(a => a.AccountName == curAccName);
        //    var curAuthor = curAuthorResult.Value.FirstOrDefault();
        //    Assert.True(curAuthorResult.Success);
        //    Assert.NotNull(curAuthor);

        //    // Act
        //    curAuthor.AccountName = newAccName;
        //    var result = await _validator.ValidateEntityAsync(curAuthor);

        //    // Assert
        //    Assert.Equal(result.Success, expectValid);
        //    if (!expectValid)
        //    {
        //        Assert.Equal(result.Messages[0].Message, errorMessage);
        //    }
        //}

        [Fact]
        public async Task AuthorAccountNameMustHasValidContentLength()
        {
            // Arrange
            var author = new  Author
            {
                AccountName = "",
                IsActivated = true,
            };

            // Act
            var result = await _validator.ValidateEntityAsync(author);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Messages[0].Message);

            // Arrange
            author = new Author
            {
                AccountName = GetRandomString(154),
                IsActivated = true,
            };

            // Act
            result = await _validator.ValidateEntityAsync(author);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.Messages);

            // Arrange
            author = new Author
            {
                AccountName = GetRandomString(500),
                IsActivated = true,
            };

            // Act
            result = await _validator.ValidateEntityAsync(author);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Messages[0].Message);
        }

        private void SetupTestEnv()
        {
            _validator = new AuthorValidator(AuthorRepository);
        }
    }
}