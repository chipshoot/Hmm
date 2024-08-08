using AutoMapper;
using Hmm.Core.DefaultManager;
using Hmm.Core.Map;
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
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<HmmMappingProfile>();
            });
            var mapper = config.CreateMapper();
            _authorManager = new AuthorManager(AuthorRepository, mapper);
        }

        [Fact]
        public async Task Can_Get_Author()
        {
            // Act
            var authors = await _authorManager.GetEntitiesAsync();

            // Assert
            Assert.True(_authorManager.ProcessResult.Success);
            Assert.True(authors.Count >= 1, "authors.Count >= 1");
        }

        [Fact]
        public async Task Can_Get_Author_With_Query()
        {
            // Act
            var authors = await _authorManager.GetEntitiesAsync(a=>a.AccountName=="fchy");

            // Assert
            Assert.True(_authorManager.ProcessResult.Success);
            Assert.Single(authors);
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

            // Act
            var newAuthor = await _authorManager.CreateAsync(author);

            // Assert
            Assert.True(_authorManager.ProcessResult.Success);
            Assert.NotNull(newAuthor);
            Assert.True(newAuthor.Id >= 0, "newAuthor.Id is greater to 0");
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
            var newAuthor = await _authorManager.CreateAsync(author);

            // Assert
            Assert.False(_authorManager.ProcessResult.Success);
            Assert.True(_authorManager.ProcessResult.MessageList.FirstOrDefault()?.Message.Contains("AccountName is longer then 256 characters"));
            Assert.Null(newAuthor);
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
            Assert.True(author.Id > 0, "user.Id is greater then 0");

            //   Act
            var savedAuthors = await _authorManager.GetEntitiesAsync();
            var savedAuthor = savedAuthors.FirstOrDefault(a => a.Id == result.Id);
            Assert.NotNull(savedAuthor);
            savedAuthor.Role = AuthorRoleType.Guest;
            var updatedAuthor = await _authorManager.UpdateAsync(savedAuthor);

            //  Assert
            Assert.NotNull(updatedAuthor);
            Assert.Equal(AuthorRoleType.Guest, updatedAuthor.Role);
            Assert.True(_authorManager.ProcessResult.Success);
            Assert.Empty(_authorManager.ProcessResult.MessageList);
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
            await _authorManager.CreateAsync(author);
            Assert.True(author.Id > 0, "newAuthor.Id is greater then 0");

            //   Act
            author.AccountName = "fchy";
            var newAuthor = await _authorManager.UpdateAsync(author);

            //  Assert
            Assert.False(_authorManager.ProcessResult.Success);
            Assert.True(_authorManager.ProcessResult.MessageList.FirstOrDefault()?.Message.Contains("AccountName : Duplicated account name"));
            Assert.Null(newAuthor);
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
            var newAuthor = await _authorManager.UpdateAsync(author);

            //  Assert
            Assert.False(_authorManager.ProcessResult.Success);
            Assert.Null(newAuthor);

            // Arrange - id not exist
            _authorManager.ProcessResult.Rest();
            author = new Author
            {
                Id = 20000,
                AccountName = "jfang2",
                ContactInfo = SampleDataGenerator.GetContact(),
                IsActivated = true,
                Description = "Author with non exists id"
            };

            // Act
            newAuthor = await _authorManager.UpdateAsync(author);

            //  Assert
            Assert.False(_authorManager.ProcessResult.Success);
            Assert.Null(newAuthor);
        }

        [Fact]
        public async Task Can_Deactivate_Author()
        {
            // Arrange
            var authors =await _authorManager.GetEntitiesAsync();
            var author = authors.FirstOrDefault();
            Assert.NotNull(author);
            Assert.True(author.IsActivated);

            // Act
            await _authorManager.DeActivateAsync(author.Id);
            var updatedAuthor = await _authorManager.GetAuthorByIdAsync(author.Id);

            // Assert
            Assert.True(_authorManager.ProcessResult.Success);
            Assert.Empty(_authorManager.ProcessResult.MessageList);
            Assert.False(updatedAuthor.IsActivated);
        }

        [Fact]
        public async Task Can_Check_Author_Exists()
        {
            // Arrange
            var authors = await _authorManager.GetEntitiesAsync();
            var author = authors.FirstOrDefault();
            Assert.NotNull(author);

            // Act
            var result =await _authorManager.AuthorExistsAsync(author.Id);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(0, false)]
        public async Task Check_Author_Exists_With_Invalid_Id_Get_False(int id, bool expectResult)
        {
            // Arrange
            // Act
            var result = await _authorManager.AuthorExistsAsync(id);

            // Act & Assert
            Assert.Equal(result, expectResult);
        }
    }
}