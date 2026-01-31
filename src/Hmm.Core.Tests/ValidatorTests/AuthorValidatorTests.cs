using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Collections.Generic;
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
        [InlineData("jfang", false, "AccountName: Duplicated account name")]
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

        #region Thread Safety Tests

        /// <summary>
        /// Verifies that concurrent validation calls on the same validator instance
        /// produce independent results without interference.
        /// This test validates the thread-safety fix for issue #42.
        /// </summary>
        [Fact]
        public async Task ConcurrentValidations_ProduceIndependentResults()
        {
            // Arrange - create multiple authors with different validity states
            var validAuthor1 = new Author { AccountName = GetRandomString(10), IsActivated = true };
            var validAuthor2 = new Author { AccountName = GetRandomString(10), IsActivated = true };
            var invalidAuthor1 = new Author { AccountName = "jfang", IsActivated = true }; // duplicate
            var invalidAuthor2 = new Author { AccountName = "", IsActivated = true }; // empty name

            // Act - run all validations concurrently
            var tasks = new List<Task<Hmm.Utility.Misc.ProcessingResult<Author>>>
            {
                _validator.ValidateEntityAsync(validAuthor1),
                _validator.ValidateEntityAsync(validAuthor2),
                _validator.ValidateEntityAsync(invalidAuthor1),
                _validator.ValidateEntityAsync(invalidAuthor2)
            };

            var results = await Task.WhenAll(tasks);

            // Assert - each result should be independent and correct
            Assert.True(results[0].Success, "First valid author should pass validation");
            Assert.True(results[1].Success, "Second valid author should pass validation");
            Assert.False(results[2].Success, "Duplicate account name should fail validation");
            Assert.False(results[3].Success, "Empty account name should fail validation");
        }

        /// <summary>
        /// Verifies that many concurrent validations complete without errors.
        /// This stress test ensures no race conditions occur under load.
        /// </summary>
        [Fact]
        public async Task HighConcurrencyValidations_CompleteWithoutErrors()
        {
            // Arrange - create many authors for concurrent validation
            const int concurrencyLevel = 50;
            var authors = Enumerable.Range(0, concurrencyLevel)
                .Select(i => new Author
                {
                    AccountName = GetRandomString(10) + i, // ensure unique names
                    IsActivated = true
                })
                .ToList();

            // Act - run all validations concurrently
            var tasks = authors.Select(a => _validator.ValidateEntityAsync(a)).ToList();
            var results = await Task.WhenAll(tasks);

            // Assert - all validations should complete successfully
            var successCount = results.Count(r => r.Success);
            Assert.Equal(concurrencyLevel, successCount);
        }

        /// <summary>
        /// Verifies that concurrent validations of the same entity produce consistent results.
        /// </summary>
        [Fact]
        public async Task SameEntityConcurrentValidations_ProduceConsistentResults()
        {
            // Arrange - same author validated multiple times concurrently
            var author = new Author
            {
                AccountName = "jfang", // known duplicate
                IsActivated = true
            };

            // Act - validate same entity 10 times concurrently
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => _validator.ValidateEntityAsync(author))
                .ToList();

            var results = await Task.WhenAll(tasks);

            // Assert - all results should be consistent (all failures with same error)
            Assert.All(results, r =>
            {
                Assert.False(r.Success);
                Assert.Contains("Duplicated account name", r.Messages[0].Message);
            });
        }

        #endregion Thread Safety Tests

        private void SetupTestEnv()
        {
            _validator = new AuthorValidator(AuthorRepository);
        }
    }
}