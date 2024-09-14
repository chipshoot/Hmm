using System;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Hmm.Core.DefaultManager;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
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

            var processResult = new ProcessingResult();
            var result = await _validator.IsValidEntityAsync(note, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
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
            var processResult = new ProcessingResult();
            var result = await _validator.IsValidEntityAsync(note, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
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

            var processResult = new ProcessingResult();
            var result = await _validator.IsValidEntityAsync(note, processResult);

            // Assert
            Assert.True(result);

            // Arrange - author is null
            note = new HmmNote
            {
                Subject = "Test name",
                Content = "Test NameSpace",
                Catalog = _catalog
            };

            // Act

            processResult = new ProcessingResult();
            result = await _validator.IsValidEntityAsync(note, processResult);

            // Assert
            Assert.False(result);

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

            processResult = new ProcessingResult();
            result = await _validator.IsValidEntityAsync(note, processResult);

            // Assert
            Assert.False(result);
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

            var processResult = new ProcessingResult();
            var result = await _validator.IsValidEntityAsync(note, processResult);

            // Assert
            Assert.True(result);

            // Arrange - catalog is null
            note = new HmmNote
            {
                Author = _author,
                Subject = "Test name",
                Content = "Test NameSpace"
            };

            // Act

            processResult = new ProcessingResult();
            result = await _validator.IsValidEntityAsync(note, processResult);

            // Assert
            Assert.False(result);

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

            processResult = new ProcessingResult();
            result = await _validator.IsValidEntityAsync(note, processResult);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CannotChangeAuthorForExistsNote()
        {
            // Arrange
            var tagManager = new TagManager(TagRepository, Mapper, LookupRepository);
            var manager = new HmmNoteManager(NoteRepository, Mapper, tagManager, LookupRepository, DateProvider);
            var note = new HmmNote
            {
                Author = _author,
                Description = "testing note",
                Subject = "testing note is here",
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = _catalog
            };

            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNote = await manager.CreateAsync(note);

            Assert.True(manager.ProcessResult.Success);
            var savedRec = await LookupRepository.GetEntityAsync<HmmNoteDao>(newNote.Id);
            Assert.NotNull(savedRec);
            Assert.Equal("fchy", savedRec.Author.AccountName);

            // change the note author
            var newAuthor = await GetTestAuthor("amyWang");
            Assert.NotNull(newAuthor);
            newNote.Author = newAuthor;

            // Act
            var processResult = new ProcessingResult();
            var result = await _validator.IsValidEntityAsync(note, processResult);

            // Assert
            Assert.False(result);
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