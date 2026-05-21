using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Vault;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests.ValidatorTests
{
    public class NoteValidatorTests : CoreTestFixtureBase, IAsyncLifetime
    {
        private NoteValidator _validator;
        private Author _author;
        private NoteCatalog _catalog;

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(15, true)]
        [InlineData(1002, false)]
        public async Task Note_Must_Has_Valid_Subject_Length(int nameLen, bool expected)
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
            var result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.Equal(expected, result.Success);
            if (!expected)
            {
                Assert.NotEmpty(result.Messages[0].Message);
            }
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(0, true)]
        [InlineData(15, true)]
        [InlineData(1001, false)]
        public async Task Note_Must_Has_Valid_Description_Length(int namespaceLen, bool expected)
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
            var result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.Equal(expected, result.Success);
            if (!expected)
            {
                Assert.NotEmpty(result.Messages[0].Message);
            }
        }

        [Fact]
        public async Task Note_Must_Has_Valid_Author()
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

            var result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.True(result.Success);

            // Arrange - author is null
            note = new HmmNote
            {
                Subject = "Test name",
                Content = "Test NameSpace",
                Catalog = _catalog
            };

            // Act

            result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.False(result.Success);

            // Arrange - author does not exist
            var author = SampleDataGenerator.GetAuthor("NotExistsAccount");
            note = new HmmNote
            {
                Author = author,
                Subject = "Test name",
                Content = "Test NameSpace",
                Catalog = _catalog
            };

            // Act

            result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Note_Must_Has_Valid_Catalog()
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

            var result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.True(result.Success);

            // Arrange - catalog is null
            note = new HmmNote
            {
                Author = _author,
                Subject = "Test name",
                Content = "Test NameSpace"
            };

            // Act
            result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.False(result.Success);

            // Arrange - catalog does not exits
            var catalog = SampleDataGenerator.GetCatalog("NotExistsCatalog");
            note = new HmmNote
            {
                Author = _author,
                Subject = "Test name",
                Content = "Test NameSpace",
                Catalog = catalog
            };

            // Act
            result = await _validator.ValidateEntityAsync(note);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task CannotChangeAuthorForExistsNote()
        {
            // Arrange
            var mockValidator = new Mock<IHmmValidator<HmmNote>>();
            mockValidator.Setup(v => v.ValidateEntityAsync(It.IsAny<HmmNote>()))
                .ReturnsAsync((HmmNote n) => ProcessingResult<HmmNote>.Ok(n));
            var manager = new HmmNoteManager(NoteRepository, UnitOfWork, Mapper, LookupRepository, DateProvider, mockValidator.Object);
            var note = new HmmNote
            {
                Author = _author,
                Description = "testing note",
                Subject = "testing note is here",
                Content = "<root><time>2017-08-01</time></root>",
                Catalog = _catalog
            };

            CurrentTime = new DateTime(2021, 4, 4, 8, 15, 0);
            var newNoteResult = await manager.CreateAsync(note);

            Assert.True(newNoteResult.Success);
            var savedRecResult = await LookupRepository.GetEntityAsync<HmmNoteDao>(newNoteResult.Value.Id);
            Assert.NotNull(savedRecResult.Value);
            Assert.Equal("fchy", savedRecResult.Value.Author.AccountName);

            // change the note author
            var newAuthor = await GetTestAuthor("amyWang");
            Assert.NotNull(newAuthor);
            newNoteResult.Value.Author = newAuthor;

            // Act
            var result = await _validator.ValidateEntityAsync(newNoteResult.Value);
            // Assert
            Assert.False(result.Success);
        }

        // ----- attachments validation gate (Phase 6c) -----

        private HmmNote ValidNote() => new()
        {
            Author = _author,
            Subject = "Test name",
            Content = "Test content",
            Catalog = _catalog,
        };

        private static VaultRef GoodRef(
            string path = "attachments/note-1/a.jpg",
            string contentType = "image/jpeg",
            long byteSize = 100)
            => new() { Path = path, ContentType = contentType, ByteSize = byteSize };

        [Fact]
        public async Task Empty_attachments_are_accepted()
        {
            // No PrimaryImage, empty Images → IsEmpty path, no work, no failure.
            var note = ValidNote();

            var result = await _validator.ValidateEntityAsync(note);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task Valid_primary_image_is_accepted()
        {
            var note = ValidNote();
            note.PrimaryImage = GoodRef("attachments/note-7/p.jpg", "image/png", 500);

            var result = await _validator.ValidateEntityAsync(note);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task Disallowed_content_type_is_rejected()
        {
            // image/gif is not in the v1 allow-list.
            var note = ValidNote();
            note.PrimaryImage = GoodRef(contentType: "image/gif");

            var result = await _validator.ValidateEntityAsync(note);

            Assert.False(result.Success);
            Assert.Contains("attachments", result.GetWholeMessage(),
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Negative_byte_size_is_rejected()
        {
            // Schema requires byteSize >= 0; the typed ref allows
            // negative because long has no constraint — this is
            // exactly what the gate catches.
            var note = ValidNote();
            note.PrimaryImage = GoodRef(byteSize: -1);

            var result = await _validator.ValidateEntityAsync(note);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Invalid_vault_path_is_rejected()
        {
            // ".." is forbidden by VaultPathUtil.
            var note = ValidNote();
            note.PrimaryImage = GoodRef(path: "attachments/../etc/passwd.jpg");

            var result = await _validator.ValidateEntityAsync(note);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Primary_image_in_images_list_is_rejected()
        {
            // Disjointness — the NoteAttachments ctor catches the
            // overlap; the validator surfaces it as Invalid rather
            // than letting it blow up at AutoMapper time.
            var primary = GoodRef("attachments/note-1/dup.jpg");
            var note = ValidNote();
            note.PrimaryImage = primary;
            note.Images = new List<VaultRef> { primary };

            var result = await _validator.ValidateEntityAsync(note);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task Valid_gallery_is_accepted()
        {
            var note = ValidNote();
            note.PrimaryImage = GoodRef("attachments/note-3/p.jpg");
            note.Images = new List<VaultRef>
            {
                GoodRef("attachments/note-3/a.jpg"),
                GoodRef("attachments/note-3/b.jpg", "image/webp", 10),
            };

            var result = await _validator.ValidateEntityAsync(note);

            Assert.True(result.Success);
        }

        public async Task InitializeAsync()
        {
            _author = await GetTestAuthor();
            _validator = new NoteValidator(LookupRepository);
            _catalog = await GetTestCatalog();
        }

        public async Task DisposeAsync()
        {
            await Task.CompletedTask;
        }
    }
}