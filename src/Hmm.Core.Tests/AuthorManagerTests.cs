using Hmm.Core.DefaultManager;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests
{
    public class AuthorManagerTests : CoreTestFixtureBase
    {
        private readonly AuthorManager _authorManager;

        public AuthorManagerTests()
        {
            _authorManager = new AuthorManager(AuthorRepository, Mapper, LookupRepository);
        }

        [Fact]
        public async Task Can_Get_Author()
        {
            // Act
            var authorsResult = await _authorManager.GetEntitiesAsync();

            // Assert
            Assert.True(authorsResult.Success);
            Assert.True(authorsResult.Value.Count >= 1, "authors.Count >= 1");
        }

        [Fact]
        public async Task Can_Get_Author_With_Query()
        {
            // Act
            var authorsResult = await _authorManager.GetEntitiesAsync(a => a.AccountName == "fchy");

            // Assert
            Assert.True(authorsResult.Success);
            Assert.Single(authorsResult.Value);
        }

        [Fact]
        public async Task Can_Add_Valid_Author()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "jfang2",
                ContactInfo = SampleDataGenerator.GetContact(),
                Description = "Testing author",
                IsActivated = true
            };
            var contactsResult = await ContactRepository.GetEntitiesAsync();
            var contactCount = contactsResult.Value.Count;

            // Act
            var newAuthorResult = await _authorManager.CreateAsync(author);
            contactsResult = await ContactRepository.GetEntitiesAsync();
            var contactCount2 = contactsResult.Value.Count;

            // Assert
            Assert.True(newAuthorResult.Success);
            Assert.NotNull(newAuthorResult.Value);
            Assert.True(newAuthorResult.Value.Id > 0, "newAuthor.Id is greater to 0");
            Assert.Equal(contactCount + 1, contactCount2);
            Assert.True(newAuthorResult.Value.ContactInfo.Id > 0, "newAuthor's contact Id is greater to 0");
        }

        [Fact]
        public async Task Can_Add_Valid_Author_With_Exits_Contact()
        {
            // Arrange
            var contactDaosResult = await ContactRepository.GetEntitiesAsync();
            var contact = Mapper.Map<Contact>(contactDaosResult.Value.FirstOrDefault());
            var author = new Author
            {
                AccountName = "jfang2",
                ContactInfo = contact,
                Description = "Testing author",
                IsActivated = true
            };
            var contactsResult = await ContactRepository.GetEntitiesAsync();
            var contactCount = contactsResult.Value.Count;

            // Act
            var newAuthorResult = await _authorManager.CreateAsync(author);
            contactsResult = await ContactRepository.GetEntitiesAsync();
            var contactCount2 = contactsResult.Value.Count;

            // Assert
            Assert.True(newAuthorResult.Success);
            Assert.NotNull(newAuthorResult.Value);
            Assert.True(newAuthorResult.Value.Id > 0, "newAuthor.Id is greater to 0");
            Assert.Equal(contactCount, contactCount2);
            Assert.True(newAuthorResult.Value.ContactInfo.Id > 0, "newAuthor's contact Id is greater to 0");
        }

        [Fact]
        public async Task Can_Add_Author_With_Null_Contact()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "jfang2",
                Description = "Testing author",
                IsActivated = true
            };

            // Act
            var newAuthorResult = await _authorManager.CreateAsync(author);

            // Assert
            Assert.True(newAuthorResult.Success);
            Assert.NotNull(newAuthorResult.Value);
            Assert.True(newAuthorResult.Value.Id >= 0, "newAuthor.Id is greater to 0");
        }

        [Fact]
        public async Task Cannot_Add_Invalid_Author()
        {
            // Arrange

            var author = new Author
            {
                AccountName = GetRandomString(300),
                ContactInfo = SampleDataGenerator.GetContact(),
                IsActivated = true,
                Description = "Test invalid author"
            };

            // Act
            var newAuthorResult = await _authorManager.CreateAsync(author);

            // Assert
            Assert.False(newAuthorResult.Success);
            Assert.True(newAuthorResult.Messages.FirstOrDefault()?.Message.Contains("AccountName is longer then 256 characters"));
            Assert.Null(newAuthorResult.Value);
        }

        [Fact]
        public async Task Can_Update_Valid_Author()
        {
            // Arrange
            var author = new Author
            {
                AccountName = "jfang2",
                ContactInfo = SampleDataGenerator.GetContact(),
                Role = AuthorRoleType.Author,
                IsActivated = true,
                Description = "Test update author"
            };
            var result = await _authorManager.CreateAsync(author);
            Assert.True(result.Value.Id > 0, "user.Id is greater then 0");

            //   Act
            var savedAuthorsResult = await _authorManager.GetEntitiesAsync();
            var savedAuthor = savedAuthorsResult.Value.FirstOrDefault(a => a.Id == result.Value.Id);
            Assert.NotNull(savedAuthor);
            savedAuthor.Role = AuthorRoleType.Guest;
            var updatedAuthorResult = await _authorManager.UpdateAsync(savedAuthor);

            //  Assert
            Assert.NotNull(updatedAuthorResult.Value);
            Assert.Equal(AuthorRoleType.Guest, updatedAuthorResult.Value.Role);
            Assert.True(updatedAuthorResult.Success);
            Assert.Empty(updatedAuthorResult.Messages);
        }

        [Fact]
        public async Task Cannot_Update_InValid_Author()
        {
            //    Arrange
            var author = new Author
            {
                AccountName = "jfang2",
                ContactInfo = SampleDataGenerator.GetContact(),
                IsActivated = true,
                Description = "Sample author"
            };
            var result = await _authorManager.CreateAsync(author);
            Assert.True(result.Value.Id > 0, "newAuthor.Id is greater then 0");

            //   Act
            author.AccountName = "fchy";
            var newAuthorResult = await _authorManager.UpdateAsync(author);

            //  Assert
            Assert.False(newAuthorResult.Success);
            Assert.True(newAuthorResult.Messages.FirstOrDefault()?.Message.Contains("AccountName : Duplicated account name"));
            Assert.Null(newAuthorResult.Value);
        }

        [Fact]
        public async Task Cannot_Update_Not_Exists_Author()
        {
            // Arrange - no id
            var author = new Author
            {
                AccountName = "jfang2",
                ContactInfo = SampleDataGenerator.GetContact(),
                IsActivated = true,
                Description = "Sample author"
            };

            //   Act
            var newAuthorResult = await _authorManager.UpdateAsync(author);

            //  Assert
            Assert.False(newAuthorResult.Success);
            Assert.Null(newAuthorResult.Value);

            // Arrange - id not exist
            author = new Author
            {
                Id = 20000,
                AccountName = "jfang2",
                ContactInfo = SampleDataGenerator.GetContact(),
                IsActivated = true,
                Description = "Author with non exists id"
            };

            // Act
            newAuthorResult = await _authorManager.UpdateAsync(author);

            //  Assert
            Assert.False(newAuthorResult.Success);
            Assert.Null(newAuthorResult.Value);
        }

        [Fact]
        public async Task Can_Deactivate_Author()
        {
            // Arrange
            var authorsResult = await _authorManager.GetEntitiesAsync();
            var author = authorsResult.Value.FirstOrDefault();
            Assert.NotNull(author);
            Assert.True(author.IsActivated);

            // Act
            await _authorManager.DeActivateAsync(author.Id);
            var updatedAuthorResult = await _authorManager.GetAuthorByIdAsync(author.Id);

            // Assert
            Assert.True(updatedAuthorResult.Success);
            Assert.Empty(updatedAuthorResult.Messages);
            Assert.Null(updatedAuthorResult.Value);
        }

        [Fact]
        public async Task Can_Check_Author_Exists()
        {
            // Arrange
            var authorsResult = await _authorManager.GetEntitiesAsync();
            var author = authorsResult.Value.FirstOrDefault();
            Assert.NotNull(author);

            // Act
            var result = await _authorManager.IsAuthorExistsAsync(author.Id);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(0, false)]
        public async Task Check_Author_Exists_With_Invalid_Id_Get_False(int id, bool expectResult)
        {
            // Arrange
            // Act
            var result = await _authorManager.IsAuthorExistsAsync(id);

            // Act & Assert
            Assert.Equal(result, expectResult);
        }
    }
}