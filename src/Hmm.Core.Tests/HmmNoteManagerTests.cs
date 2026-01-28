using Hmm.Core.DefaultManager;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests
{
    public class HmmNoteManagerTests : CoreTestFixtureBase, IAsyncLifetime
    {
        private readonly HmmNoteManager _noteManager;
        private Author _author;
        private NoteCatalog _catalog;

        public HmmNoteManagerTests()
        {
            var mockValidator = new Mock<IHmmValidator<HmmNote>>();
            mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<HmmNote>()))
                .ReturnsAsync((HmmNote note) => ProcessingResult<HmmNote>.Ok(note));
            _noteManager = new HmmNoteManager(NoteRepository, UnitOfWork, Mapper, LookupRepository, DateProvider, mockValidator.Object);
        }

        [Fact]
        public async Task Can_Add_Note_To_DataSource()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _author,
                Subject = "Testing note",
                Content = "Test content",
                Catalog = _catalog
            };

            // Act
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNoteResult = await _noteManager.CreateAsync(note);

            // Assert
            Assert.True(newNoteResult.Success);
            Assert.NotNull(newNoteResult.Value);
            Assert.True(newNoteResult.Value.Id >= 1, "newNote.Id >=1");
            Assert.Equal("Testing note", newNoteResult.Value.Subject);
            Assert.Equal(newNoteResult.Value.CreateDate, newNoteResult.Value.LastModifiedDate);
            Assert.Equal(newNoteResult.Value.CreateDate, CurrentTime);
            Assert.False(note.IsDeleted);
        }

        [Fact]
        public async Task Can_Update_Note()
        {
            // Arrange - note with null content
            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = "Testing note",
                Content = "Testing content with < and >",
                Description = "Testing description"
            };
            var crtTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var mdfTime = new DateTime(2021, 4, 4, 8, 30, 0);
            CurrentTime = crtTime;
            var createdNoteResult = await _noteManager.CreateAsync(note);
            Assert.True(createdNoteResult.Success);
            var savedNoteResult = await _noteManager.GetNoteByIdAsync(createdNoteResult.Value.Id);
            Assert.True(savedNoteResult.Success);
            Assert.Equal(savedNoteResult.Value.Version, createdNoteResult.Value.Version);

            // Act
            CurrentTime = mdfTime;
            savedNoteResult.Value.Subject = "new note subject";
            savedNoteResult.Value.Content = "This is new note content";
            var updatedNoteResult = await _noteManager.UpdateAsync(savedNoteResult.Value);

            // Assert
            Assert.True(updatedNoteResult.Success);
            Assert.NotNull(updatedNoteResult.Value);
            Assert.Equal("new note subject", updatedNoteResult.Value.Subject);
            Assert.Equal(updatedNoteResult.Value.CreateDate, crtTime);
            Assert.Equal(updatedNoteResult.Value.LastModifiedDate, mdfTime);
            Assert.NotEqual(updatedNoteResult.Value.Version, createdNoteResult.Value.Version);
            Assert.False(createdNoteResult.Value.IsDeleted);
        }

        [Fact]
        public async Task Cannot_Update_Invalid_Note()
        {
            // Arrange - use a validator that returns error when author changes
            var mockValidator = new Mock<IHmmValidator<HmmNote>>();
            mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<HmmNote>()))
                .ReturnsAsync((HmmNote note) =>
                {
                    // Simulate validation error when updating existing note with different author
                    if (note.Id > 0 && note.Author.AccountName != "fchy")
                    {
                        return ProcessingResult<HmmNote>.Invalid("Author : Cannot update note's author");
                    }
                    return ProcessingResult<HmmNote>.Ok(note);
                });
            var noteManagerWithValidation = new HmmNoteManager(NoteRepository, UnitOfWork, Mapper, LookupRepository, DateProvider, mockValidator.Object);

            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Description = "testing note",
                Subject = "testing note is here",
                Content = "<root><time>2017-08-01</time></root>"
            };
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var createdResult = await noteManagerWithValidation.CreateAsync(note);

            Assert.True(createdResult.Success);
            var savedDaosResult = await NoteRepository.GetEntitiesAsync();
            var savedDao = savedDaosResult.Value.FirstOrDefault();
            Assert.NotNull(savedDao);
            Assert.Equal("fchy", savedDao.Author.AccountName);

            // change the note author
            var newUser = await GetTestAuthor("jfang");
            Assert.NotNull(newUser);
            var noteToUpdate = createdResult.Value;
            noteToUpdate.Author = newUser;

            // Act
            var savedNoteResult = await noteManagerWithValidation.UpdateAsync(noteToUpdate);

            // Assert
            Assert.False(savedNoteResult.Success);
            Assert.Null(savedNoteResult.Value);
            Assert.Contains("Cannot update note's author", savedNoteResult.Messages[0].Message);
        }

        [Fact]
        public async Task Can_Search_Note_By_Id()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = "Testing note",
                Content = "Test content"
            };
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNoteResult = await _noteManager.CreateAsync(note);

            // Act
            var savedNoteResult = await _noteManager.GetNoteByIdAsync(newNoteResult.Value.Id);

            // Assert
            Assert.True(savedNoteResult.Success);
            Assert.NotNull(savedNoteResult.Value);
            Assert.Equal(savedNoteResult.Value.Subject, note.Subject);
            Assert.False(note.IsDeleted);
        }

        [Fact]
        public async Task Can_Delete_Note()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = "Testing note",
                Content = "Test content"
            };
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNoteResult = await _noteManager.CreateAsync(note);

            // Act
            var result = await _noteManager.DeleteAsync(newNoteResult.Value.Id);
            var deleteNoteResult = await _noteManager.GetNoteByIdAsync(newNoteResult.Value.Id, true);

            // Assert
            Assert.True(deleteNoteResult.Success);
            Assert.True(result.Success);
            Assert.True(deleteNoteResult.Value.IsDeleted);
        }

        [Fact]
        public async Task Cannot_Get_Deleted_Note()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = "Testing note",
                Content = "Test content"
            };
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNoteResult = await _noteManager.CreateAsync(note);

            // Act
            var result = await _noteManager.DeleteAsync(newNoteResult.Value.Id);
            var deleteNoteResult = await _noteManager.GetNoteByIdAsync(newNoteResult.Value.Id);

            // Assert
            Assert.True(result);
            Assert.False(deleteNoteResult.Success); // Getting deleted note without includeDelete returns error
            Assert.Null(deleteNoteResult.Value);
            Assert.Equal(ErrorCategory.Deleted, deleteNoteResult.ErrorType);
        }

        [Fact]
        public async Task Cannot_Delete_Not_Exists_Note()
        {
            // Arrange
            // Act
            var result = await _noteManager.DeleteAsync(-10);

            // Assert
            Assert.False(result.Success);
            Assert.False(result);
        }

        // NOTE: Tag-related tests have been moved to NoteTagAssociationManagerTests
        // The ApplyTag and RemoveTag methods are now part of INoteTagAssociationManager

        // [Theory]
        // [InlineData(new[] { 101 }, 1)]
        // [InlineData(new[] { 100, 102 }, 2)]
        // [InlineData(new[] { 100, 100 }, 1)]
        // public async Task Can_Add_Tag_To_Note(int[] tagIds, int expectTags)
        // - Moved to NoteTagAssociationManagerTests

        // [Fact]
        // public async Task Cannot_Apply_Deactivated_Tag_To_Note()
        // - Moved to NoteTagAssociationManagerTests

        // [Fact]
        // public async Task Can_Apply_And_Create_New_Tag_To_Note()
        // - Moved to NoteTagAssociationManagerTests

        // [Theory]
        // [InlineData(new[] { 101 }, new[] { 101 }, 0)]
        // [InlineData(new[] { 100, 102 }, new[] { 102 }, 1)]
        // [InlineData(new[] { 100, 100 }, new[] { 100 }, 0)]
        // [InlineData(new[] { 100 }, new[] { 102 }, 1)]
        // public async Task Can_Remove_Tag_From_Note(int[] tagIds, int[] tagIdsToDelete, int expectTags)
        // - Moved to NoteTagAssociationManagerTests

        public async Task InitializeAsync()
        {
            _author = await GetTestAuthor();
            Assert.NotNull(_author);

            _catalog = await GetTestCatalog();
            Assert.NotNull(_catalog);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}