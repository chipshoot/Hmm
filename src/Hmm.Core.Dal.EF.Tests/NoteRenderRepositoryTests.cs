using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    public class NoteRenderRepositoryTests : DbTestFixtureBase
    {
        [Fact]
        public void Can_Add_NoteRender_To_DataSource()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "GasLog",
                Namespace = "Note.GasLog",
                Description = "testing note",
                IsDefault = true
            };

            // Act
            var savedRec = RenderRepository.Add(render);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id > 0, "savedRec.Id> 0");
            Assert.True(render.Id == savedRec.Id, "render.Id == savedRec.Id");
            Assert.True(RenderRepository.ProcessMessage.Success);
        }

        [Fact]
        public void Can_Delete_NoteRender_From_DataSource()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "GasLog",
                Namespace = "Note.GasLog",
                Description = "testing note"
            };

            RenderRepository.Add(render);

            // Act
            var result = RenderRepository.Delete(render);

            // Assert
            Assert.True(result);
            Assert.True(RenderRepository.ProcessMessage.Success);
        }

        [Fact]
        public void Cannot_Delete_NonExists_NoteRender_From_DataSource()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "GasLog",
                Namespace = "Note.GasLog",
                Description = "testing note"
            };

            RenderRepository.Add(render);

            var render2 = new NoteRender
            {
                Name = "GasLog2",
                Namespace = "Note.GasLog2",
                Description = "testing note"
            };

            // Act
            var result = RenderRepository.Delete(render2);

            // Assert
            Assert.False(result);
            Assert.False(RenderRepository.ProcessMessage.Success);
            Assert.Single(RenderRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Cannot_Delete_NoteRender_With_CatalogAssociated()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "DefaultRender",
                Namespace = "NameSpace",
                IsDefault = true,
                Description = "Description"
            };
            var savedRender = RenderRepository.Add(render);
            var catalog = new NoteCatalog
            {
                Name = "GasLog",
                Render = savedRender,
                Schema = "Scheme",
                IsDefault = true,
                Description = "testing Catalog"
            };
            CatalogRepository.Add(catalog);

            // Act
            var result = RenderRepository.Delete(render);

            // Assert
            Assert.False(result, "Error: deleted render with catalog attached to it");
            Assert.False(RenderRepository.ProcessMessage.Success);
            Assert.Single(RenderRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Can_Update_NoteRender()
        {
            // Arrange - update name
            var render = new NoteRender
            {
                Name = "GasLog",
                Namespace = "Note.GasLog",
                Description = "testing note"
            };

            RenderRepository.Add(render);

            render.Name = "GasLog2";

            // Act
            var result = RenderRepository.Update(render);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("GasLog2", result.Name);

            // Arrange - update description
            render.Description = "new testing note";

            // Act
            result = RenderRepository.Update(render);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new testing note", result.Description);
        }

        [Fact]
        public void Cannot_Update_For_NonExists_NoteRender()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "GasLog",
                Namespace = "Note.GasLog",
                IsDefault = true,
                Description = "testing note"
            };

            RenderRepository.Add(render);

            var render2 = new NoteRender
            {
                Name = "GasLog2",
                Namespace = "Note.GasLog",
                IsDefault = false,
                Description = "testing note"
            };

            // Act
            var result = RenderRepository.Update(render2);

            // Assert
            Assert.Null(result);
            Assert.False(RenderRepository.ProcessMessage.Success);
            Assert.Single(RenderRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public void Cannot_Update_Note_Render_With_DuplicatedName()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "GasLog",
                IsDefault = true,
                Description = "testing note"
            };
            RenderRepository.Add(render);

            var render2 = new NoteRender
            {
                Name = "GasLog2",
                IsDefault = false,
                Description = "testing note2"
            };
            RenderRepository.Add(render2);

            render.Name = render2.Name;

            // Act
            var result = RenderRepository.Update(render);

            // Assert
            Assert.Null(result);
            Assert.False(RenderRepository.ProcessMessage.Success);
            Assert.Single(RenderRepository.ProcessMessage.MessageList);
        }
    }
}