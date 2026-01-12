using Hmm.Core.DefaultManager;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests
{
    public class NoteTagAssociationManagerTests : CoreTestFixtureBase, IAsyncLifetime
    {
        private readonly HmmNoteManager _noteManager;
        private readonly TagManager _tagManager;
        private readonly NoteTagAssociationManager _associationManager;
        private Author _author;
        private NoteCatalog _catalog;

        public NoteTagAssociationManagerTests()
        {
            var hmmValidator = new Mock<IHmmValidator<HmmNote>>();
            hmmValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<HmmNote>()))
                        .ReturnsAsync((HmmNote note) =>
                        {
                            return ProcessingResult<HmmNote>.Ok(note);
                        });
            var tagValidator = new Mock<IHmmValidator<Tag>>();
            tagValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<Tag>()))
                        .ReturnsAsync((Tag tag) =>
                        {
                            return ProcessingResult<Tag>.Ok(tag);
                        });
            _noteManager = new HmmNoteManager(NoteRepository, Mapper, LookupRepository, DateProvider, hmmValidator.Object);
            _tagManager = new TagManager(TagRepository, Mapper, LookupRepository, tagValidator.Object);
            _associationManager = new NoteTagAssociationManager(_noteManager, _tagManager);
        }

        #region GetNoteTagsAsync Tests

        [Fact]
        public async Task GetNoteTagsAsync_ReturnsEmptyList_WhenNoteHasNoTags()
        {
            // Arrange
            var note = await CreateTestNote("Note without tags");

            // Act
            var result = await _associationManager.GetNoteTagsAsync(note.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task GetNoteTagsAsync_ReturnsTags_WhenNoteHasTags()
        {
            // Arrange
            var note = await CreateTestNote("Note with tags");
            var tag1 = await CreateTestTag("Tag1");
            var tag2 = await CreateTestTag("Tag2");

            await _associationManager.ApplyTagToNoteAsync(note.Id, tag1);
            await _associationManager.ApplyTagToNoteAsync(note.Id, tag2);

            // Act
            var result = await _associationManager.GetNoteTagsAsync(note.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);
            Assert.Contains(result.Value, t => t.Name == "Tag1");
            Assert.Contains(result.Value, t => t.Name == "Tag2");
        }

        [Fact]
        public async Task GetNoteTagsAsync_ReturnsFail_WhenNoteNotFound()
        {
            // Arrange
            const int nonExistentNoteId = 99999;

            // Act
            var result = await _associationManager.GetNoteTagsAsync(nonExistentNoteId);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Value);
            Assert.Contains($"Cannot find note {nonExistentNoteId}", result.ErrorMessage);
        }

        #endregion GetNoteTagsAsync Tests

        #region ApplyTagToNoteAsync Tests

        [Fact]
        public async Task ApplyTagToNoteAsync_SuccessfullyAppliesTag_WhenTagExists()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag = await CreateTestTag("ExistingTag");

            // Act
            var result = await _associationManager.ApplyTagToNoteAsync(note.Id, tag);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Equal("ExistingTag", result.Value[0].Name);
        }

        [Fact]
        public async Task ApplyTagToNoteAsync_CreatesAndAppliesTag_WhenTagDoesNotExist()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var newTag = new Tag { Name = "NewTag", Description = "New tag description" };

            // Act
            var result = await _associationManager.ApplyTagToNoteAsync(note.Id, newTag);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Equal("NewTag", result.Value[0].Name);

            // Verify tag was created in repository
            var createdTagResult = await _tagManager.GetTagByNameAsync("NewTag");
            Assert.True(createdTagResult.Success);
            Assert.Equal("NewTag", createdTagResult.Value.Name);
        }

        [Fact]
        public async Task ApplyTagToNoteAsync_ReturnsFail_WhenNoteNotFound()
        {
            // Arrange
            const int nonExistentNoteId = 99999;
            var tag = new Tag { Name = "TestTag" };

            // Act
            var result = await _associationManager.ApplyTagToNoteAsync(nonExistentNoteId, tag);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Value);
            Assert.Contains($"Cannot find note {nonExistentNoteId}", result.ErrorMessage);
        }

        [Fact]
        public async Task ApplyTagToNoteAsync_ReturnsInvalid_WhenTagIsDeactivated()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag = await CreateTestTag("DeactivatedTag", isActivated: false);

            // Act
            var result = await _associationManager.ApplyTagToNoteAsync(note.Id, tag);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.Deleted, result.ErrorType);
            Assert.Contains("Cannot apply deactivated tag", result.ErrorMessage);
        }

        [Fact]
        public async Task ApplyTagToNoteAsync_ReturnsSuccess_WhenTagAlreadyApplied()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag = await CreateTestTag("TestTag");

            await _associationManager.ApplyTagToNoteAsync(note.Id, tag);

            // Act
            var result = await _associationManager.ApplyTagToNoteAsync(note.Id, tag);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Contains("already associated", result.Messages[0].Message);
        }

        [Fact]
        public async Task ApplyTagToNoteAsync_FindsTagById_WhenTagIdProvided()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var existingTag = await CreateTestTag("ExistingTag");
            var tagWithIdOnly = new Tag { Id = existingTag.Id, Name = existingTag.Name };

            // Act
            var result = await _associationManager.ApplyTagToNoteAsync(note.Id, tagWithIdOnly);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Equal("ExistingTag", result.Value[0].Name);
        }

        [Fact]
        public async Task ApplyTagToNoteAsync_FindsTagByName_WhenNameProvided()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            await CreateTestTag("ExistingTagByName");
            var tagWithNameOnly = new Tag { Name = "ExistingTagByName" };

            // Act
            var result = await _associationManager.ApplyTagToNoteAsync(note.Id, tagWithNameOnly);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value);
            Assert.Equal("ExistingTagByName", result.Value[0].Name);
        }

        #endregion ApplyTagToNoteAsync Tests

        #region RemoveTagFromNoteAsync Tests

        [Fact]
        public async Task RemoveTagFromNoteAsync_SuccessfullyRemovesTag_WhenTagIsApplied()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag = await CreateTestTag("TagToRemove");

            var applyResult = await _associationManager.ApplyTagToNoteAsync(note.Id, tag);
            Assert.True(applyResult.Success);

            // Act
            var result = await _associationManager.RemoveTagFromNoteAsync(note.Id, tag.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
        }

        [Fact]
        public async Task RemoveTagFromNoteAsync_ReturnsSuccess_WhenTagNotApplied()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag = await CreateTestTag("NotAppliedTag");

            // Act
            var result = await _associationManager.RemoveTagFromNoteAsync(note.Id, tag.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value);
            Assert.Contains("not associated", result.Messages[0].Message);
        }

        [Fact]
        public async Task RemoveTagFromNoteAsync_ReturnsFail_WhenNoteNotFound()
        {
            // Arrange
            const int nonExistentNoteId = 99999;
            const int tagId = 1;

            // Act
            var result = await _associationManager.RemoveTagFromNoteAsync(nonExistentNoteId, tagId);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Value);
            Assert.Contains($"Cannot find note {nonExistentNoteId}", result.ErrorMessage);
        }

        [Fact]
        public async Task RemoveTagFromNoteAsync_RemovesOnlySpecifiedTag_WhenMultipleTagsApplied()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag1 = await CreateTestTag("Tag1");
            var tag2 = await CreateTestTag("Tag2");
            var tag3 = await CreateTestTag("Tag3");

            await _associationManager.ApplyTagToNoteAsync(note.Id, tag1);
            await _associationManager.ApplyTagToNoteAsync(note.Id, tag2);
            await _associationManager.ApplyTagToNoteAsync(note.Id, tag3);

            // Act
            var result = await _associationManager.RemoveTagFromNoteAsync(note.Id, tag2.Id);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count);
            Assert.Contains(result.Value, t => t.Name == "Tag1");
            Assert.Contains(result.Value, t => t.Name == "Tag3");
            Assert.DoesNotContain(result.Value, t => t.Name == "Tag2");
        }

        #endregion RemoveTagFromNoteAsync Tests

        #region ApplyMultipleTagsAsync Tests

        [Fact]
        public async Task ApplyMultipleTagsAsync_SuccessfullyAppliesAllTags_WhenAllTagsValid()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag1 = await CreateTestTag("Tag1");
            var tag2 = await CreateTestTag("Tag2");
            var tag3 = new Tag { Name = "Tag3", Description = "New tag" };

            var tags = new[] { tag1, tag2, tag3 };

            // Act
            var result = await _associationManager.ApplyMultipleTagsAsync(note.Id, tags);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(3, result.Value.Count);
            Assert.Contains(result.Value, t => t.Name == "Tag1");
            Assert.Contains(result.Value, t => t.Name == "Tag2");
            Assert.Contains(result.Value, t => t.Name == "Tag3");
        }

        [Fact]
        public async Task ApplyMultipleTagsAsync_ReturnsInvalid_WhenTagsCollectionIsNull()
        {
            // Arrange
            var note = await CreateTestNote("Test note");

            // Act
            var result = await _associationManager.ApplyMultipleTagsAsync(note.Id, null);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
            Assert.Contains("cannot be null or empty", result.ErrorMessage);
        }

        [Fact]
        public async Task ApplyMultipleTagsAsync_ReturnsInvalid_WhenTagsCollectionIsEmpty()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var emptyTags = Array.Empty<Tag>();

            // Act
            var result = await _associationManager.ApplyMultipleTagsAsync(note.Id, emptyTags);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
            Assert.Contains("cannot be null or empty", result.ErrorMessage);
        }

        [Fact]
        public async Task ApplyMultipleTagsAsync_SkipsDuplicateTags_WhenSameTagProvidedMultipleTimes()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag1 = await CreateTestTag("Tag1");
            var tag2 = await CreateTestTag("Tag2");

            var tags = new[] { tag1, tag2, tag1, tag2 }; // Duplicates

            // Act
            var result = await _associationManager.ApplyMultipleTagsAsync(note.Id, tags);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count); // Only 2 unique tags
        }

        [Fact]
        public async Task ApplyMultipleTagsAsync_SkipsAlreadyAppliedTags_WhenTagsAlreadyAssociated()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag1 = await CreateTestTag("Tag1");
            var tag2 = await CreateTestTag("Tag2");
            var tag3 = await CreateTestTag("Tag3");

            // Apply tag1 first
            await _associationManager.ApplyTagToNoteAsync(note.Id, tag1);

            var tags = new[] { tag1, tag2, tag3 }; // tag1 already applied

            // Act
            var result = await _associationManager.ApplyMultipleTagsAsync(note.Id, tags);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(3, result.Value.Count); // All 3 tags
            Assert.Contains("Successfully applied 2 tags", result.Messages[0].Message); // Only 2 new tags applied
        }

        [Fact]
        public async Task ApplyMultipleTagsAsync_SkipsDeactivatedTags_AndReturnsWarning()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag1 = await CreateTestTag("Tag1");
            var tag2 = await CreateTestTag("Tag2", isActivated: false); // Deactivated
            var tag3 = await CreateTestTag("Tag3");

            var tags = new[] { tag1, tag2, tag3 };

            // Act
            var result = await _associationManager.ApplyMultipleTagsAsync(note.Id, tags);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal(2, result.Value.Count); // Only activated tags
            Assert.Contains(result.Value, t => t.Name == "Tag1");
            Assert.Contains(result.Value, t => t.Name == "Tag3");
            Assert.DoesNotContain(result.Value, t => t.Name == "Tag2");
            Assert.Contains("deactivated", result.Messages[0].Message);
        }

        [Fact]
        public async Task ApplyMultipleTagsAsync_ReturnsFail_WhenNoteNotFound()
        {
            // Arrange
            const int nonExistentNoteId = 99999;
            var tags = new[] { new Tag { Name = "Tag1" } };

            // Act
            var result = await _associationManager.ApplyMultipleTagsAsync(nonExistentNoteId, tags);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Value);
            Assert.Contains($"Cannot find note {nonExistentNoteId}", result.ErrorMessage);
        }

        [Fact]
        public async Task ApplyMultipleTagsAsync_PerformsSingleUpdate_WhenApplyingMultipleTags()
        {
            // Arrange
            var note = await CreateTestNote("Test note");
            var tag1 = new Tag { Name = "Tag1" };
            var tag2 = new Tag { Name = "Tag2" };
            var tag3 = new Tag { Name = "Tag3" };

            var tags = new[] { tag1, tag2, tag3 };

            // Act
            var result = await _associationManager.ApplyMultipleTagsAsync(note.Id, tags);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(3, result.Value.Count);

            // Verify all tags were applied in one operation
            var noteTagsResult = await _associationManager.GetNoteTagsAsync(note.Id);
            Assert.True(noteTagsResult.Success);
            Assert.Equal(3, noteTagsResult.Value.Count);
        }

        #endregion ApplyMultipleTagsAsync Tests

        #region Helper Methods

        private async Task<HmmNote> CreateTestNote(string subject)
        {
            var note = new HmmNote
            {
                Author = _author,
                Catalog = _catalog,
                Subject = subject,
                Content = $"Content for {subject}",
                Description = $"Description for {subject}"
            };

            CurrentTime = DateTime.UtcNow;
            var result = await _noteManager.CreateAsync(note);
            Assert.True(result.Success);
            return result.Value;
        }

        private async Task<Tag> CreateTestTag(string name, bool isActivated = true)
        {
            var tag = new Tag
            {
                Name = name,
                Description = $"Description for {name}",
                IsActivated = isActivated
            };

            var result = await _tagManager.CreateAsync(tag);
            Assert.True(result.Success);
            return result.Value;
        }

        #endregion Helper Methods

        #region Test Life-cycle

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

        #endregion Test Life-cycle
    }
}