using FluentValidation;
using FluentValidation.Results;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validation;
using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hmm.Core.Tests
{
    public class NoteRenderManagerTests : TestFixtureBase
    {
        private readonly INoteRenderManager _manager;
        private readonly FakeValidator _testValidator;

        public NoteRenderManagerTests()
        {
            InsertSeedRecords();
            _testValidator = new FakeValidator();
            _manager = new NoteRenderManager(RenderRepository, _testValidator);
        }

        [Fact]
        public void Can_Add_Render_To_DataSource()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "Test Render",
                Namespace = "Default NameSpace"
            };

            // Act
            var newRender = _manager.Create(render);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(newRender);
            Assert.True(newRender.Id >= 1, "newRender.Id >=1");
            Assert.Equal("Test Render", newRender.Name);
        }

        [Fact]
        public void Can_Update_Render()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "Test Render",
                Namespace = "Default NameSpace"
            };
            _manager.Create(render);
            var savedRender = _manager.GetEntities().FirstOrDefault(c => c.Id == 1);
            Assert.NotNull(savedRender);

            // Act
            savedRender.Namespace = "changed NameSpace";
            var updatedRender = _manager.Update(savedRender);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(updatedRender);
            Assert.Equal("changed NameSpace", updatedRender.Namespace);
        }

        [Fact]
        public void Cannot_Update_Invalid_Render()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "Test Render",
                Namespace = "Default NameSpace"
            };
            var savedRender = _manager.Create(render);
            _testValidator.GetInvalidResult = true;

            // Act
            savedRender.Name = "updated test catalog";
            var updatedRender = _manager.Update(savedRender);

            // Assert
            Assert.False(_manager.ProcessResult.Success);
            Assert.Null(updatedRender);
        }

        [Fact]
        public void Can_Search_Render_By_Id()
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "Test Render",
                Namespace = "Default NameSpace"
            };
            var newRender = _manager.Create(render);

            // Act
            var savedRender = _manager.GetEntities().FirstOrDefault(c => c.Id == newRender.Id);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(savedRender);
            Assert.Equal(savedRender.Name, render.Name);
        }

        private class FakeValidator : NoteRenderValidator
        {
            public bool GetInvalidResult { get; set; }

            public override ValidationResult Validate(ValidationContext<NoteRender> context)
            {
                return GetInvalidResult
                    ? new ValidationResult(new List<ValidationFailure> { new("Render", "Render validator error") })
                    : new ValidationResult();
            }
        }
    }
}