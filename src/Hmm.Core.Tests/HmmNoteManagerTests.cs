using FluentValidation;
using FluentValidation.Results;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.TestHelp;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hmm.Core.Tests
{
    public class HmmNoteManagerTests : TestFixtureBase
    {
        private IHmmNoteManager _manager;
        private Author _user;
        private FakeValidator _testValidator;

        public HmmNoteManagerTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void Can_Add_Note_To_DataSource()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _user,
                Subject = "Testing note",
                Content = "Test content"
            };

            // Act
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNote = _manager.Create(note);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(newNote);
            Assert.True(newNote.Id >= 1, "newNote.Id >=1");
            Assert.Equal("Testing note", newNote.Subject);
            Assert.Equal(newNote.CreateDate, newNote.LastModifiedDate);
            Assert.Equal(newNote.CreateDate, CurrentTime);
            Assert.False(note.IsDeleted);
        }

        [Fact]
        public void Can_Update_Note()
        {
            // Arrange - note with null content
            var note = new HmmNote
            {
                Author = _user,
                Subject = "Testing note",
                Content = "Testing content with < and >",
            };
            var crtTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var mdfTime = new DateTime(2021, 4, 4, 8, 30, 0);
            CurrentTime = crtTime;
            _manager.Create(note);
            var savedNote = _manager.GetNoteById(note.Id);
            Assert.Equal(savedNote.Version, note.Version);

            // Act
            CurrentTime = mdfTime;
            savedNote.Subject = "new note subject";
            savedNote.Content = "This is new note content";
            var updatedNote = _manager.Update(savedNote);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(updatedNote);
            Assert.Equal("new note subject", updatedNote.Subject);
            Assert.Equal(updatedNote.CreateDate, crtTime);
            Assert.Equal(updatedNote.LastModifiedDate, mdfTime);
            Assert.NotEqual(updatedNote.Version, note.Version);
            Assert.False(note.IsDeleted);
        }

        [Fact]
        public void Cannot_Update_Invalid_Note()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _user,
                Description = "testing note",
                Subject = "testing note is here",
                Content = "<root><time>2017-08-01</time></root>"
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
            _testValidator.GetInvalidResult = true;

            // Act
            savedRec = _manager.Update(note);

            // Assert
            Assert.False(_manager.ProcessResult.Success);
            Assert.Null(savedRec);
            Assert.False(note.IsDeleted);
        }

        [Fact]
        public void Can_Search_Note_By_Id()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _user,
                Subject = "Testing note",
                Content = "Test content"
            };
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNote = _manager.Create(note);

            // Act
            var savedNote = _manager.GetNoteById(newNote.Id);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedNote);
            Assert.Equal(savedNote.Subject, note.Subject);
            Assert.False(note.IsDeleted);
        }

        [Fact]
        public void Can_Delete_Note()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _user,
                Subject = "Testing note",
                Content = "Test content"
            };
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNote = _manager.Create(note);

            // Act
            var result = _manager.Delete(newNote.Id);
            var deleteNote = _manager.GetNoteById(newNote.Id, true);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.True(result);
            Assert.True(deleteNote.IsDeleted);
        }

        [Fact]
        public void Cannot_Get_Deleted_Note()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _user,
                Subject = "Testing note",
                Content = "Test content"
            };
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNote = _manager.Create(note);

            // Act
            var result = _manager.Delete(newNote.Id);
            var deleteNote = _manager.GetNoteById(newNote.Id);

            // Assert
            Assert.True(result);
            Assert.True(_manager.ProcessResult.Success);
            Assert.Null(deleteNote);
        }

        [Fact]
        public void Cannot_Delete_Not_Exists_Note()
        {
            // Arrange
            // Act
            var result = _manager.Delete(-10);

            // Assert
            Assert.False(_manager.ProcessResult.Success);
            Assert.False(result);
        }

        private class FakeValidator : NoteValidator
        {
            public FakeValidator(IVersionRepository<HmmNote> noteRepo) : base(noteRepo)
            {
            }

            public bool GetInvalidResult { get; set; }

            public override ValidationResult Validate(ValidationContext<HmmNote> context)
            {
                return GetInvalidResult
                    ? new ValidationResult(new List<ValidationFailure> { new("Author", "Author cannot change") })
                    : new ValidationResult();
            }
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _user = LookupRepo.GetEntities<Author>().FirstOrDefault();
            _testValidator = new FakeValidator(NoteRepository);
            _manager = new HmmNoteManager(NoteRepository, _testValidator, DateProvider);
        }
    }
}