using Hmm.Core.Map.DbEntity;
using Hmm.Utility.TestHelp;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    public class TagRepositoryTests : DbTestFixtureBase, IAsyncLifetime
    {
        [Fact]
        public async Task Can_Add_Tag_To_DataSource()
        {
            // Arrange
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag",
            };

            // Act
            var result = await TagRepository.AddAsync(tag);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id > 0, "savedRec.Id > 0");
            Assert.True(tag.Id == result.Value.Id, "tag.Id == savedRec.Id");
        }

        [Fact]
        public async Task CanNot_Add_Already_Existed_Tag_To_DataSource()
        {
            // Arrange
            var firstResult = await TagRepository.AddAsync(new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag",
            });
            Assert.True(firstResult.Success);

            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag",
            };

            // Act
            var result = await TagRepository.AddAsync(tag);

            // Assert
            Assert.False(result.Success);
            Assert.True(tag.Id <= 0, "tag.Id <=0");
            Assert.True(result.Messages.Count > 0);
        }

        [Fact]
        public async Task Can_Delete_Tag_From_DataSource()
        {
            // Arrange
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag"
            };

            var addResult = await TagRepository.AddAsync(tag);
            Assert.True(addResult.Success);

            // Act
            var result = await TagRepository.DeleteAsync(tag);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task Cannot_Delete_NonExists_Tag_From_DataSource()
        {
            // Arrange
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag"
            };

            var addResult = await TagRepository.AddAsync(tag);
            Assert.True(addResult.Success);

            var tag2 = new TagDao
            {
                Name = "GasLog2",
                IsActivated = true,
                Description = "testing tag"
            };

            // Act
            var result = await TagRepository.DeleteAsync(tag2);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Messages.Count > 0);
        }

        [Fact]
        public async Task Can_Update_Tag()
        {
            // Arrange - update name
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag"
            };

            var addResult = await TagRepository.AddAsync(tag);
            Assert.True(addResult.Success);
            Assert.NotNull(addResult.Value);

            tag.Name = "GasLog2";

            // Act
            var result = await TagRepository.UpdateAsync(tag);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("GasLog2", result.Value.Name);

            // Arrange - update description
            tag.Description = "new testing tag";

            // Act
            result = await TagRepository.UpdateAsync(tag);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("new testing tag", result.Value.Description);
        }

        [Fact]
        public async Task Cannot_Update_Tag_For_NonExists_Tag()
        {
            // Arrange
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag"
            };

            var addResult = await TagRepository.AddAsync(tag);
            Assert.True(addResult.Success);

            var tag2 = new TagDao
            {
                Name = "GasLog2",
                IsActivated = true,
                Description = "testing tag"
            };

            // Act
            var result = await TagRepository.UpdateAsync(tag2);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Messages.Count > 0);
        }

        [Fact]
        public async Task Cannot_Update_Tag_With_Duplicated_Name()
        {
            // Arrange
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag"
            };
            var addResult1 = await TagRepository.AddAsync(tag);
            Assert.True(addResult1.Success);

            var tag2 = new TagDao
            {
                Name = "GasLog2",
                IsActivated = true,
                Description = "testing tag2"
            };
            var addResult2 = await TagRepository.AddAsync(tag2);
            Assert.True(addResult2.Success);

            tag.Name = tag2.Name;

            // Act
            var result = await TagRepository.UpdateAsync(tag);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Messages.Count > 0);
        }

        [Theory]
        [InlineData("TestTag1", 1)]
        [InlineData("TestTag1,TestTag2", 2)]
        public async Task Can_Apply_Multiply_TagToNote(string tags, int expectTagNumber)
        {
            // Arrange
            await SetTestingData();
            var noteListResult = await NoteRepository.GetEntitiesAsync();
            Assert.True(noteListResult.Success);
            var note = noteListResult.Value.FirstOrDefault();
            Assert.NotNull(note);
            var tagNameList = tags.Split(",").ToList();
            var savedTagsResult = await TagRepository.GetEntitiesAsync();
            Assert.True(savedTagsResult.Success);
            var tagList = savedTagsResult.Value.Where(tag => tagNameList.Contains(tag.Name)).ToList();

            foreach (var tag in tagList)
            {
                // Act
                note.Tags.Add(new NoteTagRefDao { Note = note, Tag = tag });
                var updateResult = await NoteRepository.UpdateAsync(note);
                Assert.True(updateResult.Success);
            }
            var savedNoteResult = await NoteRepository.GetEntityAsync(note.Id);
            Assert.True(savedNoteResult.Success);
            var savedNote = savedNoteResult.Value;

            // Assert
            Assert.True(savedNote.Tags.ToList().Count == expectTagNumber);
            Assert.All(savedNote.Tags, tagRef => Assert.Contains(tagRef.Tag.Name, tagNameList));
        }

        [Theory]
        [InlineData("TestNote1", 1)]
        [InlineData("TestNote1,TestNote2", 2)]
        public async Task Can_Apply_One_TagTo_Multiple_Notes(string notes, int expectTagNumber)
        {
            // Arrange
            await SetTestingData();
            var tagListResult = await TagRepository.GetEntitiesAsync();
            Assert.True(tagListResult.Success);
            var tag = tagListResult.Value.FirstOrDefault();
            Assert.NotNull(tag);
            var noteSubjectList = notes.Split(",").ToList();
            var savedNotesResult = await NoteRepository.GetEntitiesAsync();
            Assert.True(savedNotesResult.Success);
            var noteList = savedNotesResult.Value.Where(n => noteSubjectList.Contains(n.Subject)).ToList();
            foreach (var note in noteList)
            {
                // Act
                tag.Notes.Add(new NoteTagRefDao { Note = note, NoteId = note.Id, Tag = tag, TagId = tag.Id });
                var updateResult = await TagRepository.UpdateAsync(tag);
                Assert.True(updateResult.Success);
            }

            var tagNotesResult = await TagRepository.GetNoteByTagAsync(tag.Id);
            Assert.True(tagNotesResult.Success);

            // Assert
            foreach (var note in noteList)
            {
                var savedNoteResult = await NoteRepository.GetEntityAsync(note.Id);
                Assert.True(savedNoteResult.Success);
                var savedNote = savedNoteResult.Value;
                Assert.Single(savedNote.Tags);
                Assert.Equal(tag.Name, savedNote.Tags.FirstOrDefault()!.Tag.Name);
            }

            Assert.Equal(expectTagNumber, tagNotesResult.Value.Count);
        }

        [Fact]
        public async Task Can_Delete_Note_Associated_Tag()
        {
            // Arrange
            await SetTestingData();
            var tagsResult = await TagRepository.GetEntitiesAsync();
            Assert.True(tagsResult.Success);
            var tag = tagsResult.Value.FirstOrDefault();
            Assert.NotNull(tag);
            var tagId = tag.Id;
            var notesResult = await NoteRepository.GetEntitiesAsync();
            Assert.True(notesResult.Success);
            var note = notesResult.Value.FirstOrDefault();
            Assert.NotNull(note);

            note.Tags.Add(new NoteTagRefDao { Note = note, Tag = tag });
            var savedNoteResult = await NoteRepository.UpdateAsync(note);
            Assert.True(savedNoteResult.Success);
            Assert.NotNull(savedNoteResult.Value);
            Assert.Single(savedNoteResult.Value.Tags);

            // Act
            var deleteResult = await TagRepository.DeleteAsync(tag);
            Assert.True(deleteResult.Success);
            var deleteTagResult = await TagRepository.GetEntityAsync(tagId);
            var finalNoteResult = await NoteRepository.GetEntityAsync(note.Id);

            // Assert
            Assert.False(deleteTagResult.Success);
            Assert.True(finalNoteResult.Success);
            Assert.Empty(finalNoteResult.Value.Tags);
        }

        private async Task SetTestingData()
        {
            // setup tag table
            var tag = new TagDao
            {
                Name = "TestTag1",
                Description = "The tag should show in reference table",
                IsActivated = true,
            };
            var newTagResult = await TagRepository.AddAsync(tag);
            Assert.True(newTagResult.Success);
            Assert.NotNull(newTagResult.Value);

            tag = new TagDao
            {
                Name = "TestTag2",
                Description = "The tag number 2 should show in reference table",
                IsActivated = true,
            };
            newTagResult = await TagRepository.AddAsync(tag);
            Assert.True(newTagResult.Success);
            Assert.NotNull(newTagResult.Value);

            // setup note table
            var catalog = new NoteCatalogDao
            {
                Name = "DefaultCatalog",
                FormatType = NoteContentFormatType.PlainText,
                Schema = "",
                IsDefault = false,
                Description = "Description"
            };
            var catalogResult = await CatalogRepository.AddAsync(catalog);
            Assert.True(catalogResult.Success);
            Assert.NotNull(catalogResult.Value);

            var contact = SampleDataGenerator.GetContactDao();
            var contactResult = await ContactRepository.AddAsync(contact);
            Assert.True(contactResult.Success);
            Assert.NotNull(contactResult.Value);

            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = contactResult.Value,
                Description = "testing user",
                IsActivated = true
            };
            var authorResult = await AuthorRepository.AddAsync(author);
            Assert.True(authorResult.Success);
            Assert.NotNull(authorResult.Value);

            var note = new HmmNoteDao
            {
                Subject = "TestNote1",
                Content = "This is first testing note, please check the ts for version control",
                CreateDate = DateProvider.UtcNow,
                LastModifiedDate = DateProvider.UtcNow,
                Author = authorResult.Value,
                Catalog = catalogResult.Value,
                Description = "Testing note1"
            };
            var noteResult1 = await NoteRepository.AddAsync(note);
            Assert.True(noteResult1.Success);
            Assert.NotNull(noteResult1.Value);

            note = new HmmNoteDao
            {
                Subject = "TestNote2",
                Content = "This is second testing note, please check the ts for version control",
                CreateDate = DateProvider.UtcNow,
                LastModifiedDate = DateProvider.UtcNow,
                Author = authorResult.Value,
                Catalog = catalogResult.Value,
                Description = "Testing note2"
            };
            var noteResult2 = await NoteRepository.AddAsync(note);
            Assert.True(noteResult2.Success);
            Assert.NotNull(noteResult2.Value);
        }

        public async Task InitializeAsync()
        {
            Transaction = await ((DbContext)DbContext).Database.BeginTransactionAsync();
        }

        public async Task DisposeAsync()
        {
            await Transaction.RollbackAsync();
            await Transaction.DisposeAsync();
        }
    }
}