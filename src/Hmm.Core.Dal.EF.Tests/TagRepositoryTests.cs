using Hmm.Core.Map.DbEntity;
using Hmm.Utility.TestHelp;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    public class TagRepositoryTests : DbTestFixtureBase, IAsyncLifetime
    {
        [Fact]
        public void Can_Add_Tag_To_DataSource()
        {
            // Arrange
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag",
            };

            // Act
            var savedRec = TagRepository.Add(tag);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id > 0, "savedRec.Id > 0");
            Assert.True(tag.Id == savedRec.Id, "tag.Id == savedRec.Id");
            Assert.True(TagRepository.ProcessMessage.Success);
        }

        [Fact]
        public void CanNot_Add_Already_Existed_Tag_To_DataSource()
        {
            // Arrange
            TagRepository.Add(new TagDao
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
            var savedRec = TagRepository.Add(tag);

            // Assert
            Assert.Null(savedRec);
            Assert.True(tag.Id <= 0, "tag.Id <=0");
            Assert.False(TagRepository.ProcessMessage.Success);
            Assert.Single(TagRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Can_Delete_Tag_From_DataSource()
        {
            // Arrange
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag"
            };

            TagRepository.Add(tag);

            // Act
            var result = TagRepository.Delete(tag);

            // Assert
            Assert.True(result);
            Assert.True(TagRepository.ProcessMessage.Success);
        }

        [Fact]
        public void Cannot_Delete_NonExists_Tag_From_DataSource()
        {
            // Arrange
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag"
            };

            TagRepository.Add(tag);

            var tag2 = new TagDao
            {
                Name = "GasLog2",
                IsActivated = true,
                Description = "testing tag"
            };

            // Act
            var result = TagRepository.Delete(tag2);

            // Assert
            Assert.False(result);
            Assert.False(TagRepository.ProcessMessage.Success);
            Assert.Single(TagRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Can_Update_Tag()
        {
            // Arrange - update name
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag"
            };

            var savedTag = TagRepository.Add(tag);
            Assert.NotNull(savedTag);

            tag.Name = "GasLog2";

            // Act
            var result = TagRepository.Update(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("GasLog2", result.Name);

            // Arrange - update description
            tag.Description = "new testing tag";

            // Act
            result = TagRepository.Update(tag);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new testing tag", result.Description);
        }

        [Fact]
        public void Cannot_Update_Tag_For_NonExists_Tag()
        {
            // Arrange
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag"
            };

            TagRepository.Add(tag);

            var tag2 = new TagDao
            {
                Name = "GasLog2",
                IsActivated = true,
                Description = "testing tag"
            };

            // Act
            var result = TagRepository.Update(tag2);

            // Assert
            Assert.Null(result);
            Assert.False(TagRepository.ProcessMessage.Success);
            Assert.Single(TagRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Cannot_Update_Tag_With_Duplicated_Name()
        {
            // Arrange
            var tag = new TagDao
            {
                Name = "GasLog",
                IsActivated = true,
                Description = "testing tag"
            };
            TagRepository.Add(tag);

            var tag2 = new TagDao
            {
                Name = "GasLog2",
                IsActivated = true,
                Description = "testing tag2"
            };
            TagRepository.Add(tag2);

            tag.Name = tag2.Name;

            // Act
            var result = TagRepository.Update(tag);

            // Assert
            Assert.Null(result);
            Assert.False(TagRepository.ProcessMessage.Success);
            Assert.Single(TagRepository.ProcessMessage.MessageList);
        }

        [Theory]
        [InlineData("TestTag1", 1)]
        [InlineData("TestTag1,TestTag2", 2)]
        public void Can_Apply_Multiply_TagToNote(string tags, int expectTagNumber)
        {
            // Arrange
            SetTestingData();
            var note = NoteRepository.GetEntities().FirstOrDefault();
            Assert.NotNull(note);
            var tagNameList = tags.Split(",").ToList();
            var tagList = new List<TagDao>();
            foreach (var tag in tagNameList.Select(tagName => TagRepository.GetEntities(t => t.Name == tagName).FirstOrDefault()))
            {
                Assert.NotNull(tag);
                tagList.Add(tag);
            }

            foreach (var tag in tagList)
            {
                // Act
                note.Tags.ToList().Add(new NoteTagRefDao { Note = note, Tag = tag });
                NoteRepository.Update(note);
            }
            var savedNote = NoteRepository.GetEntity(note.Id);

            // Assert
            Assert.True(savedNote.Tags.ToList().Count == expectTagNumber);
            Assert.All(savedNote.Tags, tagRef => Assert.Contains(tagRef.Tag.Name, tagNameList));
        }

        [Theory]
        [InlineData("TestNote1", 1)]
        [InlineData("TestNote1,TestNote2", 2)]
        public void Can_Apply_One_TagTo_Multiple_Notes(string notes, int expectTagNumber)
        {
            // Arrange
            SetTestingData();
            var tag = TagRepository.GetEntities().FirstOrDefault();
            Assert.NotNull(tag);
            var noteSubjectList = notes.Split(",").ToList();
            var noteList = new List<HmmNoteDao>();
            foreach (var note in noteSubjectList.Select(subject => NoteRepository.GetEntities(n => n.Subject == subject).FirstOrDefault()))
            {
                Assert.NotNull(note);
                noteList.Add(note);
            }

            foreach (var note in noteList)
            {
                // Act
                note.Tags.ToList().Add(new NoteTagRefDao { Note = note, Tag = tag });
                NoteRepository.Update(note);
            }
            var savedTag = TagRepository.GetEntity(tag.Id);

            // Assert
            Assert.True(savedTag.Notes.ToList().Count == expectTagNumber);
            Assert.All(savedTag.Notes, tagRef => Assert.Contains(tagRef.Note.Subject, notes));
        }

        [Fact]
        public void Can_Delete_Note_Associated_Tag()
        {
            // Arrange
            SetTestingData();
            var tag = TagRepository.GetEntities().FirstOrDefault();
            Assert.NotNull(tag);
            var tagId = tag.Id;
            var note = NoteRepository.GetEntities().FirstOrDefault();
            Assert.NotNull(note);

            note.Tags.ToList().Add(new NoteTagRefDao { Note = note, Tag = tag });
            var savedNote = NoteRepository.Update(note);
            Assert.NotNull(savedNote);
            Assert.Single(savedNote.Tags);

            // Act
            TagRepository.Delete(tag);
            var deleteTag = TagRepository.GetEntity(tagId);

            // Assert
            Assert.Null(deleteTag);
            Assert.Empty(savedNote.Tags);
        }

        private void SetTestingData()
        {
            // setup tag table
            var tag = new TagDao
            {
                Name = "TestTag1",
                Description = "The tag should show in reference table",
                IsActivated = true,
            };
            var newTag = TagRepository.Add(tag);
            Assert.NotNull(newTag);

            tag = new TagDao
            {
                Name = "TestTag2",
                Description = "The tag number 2 should show in reference table",
                IsActivated = true,
            };
            newTag = TagRepository.Add(tag);
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
            var savedCatalog = CatalogRepository.Add(catalog);

            var contact = GetTestingContact();
            var savedContact = ContactRepository.Add(contact);
            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = savedContact,
                Description = "testing user",
                IsActivated = true
            };
            var savedUser = AuthorRepository.Add(author);

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
            NoteRepository.Add(note);

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
            NoteRepository.Add(note);
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