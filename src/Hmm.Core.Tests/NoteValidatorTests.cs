using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using System.Linq;
using Xunit;

namespace Hmm.Core.Tests
{
    public class NoteValidatorTests : TestFixtureBase
    {
        private NoteValidator _validator;
        private Author _author;
        private NoteCatalog _catalog;

        public NoteValidatorTests()
        {
            SetupTestEnv();
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(15, true)]
        [InlineData(1002, false)]
        public void Note_Must_Has_Valid_Subject_Length(int nameLen, bool expected)
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
            var result = _validator.IsValidEntity(note, processResult);

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
        public void Note_Must_Has_Valid_Description_Length(int namespaceLen, bool expected)
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
            var result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
            }
        }

        [Fact]
        public void Note_Must_Has_Valid_Author()
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
            var result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.True(result);

            // Arrange
            note = new HmmNote
            {
                Subject = "Test name",
                Content = "Test NameSpace",
                Catalog = _catalog
            };

            // Act

            processResult = new ProcessingResult();
            result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Note_Must_Has_Valid_Catalog()
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
            var result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.True(result);

            // Arrange
            note = new HmmNote
            {
                Author = _author,
                Subject = "Test name",
                Content = "Test NameSpace"
            };

            // Act

            processResult = new ProcessingResult();
            result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.False(result);
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new NoteValidator(NoteRepository);
            _author = AuthorRepository.GetEntities().FirstOrDefault();
            _catalog = CatalogRepository.GetEntities().FirstOrDefault();
            Assert.NotNull(_author);
            Assert.NotNull(_catalog);
        }
    }
}