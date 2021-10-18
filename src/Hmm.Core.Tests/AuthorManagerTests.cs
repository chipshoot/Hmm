using Hmm.Core.DefaultManager;
using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System;
using System.Linq;
using Xunit;

namespace Hmm.Core.Tests

{
    public class AuthorManagerTests : TestFixtureBase
    {
        #region private fields

        private IAuthorManager _authorManager;
        private MockAuthorValidator _testValidator;

        #endregion private fields

        public AuthorManagerTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void Can_Get_Author()
        {
            // Act
            var authors = _authorManager.GetEntities().ToList();

            // Assert
            Assert.True(_authorManager.ProcessResult.Success);
            Assert.True(authors.Count > 1, "authors.Count > 1");
        }

        [Fact]
        public void Can_Add_Valid_Author()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "jfang2",
                IsActivated = true
            };

            // Act
            var newAuthor = _authorManager.Create(author);

            // Assert
            Assert.True(_authorManager.ProcessResult.Success);
            Assert.NotNull(newAuthor);
            Assert.True(newAuthor.Id != Guid.Empty, "newAuthor.Id is not empty");
        }

        [Fact]
        public void Cannot_Add_Invalid_Author()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "Test Account Name",
                IsActivated = true,
            };
            _testValidator.GetInvalidResult = true;

            // Act
            var newAuthor = _authorManager.Create(author);

            // Assert
            Assert.False(_authorManager.ProcessResult.Success);
            Assert.True(_authorManager.ProcessResult.MessageList.FirstOrDefault()?.Message.Contains("Author is invalid"));
            Assert.Null(newAuthor);
        }

        [Fact]
        public void Can_Update_Valid_Author()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "jfang2",
                Role = AuthorRoleType.Author,
                IsActivated = true,
            };
            var result = _authorManager.Create(author);
            Assert.True(author.Id != Guid.Empty, "user.Id is not Guid empty");

            //   Act
            var savedAuthor = _authorManager.GetEntities().FirstOrDefault(a => a.Id == result.Id);
            Assert.NotNull(savedAuthor);
            savedAuthor.Role = AuthorRoleType.Guest;
            var updatedAuthor = _authorManager.Update(savedAuthor);

            //  Assert
            Assert.NotNull(updatedAuthor);
            Assert.Equal(AuthorRoleType.Guest, updatedAuthor.Role);
            Assert.True(_authorManager.ProcessResult.Success);
            Assert.False(_authorManager.ProcessResult.MessageList.Any());
        }

        [Fact]
        public void Cannot_Update_InValid_Author()
        {
            //    Arrange
            var author = new Author
            {
                AccountName = "jfang2",
                IsActivated = true,
            };
            _authorManager.Create(author);
            Assert.True(author.Id != Guid.Empty, "user.Id is not empty guid");
            _testValidator.GetInvalidResult = true;

            //   Act
            author.AccountName = "jfang3";
            var newAuthor = _authorManager.Update(author);

            //  Assert
            Assert.False(_authorManager.ProcessResult.Success);
            Assert.True(_authorManager.ProcessResult.MessageList.FirstOrDefault()?.Message.Contains("Author is invalid"));
            Assert.Null(newAuthor);
        }

        [Fact]
        public void Cannot_Update_Not_Exists_Author()
        {
            // Arrange - no id
            var author = new Author
            {
                AccountName = "jfang2",
                IsActivated = true,
            };

            //   Act
            var newAuthor = _authorManager.Update(author);

            //  Assert
            Assert.False(_authorManager.ProcessResult.Success);
            Assert.Null(newAuthor);

            // Arrange - id not exist
            _authorManager.ProcessResult.Rest();
            author = new Author
            {
                Id = Guid.NewGuid(),
                AccountName = "jfang2",
                IsActivated = true,
            };

            // Act
            newAuthor = _authorManager.Update(author);

            //  Assert
            Assert.False(_authorManager.ProcessResult.Success);
            Assert.Null(newAuthor);
        }

        [Fact]
        public void Can_Deactivate_Author()
        {
            // Arrange
            var author = _authorManager.GetEntities().FirstOrDefault();
            Assert.NotNull(author);
            Assert.True(author.IsActivated);

            // Act
            _authorManager.DeActivate(author.Id);

            // Assert
            Assert.True(_authorManager.ProcessResult.Success);
            Assert.False(_authorManager.ProcessResult.MessageList.Any());
            Assert.False(author.IsActivated);
        }

        [Theory]
        [InlineData("00000000-0000-0000-0000-000000000000", false)]
        [InlineData("FirstID", true)]
        public void Can_Check_Author_Exists(string authorId, bool expected)
        {
            // Arrange
            var id = authorId;
            if (authorId == "FirstID")
            {
                id = LookupRepo.GetEntities<Author>().FirstOrDefault()?.Id.ToString();
            }

            // Act
            var result = _authorManager.AuthorExists(id);

            // Assert
            Assert.Equal(result, expected);
        }

        [Fact]
        public void Cannot_Check_Author_Exists_With_Invalid_Id()
        {
            // Arrange
            var id = "";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _authorManager.AuthorExists(id));

            // Arrange
            id = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _authorManager.AuthorExists(id));
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _testValidator = FakeAuthorValidator;
            _authorManager = new AuthorManager(AuthorRepository, _testValidator);
        }
    }
}