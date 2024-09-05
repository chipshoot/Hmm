using Hmm.Core.DefaultManager;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.TestHelp;
using System;
using System.Collections.Generic;
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
        private readonly TagManager _tagManager;

        public HmmNoteManagerTests()
        {
            _tagManager = new TagManager(TagRepository, Mapper, LookupRepository);
            _noteManager = new HmmNoteManager(NoteRepository, Mapper, _tagManager, DateProvider);
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
            var newNote = await _noteManager.CreateAsync(note);

            // Assert
            Assert.True(_noteManager.ProcessResult.Success);
            Assert.NotNull(newNote);
            Assert.True(newNote.Id >= 1, "newNote.Id >=1");
            Assert.Equal("Testing note", newNote.Subject);
            Assert.Equal(newNote.CreateDate, newNote.LastModifiedDate);
            Assert.Equal(newNote.CreateDate, CurrentTime);
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
            await _noteManager.CreateAsync(note);
            var savedNote = await _noteManager.GetNoteByIdAsync(note.Id);
            Assert.Equal(savedNote.Version, note.Version);

            // Act
            CurrentTime = mdfTime;
            savedNote.Subject = "new note subject";
            savedNote.Content = "This is new note content";
            var updatedNote = await _noteManager.UpdateAsync(savedNote);

            // Assert
            Assert.True(_noteManager.ProcessResult.Success);
            Assert.NotNull(updatedNote);
            Assert.Equal("new note subject", updatedNote.Subject);
            Assert.Equal(updatedNote.CreateDate, crtTime);
            Assert.Equal(updatedNote.LastModifiedDate, mdfTime);
            Assert.NotEqual(updatedNote.Version, note.Version);
            Assert.False(note.IsDeleted);
        }

        [Fact]
        public async Task Cannot_Update_Invalid_Note()
        {
            // Arrange
            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Description = "testing note",
                Subject = "testing note is here",
                Content = "<root><time>2017-08-01</time></root>"
            };
            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            await _noteManager.CreateAsync(note);

            Assert.True(_noteManager.ProcessResult.Success);
            var savedDaos = await NoteRepository.GetEntitiesAsync();
            var savedDao = savedDaos.FirstOrDefault();
            Assert.NotNull(savedDao);
            Assert.Equal("fchy", savedDao.Author.AccountName);

            // change the note author
            var newUser = await GetTestAuthor("jfang");
            Assert.NotNull(newUser);
            note.Author = newUser;

            // Act
            var savedNote = await _noteManager.UpdateAsync(note);

            // Assert
            Assert.False(_noteManager.ProcessResult.Success);
            Assert.Null(savedNote);
            Assert.Equal("Author : Cannot update note's author", _noteManager.ProcessResult.MessageList.First().Message);
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
            var newNote = await _noteManager.CreateAsync(note);

            // Act
            var savedNote = await _noteManager.GetNoteByIdAsync(newNote.Id);

            // Assert
            Assert.True(_noteManager.ProcessResult.Success);
            Assert.NotNull(savedNote);
            Assert.Equal(savedNote.Subject, note.Subject);
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
            var newNote = await _noteManager.CreateAsync(note);

            // Act
            var result = await _noteManager.DeleteAsync(newNote.Id);
            var deleteNote = await _noteManager.GetNoteByIdAsync(newNote.Id, true);

            // Assert
            Assert.True(_noteManager.ProcessResult.Success);
            Assert.True(result);
            Assert.True(deleteNote.IsDeleted);
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
            var newNote = await _noteManager.CreateAsync(note);

            // Act
            var result = await _noteManager.DeleteAsync(newNote.Id);
            var deleteNote = await _noteManager.GetNoteByIdAsync(newNote.Id);

            // Assert
            Assert.True(result);
            Assert.True(_noteManager.ProcessResult.Success);
            Assert.Null(deleteNote);
        }

        [Fact]
        public async Task Cannot_Delete_Not_Exists_Note()
        {
            // Arrange
            // Act
            var result = await _noteManager.DeleteAsync(-10);

            // Assert
            Assert.False(_noteManager.ProcessResult.Success);
            Assert.False(result);
        }

        [Theory]
        [InlineData(new[] { 101 }, 1)]
        [InlineData(new[] { 100, 102 }, 2)]
        [InlineData(new[] { 100, 100 }, 1)]
        public async Task Can_Add_Tag_To_Note(int[] tagIds, int expectTags)
        {
            // Arrange
            var tagsToApply = new List<Tag>();
            foreach (var tagId in tagIds)
            {
                var tagToApply = await _tagManager.GetTagByIdAsync(tagId);
                tagsToApply.Add(tagToApply);
            }

            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = "Testing note",
                Content = "Test content",
                Description = "Test note with tag applied"
            };
            var newNote = await _noteManager.CreateAsync(note);
            Assert.NotNull(newNote);

            // Act
            var tags = new List<Tag>();
            foreach (var tag in tagsToApply)
            {
                tags = await _noteManager.ApplyTag(note, tag);
            }

            // Assert
            Assert.Equal(expectTags, tags.Count);
        }

        [Fact]
        public async Task Cannot_Apply_Deactivated_Tag_To_Note()
        {
            var tagList = await _tagManager.GetEntitiesAsync();
            var tag = tagList.FirstOrDefault();
            Assert.NotNull(tag);
            await _tagManager.DeActivateAsync(tag.Id);
            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = "Testing note",
                Content = "Test content",
                Description = "Test note with tag applied"
            };
            var newNote = await _noteManager.CreateAsync(note);
            Assert.NotNull(newNote);

            // Act
            var tags = await _noteManager.ApplyTag(note, tag);

            // Assert
            Assert.Null(tags);
            Assert.False(_noteManager.ProcessResult.Success);
        }

        [Fact]
        public async Task Can_Apply_And_Create_New_Tag_To_Note()
        {
            var tagList = await _tagManager.GetEntitiesAsync();
            var tagNum = tagList.Count;
            var tag = new Tag
            {
                Name = "NonExistsTag"
            };
            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = "Testing note",
                Content = "Test content",
                Description = "Test note with tag applied"
            };
            var newNote = await _noteManager.CreateAsync(note);
            Assert.NotNull(newNote);

            // Act
            var tags = await _noteManager.ApplyTag(note, tag);
            var savedTags = await _tagManager.GetEntitiesAsync(t => t.Name == "NonExistsTag");
            var savedTag = savedTags.FirstOrDefault();
            tagList = await _tagManager.GetEntitiesAsync();

            // Assert
            Assert.Single(tags);
            Assert.NotNull(savedTag);
            Assert.Equal(tagNum + 1, tagList.Count);
        }

        [Theory]
        [InlineData(new[] { 101 }, new[] { 101 }, 0)]
        [InlineData(new[] { 100, 102 }, new[] { 102 }, 1)]
        [InlineData(new[] { 100, 100 }, new[] { 100 }, 0)]
        [InlineData(new[] { 100 }, new[] { 102 }, 1)]
        public async Task Can_Remove_Tag_From_Note(int[] tagIds, int[] tagIdsToDelete, int expectTags)
        {
            // Arrange
            var tagsToApply = new List<Tag>();
            foreach (var tagId in tagIds)
            {
                var tagToApply = await _tagManager.GetTagByIdAsync(tagId);
                tagsToApply.Add(tagToApply);
            }
            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = "Testing note",
                Content = "Test content",
                Description = "Test note with tag applied"
            };
            var newNote = await _noteManager.CreateAsync(note);
            Assert.NotNull(newNote);

            var tags = new List<Tag>();
            foreach (var tag in tagsToApply)
            {
                tags = await _noteManager.ApplyTag(note, tag);
            }

            // Act
            foreach (var id in tagIdsToDelete)
            {
                tags = await _noteManager.RemoveTag(note, id);
            }

            // Assert
            Assert.Equal(expectTags, tags.Count);
        }

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