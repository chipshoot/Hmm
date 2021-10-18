using Hmm.Core.DefaultManager;
using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Linq;
using Hmm.Core.DefaultManager.Validator;
using Xunit;

namespace Hmm.Core.Tests
{
    public class NoteCatalogManagerTests : TestFixtureBase
    {
        private INoteCatalogManager _manager;
        private INoteRenderManager _renderMan;
        private NoteRender _render;
        private MockCatalogValidator _testValidator;

        public NoteCatalogManagerTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void Can_Add_Catalog_To_DataSource()
        {
            // Arrange
            var oldRenders = _renderMan.GetEntities().Count();
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Render = _render,
                Schema = "test schema"
            };

            // Act
            var newCatalog = _manager.Create(catalog);
            var newRender = _renderMan.GetEntities().FirstOrDefault(r => r.Name == _render.Name);
            var renders = _renderMan.GetEntities().Count();

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(newCatalog);
            Assert.NotNull(newRender);
            Assert.Equal(oldRenders + 1, renders);
            Assert.True(newCatalog.Id >= 1, "newNote.Id >=1");
            Assert.Equal("Test Catalog", newCatalog.Name);
            Assert.NotNull(newCatalog.Render);
        }

        [Fact]
        public void New_Add_Catalog_Can_Reference_Exists_Render()
        {
            // Arrange
            var render = _renderMan.GetEntities().FirstOrDefault();
            Assert.NotNull(render);
            var oldRenders = _renderMan.GetEntities().Count();
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Render = render,
                Schema = "test schema"
            };

            // Act
            var newCatalog = _manager.Create(catalog);
            var renders = _renderMan.GetEntities().Count();

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(newCatalog);
            Assert.Equal(oldRenders, renders);
            Assert.True(newCatalog.Id >= 1, "newNote.Id >=1");
            Assert.Equal("Test Catalog", newCatalog.Name);
        }

        [Fact]
        public void Can_Update_Catalog()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Render = _render,
                Schema = "test schema"
            };
            _manager.Create(catalog);
            var savedCatalog = _manager.GetEntities().FirstOrDefault(c => c.Id == 1);
            Assert.NotNull(savedCatalog);

            // Act
            savedCatalog.Schema = "changed test schema";
            savedCatalog.Render.Name = "Updated render name";
            var updatedCatalog = _manager.Update(savedCatalog);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(updatedCatalog);
            Assert.Equal("changed test schema", updatedCatalog.Schema);
            Assert.Equal("Updated render name", savedCatalog.Render.Name);
            Assert.NotNull(updatedCatalog.Render);
        }

        [Fact]
        public void Cannot_Update_Invalid_Catalog()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Render = _render,
                Schema = "test schema"
            };
            var savedCatalog = _manager.Create(catalog);
            _testValidator.GetInvalidResult = true;

            // Act
            savedCatalog.Name = "updated test catalog";
            var updatedCatalog = _manager.Update(savedCatalog);

            // Assert
            Assert.False(_manager.ProcessResult.Success);
            Assert.Null(updatedCatalog);
        }

        [Fact]
        public void Cannot_Update_Not_Exists_Catalog()
        {
            // Arrange - no id
            var render = _renderMan.GetEntities().FirstOrDefault();
            Assert.NotNull(render);
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Render = render,
                Schema = "test schema"
            };

            // Act
            var updatedCatalog = _manager.Update(catalog);

            // Assert
            Assert.False(_manager.ProcessResult.Success);
            Assert.Null(updatedCatalog);

            // Arrange - id does not exists
            _manager.ProcessResult.Rest();
            catalog = new NoteCatalog
            {
                Id = 100,
                Name = "Test Catalog",
                Render = render,
                Schema = "test schema"
            };

            // Act
            updatedCatalog = _manager.Update(catalog);

            // Assert
            Assert.False(_manager.ProcessResult.Success);
            Assert.Null(updatedCatalog);
        }

        [Fact]
        public void Cannot_Update_Catalog_With_Invalid_Render()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Render = _render,
                Schema = "test schema"
            };
            var savedCatalog = _manager.Create(catalog);

            // Act
            savedCatalog.Render = new NoteRender();
            var updatedCatalog = _manager.Update(savedCatalog);

            // Assert
            Assert.False(_manager.ProcessResult.Success);
            Assert.Null(updatedCatalog);
        }

        [Fact]
        public void Can_Search_Catalog_By_Id()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Render = _render,
                Schema = "test schema"
            };
            var newCatalog = _manager.Create(catalog);

            // Act
            var savedNote = _manager.GetEntities().FirstOrDefault(c => c.Id == newCatalog.Id);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedNote);
            Assert.Equal(savedNote.Name, catalog.Name);
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _render = new NoteRender { Name = "Test Render" };
            _renderMan = new NoteRenderManager(RenderRepository, new NoteRenderValidator());
            _testValidator = FakeCatalogValidator;
            _manager = new NoteCatalogManager(CatalogRepository, _testValidator, LookupRepo);
        }
    }
}