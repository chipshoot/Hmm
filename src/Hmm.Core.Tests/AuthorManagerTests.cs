using Hmm.Core.DefaultManager;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests
{
    public class AuthorManagerTests : CoreTestFixtureBase
    {
        private readonly AuthorManager _authorManager;
        private readonly Mock<IHmmValidator<Author>> _mockValidator;

        public AuthorManagerTests()
        {
            _mockValidator = new Mock<IHmmValidator<Author>>();
            _mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Author>()))
                .ReturnsAsync(ProcessingResult<Author>.Ok(It.IsAny<Author>()));
            _authorManager = new AuthorManager(AuthorRepository, UnitOfWork, Mapper, LookupRepository, _mockValidator.Object);
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
        public async Task GetEntitiesAsync_Should_Not_Return_Deactivated_Authors()
        {
            // Arrange
            var authorsResult = await _authorManager.GetEntitiesAsync();
            var originalCount = authorsResult.Value.Count;
            var author = authorsResult.Value.First();
            await _authorManager.DeActivateAsync(author.Id);

            // Act
            var currentAuthorsResult = await _authorManager.GetEntitiesAsync();

            // Assert
            Assert.True(currentAuthorsResult.Success);
            Assert.Equal(originalCount - 1, currentAuthorsResult.Value.Count);
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
        public async Task GetAuthorByIdAsync_Should_Return_NotFound_For_Nonexistent_Id()
        {
            // Arrange
            var nonExistentId = 9999;

            // Act
            var result = await _authorManager.GetAuthorByIdAsync(nonExistentId);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
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
                AccountName = "test",
                Description = "Test invalid author"
            };
            var validationMessage = "Author is not valid.";
            _mockValidator.Setup(v => v.ValidateEntityAsync(author))
                .ReturnsAsync(ProcessingResult<Author>.Invalid(validationMessage));

            // Act
            var newAuthorResult = await _authorManager.CreateAsync(author);

            // Assert
            Assert.False(newAuthorResult.Success);
            Assert.Equal(ErrorCategory.ValidationError, newAuthorResult.ErrorType);
            Assert.Contains(validationMessage, newAuthorResult.GetWholeMessage());
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
        public async Task Cannot_Update_Author_With_Duplicate_AccountName()
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
            Assert.Null(newAuthorResult.Value);
        }

        [Fact]
        public async Task Cannot_Update_Invalid_Author()
        {
            // Arrange
            var author = new Author { Id = 1, AccountName = "test" };
            var validationMessage = "Author is not valid.";
            _mockValidator.Setup(v => v.ValidateEntityAsync(author))
                .ReturnsAsync(ProcessingResult<Author>.Invalid((validationMessage)));

            // Act
            var result = await _authorManager.UpdateAsync(author);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
            Assert.Contains(validationMessage, result.GetWholeMessage());
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
            Assert.Equal(ErrorCategory.NotFound, newAuthorResult.ErrorType);
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
            var deactivateResult = await _authorManager.DeActivateAsync(author.Id);
            var updatedAuthorResult = await _authorManager.GetAuthorByIdAsync(author.Id);

            // Assert
            Assert.True(deactivateResult.Success);
            Assert.False(updatedAuthorResult.Success);
            Assert.Equal(ErrorCategory.Deleted, updatedAuthorResult.ErrorType);
            Assert.Null(updatedAuthorResult.Value);
        }

        [Fact]
        public async Task DeActivateAsync_Should_Do_Nothing_If_Author_Is_Already_Deactivated()
        {
            // Arrange
            var authorsResult = await _authorManager.GetEntitiesAsync();
            var author = authorsResult.Value.First();
            await _authorManager.DeActivateAsync(author.Id);

            // Act
            var result = await _authorManager.DeActivateAsync(author.Id);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(ErrorCategory.None, result.ErrorType);
        }

        [Fact]
        public async Task DeActivateAsync_Should_Return_NotFound_For_Nonexistent_Id()
        {
            // Arrange
            var nonExistentId = 9999;

            // Act
            var result = await _authorManager.DeActivateAsync(nonExistentId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.NotFound, result.ErrorType);
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
        [InlineData(9999, false)]
        public async Task Check_Author_Exists_With_Invalid_Id_Get_False(int id, bool expectResult)
        {
            // Arrange
            // Act
            var result = await _authorManager.IsAuthorExistsAsync(id);

            // Act & Assert
            Assert.Equal(expectResult, result);
        }
    }
}