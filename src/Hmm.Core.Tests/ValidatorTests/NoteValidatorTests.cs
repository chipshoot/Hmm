using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests.ValidatorTests
{
    public class NoteValidatorTests : CoreTestFixtureBase, IAsyncLifetime
    {
        private NoteValidator _validator;
        private Author _author;
        private NoteCatalog _catalog;

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(15, true)]
        [InlineData(1002, false)]
        public async Task Note_Must_Has_Valid_Subject_Length(int nameLen, bool expected)
        {
            // Arrange
            var note = new HmmNote()
            {
                Author = _author,
                Subject = GetRandomString(nameLen),
                Content = "Default NameSpace",
                Catalog = _catalog
            };

            // Act
            var result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.Equal(expected, result.Success);
            if (!expected)
            {
                Assert.NotEmpty(result.Messages[0].Message);
            }
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(0, true)]
        [InlineData(15, true)]
        [InlineData(1001, false)]
        public async Task Note_Must_Has_Valid_Description_Length(int namespaceLen, bool expected)
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _author,
                Subject = "Test name",
                Content = "Test Content",
                Description = GetRandomString(namespaceLen),
                Catalog = _catalog
            };

            // Act
            var result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.Equal(expected, result.Success);
            if (!expected)
            {
                Assert.NotEmpty(result.Messages[0].Message);
            }
        }

        [Fact]
        public async Task Note_Must_Has_Valid_Author()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _author,
                Subject = "Test name",
                Content = "Test NameSpace",
                Catalog = _catalog
            };

            // Act

            var result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.True(result.Success);

            // Arrange - author is null
            note = new HmmNote
            {
                Subject = "Test name",
                Content = "Test NameSpace",
                Catalog = _catalog
            };

            // Act

            result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.False(result.Success);

            // Arrange - author does not exist
            var author = SampleDataGenerator.GetAuthor("NotExistsAccount");
            note = new HmmNote
            {
                Author = author,
                Subject = "Test name",
                Content = "Test NameSpace",
                Catalog = _catalog
            };

            // Act

            result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Note_Must_Has_Valid_Catalog()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _author,
                Subject = "Test name",
                Content = "Test NameSpace",
                Catalog = _catalog
            };

            // Act

            var result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.True(result.Success);

            // Arrange - catalog is null
            note = new HmmNote
            {
                Author = _author,
                Subject = "Test name",
                Content = "Test NameSpace"
            };

            // Act
            result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.False(result.Success);

            // Arrange - catalog does not exits
            var catalog = SampleDataGenerator.GetCatalog("NotExistsCatalog");
            note = new HmmNote
            {
                Author = _author,
                Subject = "Test name",
                Content = "Test NameSpace",
                Catalog = catalog
            };

            // Act
            result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task CannotChangeAuthorForExistsNote()
        {
            // Arrange
            var manager = new HmmNoteManager(NoteRepository, UnitOfWork, Mapper, LookupRepository, DateProvider, Mock.Of<IHmmValidator<HmmNote>>());
            var note = new HmmNote
            {
                Author = _author,
                Description = "testing note",
                Subject = "testing note is here",
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = _catalog
            };

            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNoteResult = await manager.CreateAsync(note);

            Assert.True(newNoteResult.Success);
            var savedRecResult = await LookupRepository.GetEntityAsync<HmmNoteDao>(newNoteResult.Value.Id);
            Assert.NotNull(savedRecResult.Value);
            Assert.Equal("fchy", savedRecResult.Value.Author.AccountName);

            // change the note author
            var newAuthor = await GetTestAuthor("amyWang");
            Assert.NotNull(newAuthor);
            newNoteResult.Value.Author = newAuthor;

            // Act
            var result = await _validator.ValidateEntityAsync(newNoteResult.Value);
            // Assert
            Assert.False(result.Success);
        }

        public async Task InitializeAsync()
        {
            _author = await GetTestAuthor();
            _validator = new NoteValidator(LookupRepository);
            _catalog = await GetTestCatalog();
        }

        public async Task DisposeAsync()
        {
            await Task.CompletedTask;
        }
    }
}