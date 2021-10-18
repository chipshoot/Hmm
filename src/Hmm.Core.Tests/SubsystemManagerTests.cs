using Hmm.Core.DefaultManager;
using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hmm.Core.Tests
{
    public class SubsystemManagerTests : TestFixtureBase
    {
        private ISubsystemManager _manager;
        private NoteRenderManager _renderMan;
        private NoteCatalogManager _catalogMan;
        private MockSubsystemValidator _testValidator;
        private Author _author;

        public SubsystemManagerTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void Can_Add_Subsystem_To_DataSource()
        {
            // Arrange
            var oldCatalogCount = _catalogMan.GetEntities().Count();
            var oldRenderCount = _renderMan.GetEntities().Count();
            var render = new NoteRender();
            var sys = new Subsystem
            {
                Name = "Test subsystem",
                DefaultAuthor = _author,
                Description = "Default subsystem",
                NoteCatalogs = new List<NoteCatalog>
                {
                    new()
                    {
                        Name = "Test Catalog1",
                        Render = render,
                        Schema = ""
                    },
                    new()
                    {
                        Name = "Test Catalog2",
                        Render = render,
                        Schema = ""
                    }
                }
            };

            // Act
            var newSys = _manager.Create(sys);
            var newCatalogCount = _catalogMan.GetEntities().Count();
            var newRenderCount = _renderMan.GetEntities().Count();

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(newSys);
            Assert.True(newSys.Id >= 1, "newSubsystem.Id >=1");
            Assert.Equal("Test subsystem", newSys.Name);
            Assert.Equal(oldCatalogCount + 2, newCatalogCount);
            Assert.Equal(oldRenderCount + 2, newRenderCount);
            Assert.Equal(2, newSys.NoteCatalogs.Count());
        }

        [Fact]
        public void Can_Update_Subsystem()
        {
            // Arrange
            var sys = new Subsystem
            {
                Name = "Test Subsystem",
                DefaultAuthor = _author,
                Description = "Default Subsystem",
                NoteCatalogs = new List<NoteCatalog>
                {
                    new()
                    {
                        Name = "Test Catalog1",
                        Render = new NoteRender { Name = "Test Render1", Namespace = "Test Namespace"},
                        Schema = "Test Schema1"
                    },
                    new()
                    {
                        Name = "Test Catalog2",
                        Render = new NoteRender { Name = "Test Render2", Namespace = "Test Namespace"},
                        Schema = "Test Schema2"
                    }
                }
            };
            _manager.Create(sys);
            var savedSys = _manager.GetEntities().FirstOrDefault(s => s.Id == 1);
            Assert.NotNull(savedSys);

            // Act
            savedSys.Description = "changed default Subsystem";
            var savedCatalog = savedSys.NoteCatalogs.FirstOrDefault();
            Assert.NotNull(savedCatalog);
            savedCatalog.Schema = "Updated Tested schema1";
            savedCatalog.Render.Description = "Tested Render description";

            var updatedSys = _manager.Update(savedSys);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(updatedSys);
            Assert.Equal("changed default Subsystem", updatedSys.Description);
            Assert.Equal("Updated Tested schema1", updatedSys.NoteCatalogs.First().Schema);
            Assert.Equal("Tested Render description", updatedSys.NoteCatalogs.First().Render.Description);
        }

        [Fact]
        public void Cannot_Update_Invalid_Subsystem()
        {
            // Arrange
            var sys = new Subsystem
            {
                Name = "Test Subsystem",
                DefaultAuthor = _author,
                Description = "Default Subsystem"
            };
            var savedSys = _manager.Create(sys);
            _testValidator.GetInvalidResult = true;

            // Act
            savedSys.Name = "updated test Subsystem";
            var updatedSystem = _manager.Update(savedSys);

            // Assert
            Assert.False(_manager.ProcessResult.Success);
            Assert.Null(updatedSystem);
        }

        [Fact]
        public void Can_Search_Subsystem_By_Id()
        {
            // Arrange
            var sys = new Subsystem
            {
                Name = "Test Subsystem",
                DefaultAuthor = _author,
                Description = "Default Subsystem"
            };
            var newSys = _manager.Create(sys);

            // Act
            var savedSys = _manager.GetEntities().FirstOrDefault(c => c.Id == newSys.Id);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedSys);
            Assert.Equal(savedSys.Name, sys.Name);
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _testValidator = FakeSubsystemValidator;
            _renderMan = new NoteRenderManager(RenderRepository, FakeRenderValidator);
            _catalogMan = new NoteCatalogManager(CatalogRepository, FakeCatalogValidator, LookupRepo);
            _manager = new SubsystemManager(SubsystemRepository, _testValidator);
            _author = AuthorRepository.GetEntities().FirstOrDefault();
            Assert.NotNull(_author);
        }
    }
}