// Ignore Spelling: Dao

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.TestHelp;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    public class AuthorDaoRepositoryTests : DbTestFixtureBase, IAsyncLifetime
    {
        private ContactDao _defaultContact;

        [Fact]
        public async Task Can_Add_Author_To_DataSource()

        {
            // Arrange
            var author = new AuthorDao
            {
                AccountName = "fchy",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            // Act
            var savedRec = await AuthorRepository.AddAsync(author);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id != 0, "savedRec.Id is not empty id 0");
            Assert.Equal(author.Id, savedRec.Id);
        }

        [Fact]
        public async Task Cannot_Add_Already_Existed_AccountName_To_DataSource()
        {
            // Arrange
            var authorExists = new AuthorDao
            {
                Id = 1,
                AccountName = "glog",
                ContactInfo = _defaultContact,
                Description = "testing author",
                IsActivated = true
            };
            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = _defaultContact,
                Description = "testing author",
                IsActivated = true
            };

            // Act
            await AuthorRepository.AddAsync(authorExists);
            var savedAuthor = await AuthorRepository.AddAsync(author);

            // Assert
            Assert.Null(savedAuthor);
            Assert.False(AuthorRepository.ProcessMessage.Success);
            Assert.Single(AuthorRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public async Task Can_Delete_Author_From_DataSource()
        {
            // Arrange
            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            var savedAuthor = await AuthorRepository.AddAsync(author);

            // Act
            var result = await AuthorRepository.DeleteAsync(savedAuthor);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task Cannot_Delete_NonExists_Author_From_DataSource()
        {
            // Arrange

            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            await AuthorRepository.AddAsync(author);

            var author2 = new AuthorDao
            {
                Id = 1,
                AccountName = "glog2",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            // Act
            var result = await AuthorRepository.DeleteAsync(author2);

            // Assert
            Assert.False(result);
            Assert.False(AuthorRepository.ProcessMessage.Success);
            Assert.Single(AuthorRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public async Task Cannot_Delete_Author_With_Note_Associated()
        {
            // Arrange
            var catalog = new NoteCatalogDao
            {
                Name = "DefaultCatalog",
                FormatType = NoteContentFormatType.PlainText,
                Schema = "",
                IsDefault = false,
                Description = "Description"
            };
            var savedCatalog = await CatalogRepository.AddAsync(catalog);

            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };
            var savedUser = await AuthorRepository.AddAsync(author);

            var note = new HmmNoteDao
            {
                Subject = string.Empty,
                Content = string.Empty,
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Author = savedUser,
                Catalog = savedCatalog
            };
            await NoteRepository.AddAsync(note);

            // Act
            var result = await AuthorRepository.DeleteAsync(author);

            // Assert
            Assert.False(result, "Error: deleted user with note");
            Assert.False(AuthorRepository.ProcessMessage.Success);
            Assert.Single(AuthorRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public async Task Can_Update_Author()
        {
            // Arrange - update first name
            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            await AuthorRepository.AddAsync(author);

            // Arrange - activate status
            author.IsActivated = false;

            // Act
            var result = await AuthorRepository.UpdateAsync(author);

            // Arrange
            Assert.NotNull(result);
            Assert.False(result.IsActivated);

            // Arrange - update description
            author.Description = "new testing user";

            // Act
            result = await AuthorRepository.UpdateAsync(author);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new testing user", result.Description);
        }

        [Fact]
        public async Task Cannot_Update_For_Non_Exists_Author()
        {
            // Arrange
            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            await AuthorRepository.AddAsync(author);

            var author2 = new AuthorDao
            {
                AccountName = "glog2",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            // Act
            var result = await AuthorRepository.UpdateAsync(author2);

            // Assert
            Assert.Null(result);
            Assert.False(AuthorRepository.ProcessMessage.Success);
            Assert.Single(AuthorRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public async Task Cannot_Update_Author_With_Duplicated_AccountName()
        {
            // Arrange
            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };
            await AuthorRepository.AddAsync(author);

            var user2 = new AuthorDao
            {
                AccountName = "glog2",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };
            await AuthorRepository.AddAsync(user2);

            author.AccountName = user2.AccountName;

            // Act
            var result = await AuthorRepository.UpdateAsync(author);

            // Assert
            Assert.Null(result);
            Assert.False(AuthorRepository.ProcessMessage.Success);
            Assert.Single(AuthorRepository.ProcessMessage.MessageList);
        }

        public async Task InitializeAsync()
        {
            Transaction = await ((DbContext)DbContext).Database.BeginTransactionAsync();
            var contact = SampleDataGenerator.GetContactDao();
            await ContactRepository.AddAsync(contact);
            _defaultContact = contact;
        }

        public async Task DisposeAsync()
        {
            await Transaction.RollbackAsync();
            await Transaction.DisposeAsync();
        }
    }
}