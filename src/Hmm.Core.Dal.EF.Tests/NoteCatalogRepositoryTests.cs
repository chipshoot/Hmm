using Hmm.Utility.TestHelp;
using System;
using Hmm.Core.DomainEntity;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    public class NoteCatalogRepositoryTests : DbTestFixtureBase
    {
        private readonly NoteRender _render;
        private readonly Author _author;

        public NoteCatalogRepositoryTests()
        {
            var render = new NoteRender
            {
                Name = "TestRender",
                Namespace = "TestNamespace",
                Description = "Description"
            };

            _render = RenderRepository.Add(render);

            var user = new Author
            {
                AccountName = "fchy",
                Description = "Testing User",
                IsActivated = true,
            };
            _author = AuthorRepository.Add(user);
        }

        [Fact]
        public void Can_Add_NoteCatalog_To_DataSource()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "GasLog",
                Render = _render,
                Schema = "TestSchema",
                Description = "testing note",
            };

            // Act
            var savedRec = CatalogRepository.Add(catalog);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id > 0, "savedRec.Id > 0");
            Assert.True(catalog.Id == savedRec.Id, "cat.Id == savedRec.Id");
            Assert.True(CatalogRepository.ProcessMessage.Success);
        }

        [Fact]
        public void CanNot_Add_Already_Existed_NoteCatalog_To_DataSource()
        {
            // Arrange
            CatalogRepository.Add(new NoteCatalog
            {
                Name = "GasLog",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = true,
                Description = "testing note",
            });

            var cat = new NoteCatalog
            {
                Name = "GasLog",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = false,
                Description = "testing note",
            };

            // Act
            var savedRec = CatalogRepository.Add(cat);

            // Assert
            Assert.Null(savedRec);
            Assert.True(cat.Id <= 0, "cat.Id <=0");
            Assert.False(CatalogRepository.ProcessMessage.Success);
            Assert.Single(CatalogRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Can_Delete_Note_Catalog_From_DataSource()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "GasLog",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = true,
                Description = "testing note"
            };

            CatalogRepository.Add(catalog);

            // Act
            var result = CatalogRepository.Delete(catalog);

            // Assert
            Assert.True(result);
            Assert.True(CatalogRepository.ProcessMessage.Success);
        }

        [Fact]
        public void Cannot_Delete_NonExists_Catalog_From_DataSource()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "GasLog",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = true,
                Description = "testing note"
            };

            CatalogRepository.Add(catalog);

            var catalog2 = new NoteCatalog
            {
                Name = "GasLog2",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = false,
                Description = "testing note"
            };

            // Act
            var result = CatalogRepository.Delete(catalog2);

            // Assert
            Assert.False(result);
            Assert.False(CatalogRepository.ProcessMessage.Success);
            Assert.Single(CatalogRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Cannot_Delete_Catalog_With_Note_Associated()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "GasLog",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = true,
                Description = "testing note"
            };
            var savedCatalog = CatalogRepository.Add(catalog);

            var note = new HmmNote
            {
                Subject = "Testing subject",
                Content = "Testing content",
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Author = _author,
                Catalog = savedCatalog
            };
            NoteRepository.Add(note);

            // Act
            var result = CatalogRepository.Delete(catalog);

            // Assert
            Assert.False(result, "Error: deleted catalog with note attached to it");
            Assert.False(CatalogRepository.ProcessMessage.Success);
            Assert.Single(CatalogRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Can_Update_Catalog()
        {
            // Arrange - update name
            var catalog = new NoteCatalog
            {
                Name = "GasLog",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = true,
                Description = "testing note"
            };

            CatalogRepository.Add(catalog);

            catalog.Name = "GasLog2";

            // Act
            var result = CatalogRepository.Update(catalog);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("GasLog2", result.Name);

            // Arrange - update description
            catalog.Description = "new testing note";

            // Act
            result = CatalogRepository.Update(catalog);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new testing note", result.Description);
        }

        [Fact]
        public void Cannot_Update_Catalog_For_NonExists_Catalog()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "GasLog",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = true,
                Description = "testing note"
            };

            CatalogRepository.Add(catalog);

            var catalog2 = new NoteCatalog
            {
                Name = "GasLog2",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = false,
                Description = "testing note"
            };

            // Act
            var result = CatalogRepository.Update(catalog2);

            // Assert
            Assert.Null(result);
            Assert.False(CatalogRepository.ProcessMessage.Success);
            Assert.Single(CatalogRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Cannot_Update_Catalog_With_Duplicated_Name()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "GasLog",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = true,
                Description = "testing note"
            };
            CatalogRepository.Add(catalog);

            var catalog2 = new NoteCatalog
            {
                Name = "GasLog2",
                Render = _render,
                Schema = "TestSchema",
                IsDefault = false,
                Description = "testing note2"
            };
            CatalogRepository.Add(catalog2);

            catalog.Name = catalog2.Name;

            // Act
            var result = CatalogRepository.Update(catalog);

            // Assert
            Assert.Null(result);
            Assert.False(CatalogRepository.ProcessMessage.Success);
            Assert.Single(CatalogRepository.ProcessMessage.MessageList);
        }
    }
}