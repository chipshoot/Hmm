using Hmm.Core.DomainEntity;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    public class SubsystemRepositoryTests : CoreDalEfTestBase
    {
        private readonly Author _author;

        public SubsystemRepositoryTests()
        {
            SetupTestingEnv();
            _author = AuthorRepository.GetEntities().FirstOrDefault();
        }

        [Fact]
        public void Can_Add_Subsystem_To_DataSource()
        {
            // Arrange
            Assert.NotNull(_author);
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
                        Schema = "Test Catalog1 Schema",
                        Render = new NoteRender
                        {
                            Name = "Test Catalog1 Render",
                            Namespace = "Hmm.Render",
                            Description = "This is description of test catalog1 render"
                        }
                    },
                    new ()
                    {
                        Name = "Test Catalog2",
                        Schema = "Test Catalog2 Schema",
                        Render = new NoteRender
                        {
                            Name = "Test Catalog2 Render",
                            Namespace = "Hmm.Render",
                            Description = "This is description of test catalog2 render"
                        }
                    }
                }
            };

            // Act
            var newSys = SubsystemRepository.Add(sys);

            // Assert
            Assert.NotNull(newSys);
            Assert.Equal(2, newSys.NoteCatalogs.Count());
            Assert.True(newSys.Id > 0, "newSubsystem.Id >=1");
            Assert.Equal("Test subsystem", newSys.Name);
            Assert.True(SubsystemRepository.ProcessMessage.Success);
        }

        [Fact]
        public void Can_Update_Subsystem()
        {
            // Arrange
            Assert.NotNull(_author);
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
                        Schema = "Test Catalog1 Schema",
                        Render = new NoteRender
                        {
                            Name = "Test Catalog1 Render",
                            Namespace = "Hmm.Render",
                            Description = "This is description of test catalog1 render"
                        }
                    },
                    new ()
                    {
                        Name = "Test Catalog2",
                        Schema = "Test Catalog2 Schema",
                        Render = new NoteRender
                        {
                            Name = "Test Catalog2 Render",
                            Namespace = "Hmm.Render",
                            Description = "This is description of test catalog2 render"
                        }
                    }
                }
            };
            var newSys = SubsystemRepository.Add(sys);
            var savedSys = LookupRepo.GetEntities<Subsystem>().FirstOrDefault(s => s.Id == newSys.Id);
            Assert.NotNull(savedSys);

            // Act
            savedSys.Description = "changed default Subsystem";
            var firstCatalog = savedSys.NoteCatalogs.First();
            Assert.NotNull(firstCatalog);
            firstCatalog.Name = "Updated Test Catalog1";
            firstCatalog.Render.Name = "Update Test Catalog1 Render Name";
            var updatedSys = SubsystemRepository.Update(savedSys);

            // Assert
            Assert.NotNull(updatedSys);
            Assert.Equal("changed default Subsystem", updatedSys.Description);
            Assert.True(SubsystemRepository.ProcessMessage.Success);
        }

        [Fact]
        public void Can_Search_Subsystem_By_Id()
        {
            // Arrange
            Assert.NotNull(_author);
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
                        Schema = "Test Catalog1 Schema",
                        Render = new NoteRender
                        {
                            Name = "Test Catalog1 Render",
                            Namespace = "Hmm.Render",
                            Description = "This is description of test catalog1 render"
                        }
                    }
                }
            };
            var newSys = SubsystemRepository.Add(sys);

            // Act
            var savedSys = SubsystemRepository.GetEntities().FirstOrDefault(c => c.Id == newSys.Id);

            // Assert
            Assert.NotNull(savedSys);
            Assert.Equal(savedSys.Name, sys.Name);
            Assert.True(SubsystemRepository.ProcessMessage.Success);
        }
    }
}