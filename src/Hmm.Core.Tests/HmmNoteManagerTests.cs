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
            _noteManager = new HmmNoteManager(NoteRepository, Mapper, _tagManager, LookupRepository, DateProvider);
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
            await _noteManager.CreateAsync(note);
            var savedNoteResult = await _noteManager.GetNoteByIdAsync(note.Id);
            Assert.Equal(savedNoteResult.Value.Version, note.Version);

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
            Assert.NotEqual(updatedNoteResult.Value.Version, note.Version);
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
            var result = await _noteManager.CreateAsync(note);

            Assert.True(result.Success);
            var savedDaosResult = await NoteRepository.GetEntitiesAsync();
            var savedDao = savedDaosResult.Value.FirstOrDefault();
            Assert.NotNull(savedDao);
            Assert.Equal("fchy", savedDao.Author.AccountName);

            // change the note author
            var newUser = await GetTestAuthor("jfang");
            Assert.NotNull(newUser);
            note.Author = newUser;

            // Act
            var savedNoteResult = await _noteManager.UpdateAsync(note);

            // Assert
            Assert.False(savedNoteResult.Success);
            Assert.Null(savedNoteResult.Value);
            Assert.Equal("Author : Cannot update note's author", savedNoteResult.Messages.First().Message);
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
            Assert.True(deleteNoteResult.Success);
            Assert.Null(deleteNoteResult.Value);
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
                var tagToApplyResult = await _tagManager.GetTagByIdAsync(tagId);
                tagsToApply.Add(tagToApplyResult.Value);
            }

            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = "Testing note",
                Content = "Test content",
                Description = "Test note with tag applied"
            };
            var newNoteResult = await _noteManager.CreateAsync(note);
            Assert.NotNull(newNoteResult);

            // Act
            var tags = new List<Tag>();
            foreach (var tag in tagsToApply)
            {
                var tagsResult = await _noteManager.ApplyTag(note, tag);
                tags = tagsResult.Value;
            }

            // Assert
            Assert.Equal(expectTags, tags.Count);
        }

        [Fact]
        public async Task Cannot_Apply_Deactivated_Tag_To_Note()
        {
            var tagListResult = await _tagManager.GetEntitiesAsync();
            var tag = tagListResult.Value.FirstOrDefault();
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
            var newNoteResult = await _noteManager.CreateAsync(note);
            Assert.NotNull(newNoteResult.Value);

            // Act
            var tagsResult = await _noteManager.ApplyTag(note, tag);

            // Assert
            Assert.Null(tagsResult.Value);
            Assert.False(tagsResult.Success);
        }

        [Fact]
        public async Task Can_Apply_And_Create_New_Tag_To_Note()
        {
            var tagListResult = await _tagManager.GetEntitiesAsync();
            var tagNum = tagListResult.Value.Count;
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
            var newNoteResult = await _noteManager.CreateAsync(note);
            Assert.NotNull(newNoteResult.Value);

            // Act
            var tagsResult = await _noteManager.ApplyTag(note, tag);
            var tags = tagsResult.Value;
            var savedTagsResult = await _tagManager.GetEntitiesAsync(t => t.Name == "NonExistsTag");
            var savedTag = savedTagsResult.Value.FirstOrDefault();
            tagListResult = await _tagManager.GetEntitiesAsync();

            // Assert
            Assert.Single(tags);
            Assert.NotNull(savedTag);
            Assert.Equal(tagNum + 1, tagListResult.Value.Count);
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
                var tagToApplyResult = await _tagManager.GetTagByIdAsync(tagId);
                tagsToApply.Add(tagToApplyResult.Value);
            }
            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = "Testing note",
                Content = "Test content",
                Description = "Test note with tag applied"
            };
            var newNoteResult = await _noteManager.CreateAsync(note);
            Assert.NotNull(newNoteResult.Value);

            var tags = new List<Tag>();
            foreach (var tag in tagsToApply)
            {
                var tagsResult = await _noteManager.ApplyTag(note, tag);
                tags.AddRange(tagsResult.Value);
            }

            // Act
            foreach (var id in tagIdsToDelete)
            {
                var tagsResult = await _noteManager.RemoveTag(note, id);
                tags = tagsResult.Value;
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