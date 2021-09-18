using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using System;
using System.Linq;
using Xunit;

namespace Hmm.Core.Tests
{
    public class HmmNoteValidatorTests : TestFixtureBase
    {
        private IHmmNoteManager _manager;
        private Author _user;
        private NoteValidator _validator;

        public HmmNoteValidatorTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void CannotChangeAuthorForExistsNote()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _user,
                Description = "testing note",
                Subject = "testing note is here",
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = new NoteCatalog()
            };
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            _manager.Create(note);

            Assert.True(_manager.ProcessResult.Success);
            var savedRec = NoteRepository.GetEntities().FirstOrDefault();
            Assert.NotNull(savedRec);
            Assert.Equal("jfang", savedRec.Author.AccountName);

            // change the note render
            var newUser = LookupRepo.GetEntities<Author>().FirstOrDefault(u => u.AccountName != "jfang");
            Assert.NotNull(newUser);
            note.Author = newUser;

            // Act
            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SubjectMustHasValidContentLength()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _user,
                Description = "testing note",
                Subject = "",
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = new NoteCatalog()
            };

            // Act
            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.False(result);

            // Arrange
            note = new HmmNote
            {
                Author = _user,
                Description = "testing note",
                Subject = GetRandomString(1001),
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = new NoteCatalog()
            };

            // Act
            processResult = new ProcessingResult();
            result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.False(result);

            // Arrange
            note = new HmmNote
            {
                Author = _user,
                Description = "testing note",
                Subject = GetRandomString(15),
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = new NoteCatalog()
            };

            // Act
            processResult = new ProcessingResult();
            result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DescriptionMustHasValidContentLength()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _user,
                Description = "",
                Subject = "test note",
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = new NoteCatalog()
            };

            // Act
            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.False(result);

            // Arrange
            note = new HmmNote
            {
                Author = _user,
                Description = GetRandomString(1001),
                Subject = "testing note",
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = new NoteCatalog()
            };

            // Act
            processResult = new ProcessingResult();
            result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.False(result);

            // Arrange
            note = new HmmNote
            {
                Author = _user,
                Description = GetRandomString(100),
                Subject = "testing note",
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = new NoteCatalog()
            };

            // Act
            processResult = new ProcessingResult();
            result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void NoteCatalogMustValid()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _user,
                Description = "testing note",
                Subject = "testing note is here",
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = null
            };

            // Act
            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(note, processResult);

            // Assert
            Assert.False(result);
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _user = LookupRepo.GetEntities<Author>().FirstOrDefault();
            _validator = new NoteValidator(NoteRepository);
            _manager = new HmmNoteManager(NoteRepository, _validator, DateProvider);
        }
    }
}