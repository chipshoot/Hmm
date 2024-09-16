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
            var savedRec = await TagRepository.AddAsync(tag);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id > 0, "savedRec.Id > 0");
            Assert.True(tag.Id == savedRec.Id, "tag.Id == savedRec.Id");
            Assert.True(TagRepository.ProcessMessage.Success);
        }

        [Fact]
        public async Task CanNot_Add_Already_Existed_Tag_To_DataSource()
        {
            // Arrange
            await TagRepository.AddAsync(new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag",
            });

            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag",
            };

            // Act
            var savedRec = await TagRepository.AddAsync(tag);

            // Assert
            Assert.Null(savedRec);
            Assert.True(tag.Id <= 0, "tag.Id <=0");
            Assert.False(TagRepository.ProcessMessage.Success);
            Assert.Single(TagRepository.ProcessMessage.MessageList);
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

            await TagRepository.AddAsync(tag);

            // Act
            var result = await TagRepository.DeleteAsync(tag);

            // Assert
            Assert.True(result);
            Assert.True(TagRepository.ProcessMessage.Success);
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

            await TagRepository.AddAsync(tag);

            var tag2 = new TagDao
            {
                Name = "GasLog2",
                IsActivated = true,
                Description = "testing tag"
            };

            // Act
            var result = await TagRepository.DeleteAsync(tag2);

            // Assert
            Assert.False(result);
            Assert.False(TagRepository.ProcessMessage.Success);
            Assert.Single(TagRepository.ProcessMessage.MessageList);
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

            var savedTag = await TagRepository.AddAsync(tag);
            Assert.NotNull(savedTag);

            tag.Name = "GasLog2";

            // Act
            var result = await TagRepository.UpdateAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("GasLog2", result.Name);

            // Arrange - update description
            tag.Description = "new testing tag";

            // Act
            result = await TagRepository.UpdateAsync(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new testing tag", result.Description);
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

            await TagRepository.AddAsync(tag);

            var tag2 = new TagDao
            {
                Name = "GasLog2",
                IsActivated = true,
                Description = "testing tag"
            };

            // Act
            var result = await TagRepository.UpdateAsync(tag2);

            // Assert
            Assert.Null(result);
            Assert.False(TagRepository.ProcessMessage.Success);
            Assert.Single(TagRepository.ProcessMessage.MessageList);
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
            await TagRepository.AddAsync(tag);

            var tag2 = new TagDao
            {
                Name = "GasLog2",
                IsActivated = true,
                Description = "testing tag2"
            };
            await TagRepository.AddAsync(tag2);

            tag.Name = tag2.Name;

            // Act
            var result = await TagRepository.UpdateAsync(tag);

            // Assert
            Assert.Null(result);
            Assert.False(TagRepository.ProcessMessage.Success);
            Assert.Single(TagRepository.ProcessMessage.MessageList);
        }

        [Theory]
        [InlineData("TestTag1", 1)]
        [InlineData("TestTag1,TestTag2", 2)]
        public async Task Can_Apply_Multiply_TagToNote(string tags, int expectTagNumber)
        {
            // Arrange
            await SetTestingData();
            var noteList = await NoteRepository.GetEntitiesAsync();
            var note = noteList.FirstOrDefault();
            Assert.NotNull(note);
            var tagNameList = tags.Split(",").ToList();
            var savedTags = await TagRepository.GetEntitiesAsync();
            var tagList = savedTags.Where(tag => tagNameList.Contains(tag.Name)).ToList();

            foreach (var tag in tagList)
            {
                // Act
                note.Tags.Add(new NoteTagRefDao { Note = note, Tag = tag });
                await NoteRepository.UpdateAsync(note);
            }
            var savedNote = await NoteRepository.GetEntityAsync(note.Id);

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
            var tagList = await TagRepository.GetEntitiesAsync();
            var tag = tagList.FirstOrDefault();
            Assert.NotNull(tag);
            var noteSubjectList = notes.Split(",").ToList();
            var savedNotes = await NoteRepository.GetEntitiesAsync();
            var noteList = savedNotes.Where(n => noteSubjectList.Contains(n.Subject)).ToList();
            foreach (var note in noteList)
            {
                // Act
                note.Tags.Add(new NoteTagRefDao { Note = note, Tag = tag });
                await NoteRepository.UpdateAsync(note);
            }

            var tagNotes = await TagRepository.GetNoteByTagAsync(tag.Id);

            // Assert
            foreach (var note in noteList)
            {
                var savedNote = await NoteRepository.GetEntityAsync(note.Id);
                Assert.Single(savedNote.Tags);
                Assert.Equal(tag.Name, savedNote.Tags.FirstOrDefault()!.Tag.Name);
            }

            Assert.Equal(expectTagNumber, tagNotes.Count);
        }

        [Fact]
        public async Task Can_Delete_Note_Associated_Tag()
        {
            // Arrange
            await SetTestingData();
            var tags = await TagRepository.GetEntitiesAsync();
            var tag = tags.FirstOrDefault();
            Assert.NotNull(tag);
            var tagId = tag.Id;
            var notes = await NoteRepository.GetEntitiesAsync();
            var note = notes.FirstOrDefault();
            Assert.NotNull(note);

            note.Tags.Add(new NoteTagRefDao { Note = note, Tag = tag });
            var savedNote = await NoteRepository.UpdateAsync(note);
            Assert.NotNull(savedNote);
            Assert.Single(savedNote.Tags);

            // Act
            await TagRepository.DeleteAsync(tag);
            var deleteTag = await TagRepository.GetEntityAsync(tagId);
            savedNote = await NoteRepository.GetEntityAsync(note.Id);

            // Assert
            Assert.Null(deleteTag);
            Assert.Empty(savedNote.Tags);
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
            var newTag = await TagRepository.AddAsync(tag);
            Assert.NotNull(newTag);

            tag = new TagDao
            {
                Name = "TestTag2",
                Description = "The tag number 2 should show in reference table",
                IsActivated = true,
            };
            newTag = await TagRepository.AddAsync(tag);
            Assert.NotNull(newTag);

            // setup note table
            var catalog = new NoteCatalogDao
            {
                Name = "DefaultCatalog",
                FormatType = NoteContentFormatType.PlainText,
                Schema = "",
                IsDefault = false,
                Description = "Description"
            };
            var savedCatalog = await CatalogRepository.AddAsync(catalog);

            var contact = SampleDataGenerator.GetContactDao();
            var savedContact = await ContactRepository.AddAsync(contact);
            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = savedContact,
                Description = "testing user",
                IsActivated = true
            };
            var savedUser = await AuthorRepository.AddAsync(author);

            var note = new HmmNoteDao
            {
                Subject = "TestNote1",
                Content = "This is first testing note, please check the ts for version control",
                CreateDate = DateProvider.UtcNow,
                LastModifiedDate = DateProvider.UtcNow,
                Author = savedUser,
                Catalog = savedCatalog,
                Description = "Testing note1"
            };
            await NoteRepository.AddAsync(note);

            note = new HmmNoteDao
            {
                Subject = "TestNote2",
                Content = "This is second testing note, please check the ts for version control",
                CreateDate = DateProvider.UtcNow,
                LastModifiedDate = DateProvider.UtcNow,
                Author = savedUser,
                Catalog = savedCatalog,
                Description = "Testing note2"
            };
            await NoteRepository.AddAsync(note);
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