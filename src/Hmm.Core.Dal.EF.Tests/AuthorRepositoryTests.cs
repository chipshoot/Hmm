using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    public class AuthorRepositoryTests : DbTestFixtureBase
    {
        [Fact]
        public void Can_Add_Author_To_DataSource()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "glog",
                Description = "testing user",
                IsActivated = true
            };

            // Act
            var savedRec = AuthorRepository.Add(author);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id != Guid.Empty, "savedRec.Id is not empty GUID 0");
            Assert.Equal(author.Id, savedRec.Id);
        }

        [Fact]
        public void Cannot_Add_Already_Existed_AccountName_To_DataSource()
        {
            // Arrange
            var authorExists = new Author
            {
                Id = Guid.NewGuid(),
                AccountName = "glog",
                Description = "testing author",
                IsActivated = true
            };

            var author = new Author
            {
                AccountName = "glog",
                Description = "testing author",
                IsActivated = true
            };

            // Act
            AuthorRepository.Add(authorExists);
            var savedAuthor = AuthorRepository.Add(author);

            // Assert
            Assert.Null(savedAuthor);
            Assert.False(AuthorRepository.ProcessMessage.Success);
            Assert.Single(AuthorRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Can_Delete_Author_From_DataSource()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "glog",
                Description = "testing user",
                IsActivated = true
            };

            var savedAuthor = AuthorRepository.Add(author);

            // Act
            var result = AuthorRepository.Delete(savedAuthor);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Cannot_Delete_NonExists_Author_From_DataSource()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "glog",
                Description = "testing user",
                IsActivated = true
            };

            AuthorRepository.Add(author);

            var author2 = new Author
            {
                Id = Guid.NewGuid(),
                AccountName = "glog2",
                Description = "testing user",
                IsActivated = true
            };

            // Act
            var result = AuthorRepository.Delete(author2);

            // Assert
            Assert.False(result);
            Assert.False(AuthorRepository.ProcessMessage.Success);
            Assert.Single(AuthorRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Cannot_Delete_Author_With_Note_Associated()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "DefaultRender",
                Namespace = "NameSpace",
                IsDefault = true,
                Description = "Description"
            };
            var savedRender = RenderRepository.Add(render);

            var catalog = new NoteCatalog
            {
                Name = "DefaultCatalog",
                Render = savedRender,
                Schema = "testScheme",
                IsDefault = false,
                Description = "Description"
            };
            var savedCatalog = CatalogRepository.Add(catalog);

            var author = new Author
            {
                AccountName = "glog",
                Description = "testing user",
                IsActivated = true
            };
            var savedUser = AuthorRepository.Add(author);

            var note = new HmmNote
            {
                Subject = string.Empty,
                Content = string.Empty,
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Author = savedUser,
                Catalog = savedCatalog
            };
            NoteRepository.Add(note);

            // Act
            var result = AuthorRepository.Delete(author);

            // Assert
            Assert.False(result, "Error: deleted user with note");
            Assert.False(AuthorRepository.ProcessMessage.Success);
            Assert.Single(AuthorRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Can_Update_Author()
        {
            // Arrange - update first name
            var author = new Author
            {
                AccountName = "glog",
                Description = "testing user",
                IsActivated = true
            };

            AuthorRepository.Add(author);

            // Arrange - activate status
            author.IsActivated = false;

            // Act
            var result = AuthorRepository.Update(author);

            // Arrange
            Assert.NotNull(result);
            Assert.False(result.IsActivated);

            // Arrange - update description
            author.Description = "new testing user";

            // Act
            result = AuthorRepository.Update(author);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new testing user", result.Description);
        }

        [Fact]
        public void Cannot_Update_For_Non_Exists_Author()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "glog",
                Description = "testing user",
                IsActivated = true
            };

            AuthorRepository.Add(author);

            var author2 = new Author
            {
                AccountName = "glog2",
                Description = "testing user",
                IsActivated = true
            };

            // Act
            var result = AuthorRepository.Update(author2);

            // Assert
            Assert.Null(result);
            Assert.False(AuthorRepository.ProcessMessage.Success);
            Assert.Single(AuthorRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Cannot_Update_Author_With_Duplicated_AccountName()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "glog",
                Description = "testing user",
                IsActivated = true
            };
            AuthorRepository.Add(author);

            var user2 = new Author
            {
                AccountName = "glog2",
                Description = "testing user",
                IsActivated = true
            };
            AuthorRepository.Add(user2);

            author.AccountName = user2.AccountName;

            // Act
            var result = AuthorRepository.Update(author);

            // Assert
            Assert.Null(result);
            Assert.False(AuthorRepository.ProcessMessage.Success);
            Assert.Single(AuthorRepository.ProcessMessage.MessageList);
        }
    }
}