using FluentValidation;
using FluentValidation.Results;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.TestHelp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hmm.Core.Tests
{
    public class NoteCatalogManagerTests : TestFixtureBase
    {
        private INoteCatalogManager _manager;
        private FakeValidator _testValidator;
        private NoteRender _render;

        public NoteCatalogManagerTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void Can_Add_Catalog_To_DataSource()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test Catalog",
                Render = _render,
                Schema = "test schema"
            };

            // Act
            var newCatalog = _manager.Create(catalog);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(newCatalog);
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
            var updatedCatalog = _manager.Update(savedCatalog);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(updatedCatalog);
            Assert.Equal("changed test schema", updatedCatalog.Schema);
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

        private class FakeValidator : NoteCatalogValidator
        {
            public bool GetInvalidResult { get; set; }

            public override ValidationResult Validate(ValidationContext<NoteCatalog> context)
            {
                return GetInvalidResult
                    ? new ValidationResult(new List<ValidationFailure> { new("Catalog", "Catalog validator error") })
                    : new ValidationResult();
            }
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _testValidator = new FakeValidator();
            _render = new NoteRender();
            _manager = new NoteCatalogManager(CatalogRepository, _testValidator);
        }
    }
}