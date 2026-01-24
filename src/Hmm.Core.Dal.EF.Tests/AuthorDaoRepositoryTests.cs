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
                AccountName = "fchy-dal-test",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            // Act
            var result = await AuthorRepository.AddAsync(author);
            await DbContext.CommitAsync();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id != 0, "savedRec.Id is not empty id 0");
            Assert.Equal(author.Id, result.Value.Id);
        }

        [Fact]
        public async Task Can_Add_Author_With_Null_Contact_To_DataSource()

        {
            // Arrange
            var author = new AuthorDao
            {
                AccountName = "NotExistUser-dal-test",
                ContactInfo = null,
                Description = "testing user",
                IsActivated = true
            };

            // Act
            var result = await AuthorRepository.AddAsync(author);
            await DbContext.CommitAsync();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id != 0, "savedRec.Id is not empty id 0");
            Assert.Equal(author.Id, result.Value.Id);
        }

        [Fact]
        public async Task Cannot_Add_Already_Existed_AccountName_To_DataSource()
        {
            // Arrange
            var authorExists = new AuthorDao
            {
                Id = 1,
                AccountName = "glog-dal-test",
                ContactInfo = _defaultContact,
                Description = "testing author",
                IsActivated = true
            };
            var author = new AuthorDao
            {
                AccountName = "glog-dal-test",
                ContactInfo = _defaultContact,
                Description = "testing author",
                IsActivated = true
            };

            // Act
            await AuthorRepository.AddAsync(authorExists);
            await DbContext.CommitAsync();
            var result = await AuthorRepository.AddAsync(author);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Messages.Count > 0);
        }

        [Fact]
        public async Task Can_Delete_Author_From_DataSource()
        {
            // Arrange
            var author = new AuthorDao
            {
                AccountName = "glog-dal-test",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            var addResult = await AuthorRepository.AddAsync(author);
            await DbContext.CommitAsync();

            // Act
            var result = await AuthorRepository.DeleteAsync(addResult.Value);
            await DbContext.CommitAsync();

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task Cannot_Delete_NonExists_Author_From_DataSource()
        {
            // Arrange

            var author = new AuthorDao
            {
                AccountName = "glog-dal-test",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            await AuthorRepository.AddAsync(author);
            await DbContext.CommitAsync();

            var author2 = new AuthorDao
            {
                Id = 1,
                AccountName = "glog-dal-test2",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            // Act
            var result = await AuthorRepository.DeleteAsync(author2);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Messages.Count > 0);
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
            var catalogResult = await CatalogRepository.AddAsync(catalog);
            await DbContext.CommitAsync();

            var author = new AuthorDao
            {
                AccountName = "glog-dal-test",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };
            var authorResult = await AuthorRepository.AddAsync(author);
            await DbContext.CommitAsync();

            var note = new HmmNoteDao
            {
                Subject = string.Empty,
                Content = string.Empty,
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Author = authorResult.Value,
                Catalog = catalogResult.Value
            };
            await NoteRepository.AddAsync(note);
            await DbContext.CommitAsync();

            // Act
            var result = await AuthorRepository.DeleteAsync(author);

            // Assert
            Assert.False(result.Success, "Error: deleted user with note");
            Assert.True(result.Messages.Count > 0);
        }

        [Fact]
        public async Task Can_Update_Author()
        {
            // Arrange - update first name
            var author = new AuthorDao
            {
                AccountName = "glog-dal-test",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            await AuthorRepository.AddAsync(author);
            await DbContext.CommitAsync();

            // Arrange - activate status
            author.IsActivated = false;

            // Act
            var result = await AuthorRepository.UpdateAsync(author);
            await DbContext.CommitAsync();

            // Arrange
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.False(result.Value.IsActivated);

            // Arrange - update description
            author.Description = "new testing user";

            // Act
            result = await AuthorRepository.UpdateAsync(author);
            await DbContext.CommitAsync();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("new testing user", result.Value.Description);
        }

        [Fact]
        public async Task Can_Update_Author_With_New_Contact()
        {
            // Arrange - update first name
            var author = new AuthorDao
            {
                AccountName = "glog-dal-test",
                ContactInfo = null,
                Description = "testing user",
                IsActivated = true
            };

            await AuthorRepository.AddAsync(author);
            await DbContext.CommitAsync();

            // Arrange - activate status
            author.ContactInfo = _defaultContact;

            // Act
            var result = await AuthorRepository.UpdateAsync(author);
            await DbContext.CommitAsync();

            // Arrange
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.NotNull(result.Value.ContactInfo);

            // Arrange - update description
            author.Description = "new testing user";

            // Act
            result = await AuthorRepository.UpdateAsync(author);
            await DbContext.CommitAsync();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("new testing user", result.Value.Description);
        }

        [Fact]
        public async Task Cannot_Update_For_Non_Exists_Author()
        {
            // Arrange
            var author = new AuthorDao
            {
                AccountName = "glog-dal-test",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            await AuthorRepository.AddAsync(author);
            await DbContext.CommitAsync();

            var author2 = new AuthorDao
            {
                AccountName = "glog-dal-test2",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };

            // Act
            var result = await AuthorRepository.UpdateAsync(author2);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Messages.Count > 0);
        }

        [Fact]
        public async Task Cannot_Update_Author_With_Duplicated_AccountName()
        {
            // Arrange
            var author = new AuthorDao
            {
                AccountName = "glog-dal-test",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };
            await AuthorRepository.AddAsync(author);
            await DbContext.CommitAsync();

            var user2 = new AuthorDao
            {
                AccountName = "glog-dal-test2",
                ContactInfo = _defaultContact,
                Description = "testing user",
                IsActivated = true
            };
            await AuthorRepository.AddAsync(user2);
            await DbContext.CommitAsync();

            author.AccountName = user2.AccountName;

            // Act
            var result = await AuthorRepository.UpdateAsync(author);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Messages.Count > 0);
        }

        public async Task InitializeAsync()
        {
            Transaction = await ((DbContext)DbContext).Database.BeginTransactionAsync();
            var contact = SampleDataGenerator.GetContactDao();
            var result = await ContactRepository.AddAsync(contact);
            _defaultContact = result.Value;
            await DbContext.CommitAsync();
        }

        public async Task DisposeAsync()
        {
            await Transaction.RollbackAsync();
            await Transaction.DisposeAsync();
        }
    }
}