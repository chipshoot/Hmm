using Hmm.Core.Map.DbEntity;
using Hmm.Utility.TestHelp;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    public class NoteCatalogRepositoryTests : DbTestFixtureBase, IAsyncLifetime
    {
        private readonly string _sampleSchema;

        public NoteCatalogRepositoryTests()
        {
            var xDocument = new XDocument(
                new XElement("Root", new XElement("Child", "Value")));
            _sampleSchema = xDocument.ToString();
        }

        [Fact]
        public async Task Can_Add_NoteCatalog_To_DataSource()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();

            // Act
            var result = await CatalogRepository.AddAsync(catalog);
            await DbContext.CommitAsync();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.True(result.Value.Id > 0, "savedRec.Id > 0");
            Assert.True(catalog.Id == result.Value.Id, "cat.Id == savedRec.Id");
        }

        [Fact]
        public async Task CanNot_Add_Already_Existed_NoteCatalog_To_DataSource()
        {
            // Arrange
            await CatalogRepository.AddAsync(SampleDataGenerator.GetCatalogDao());
            await DbContext.CommitAsync();

            var cat = SampleDataGenerator.GetCatalogDao();

            // Act
            var result = await CatalogRepository.AddAsync(cat);

            // Assert
            Assert.False(result.Success);
            Assert.True(cat.Id <= 0, "cat.Id <=0");
            Assert.True(result.Messages.Count > 0);
        }

        [Fact]
        public async Task Can_Delete_Note_Catalog_From_DataSource()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();
            await CatalogRepository.AddAsync(catalog);
            await DbContext.CommitAsync();

            // Act
            var result = await CatalogRepository.DeleteAsync(catalog);
            await DbContext.CommitAsync();

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task Cannot_Delete_NonExists_Catalog_From_DataSource()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();

            await CatalogRepository.AddAsync(catalog);
            await DbContext.CommitAsync();

            var catalog2 = new NoteCatalogDao
            {
                Name = "GasLog2",
                Schema = _sampleSchema,
                IsDefault = false,
                Description = "testing note"
            };

            // Act
            var result = await CatalogRepository.DeleteAsync(catalog2);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Messages.Count > 0);
        }

        [Fact]
        public async Task Cannot_Delete_Catalog_With_Note_Associated()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();
            var catalogResult = await CatalogRepository.AddAsync(catalog);
            await DbContext.CommitAsync();

            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = SampleDataGenerator.GetContactDao(),
                Description = "testing user",
                IsActivated = true
            };
            await AuthorRepository.AddAsync(author);
            await DbContext.CommitAsync();

            var note = new HmmNoteDao
            {
                Subject = "Testing subject",
                Content = "Testing content",
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Author = author,
                Catalog = catalogResult.Value
            };
            await NoteRepository.AddAsync(note);
            await DbContext.CommitAsync();

            // Act
            var result = await CatalogRepository.DeleteAsync(catalog);

            // Assert
            Assert.False(result.Success, "Error: deleted catalog with note attached to it");
            Assert.True(result.Messages.Count > 0);
        }

        [Fact]
        public async Task Can_Update_Catalog()
        {
            // Arrange - update name
            var catalog = SampleDataGenerator.GetCatalogDao();
            await CatalogRepository.AddAsync(catalog);
            await DbContext.CommitAsync();
            catalog.Name = "GasLog2";

            // Act
            var result = await CatalogRepository.UpdateAsync(catalog);
            await DbContext.CommitAsync();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("GasLog2", result.Value.Name);

            // Arrange - update description
            catalog.Description = "new testing note";

            // Act
            result = await CatalogRepository.UpdateAsync(catalog);
            await DbContext.CommitAsync();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Equal("new testing note", result.Value.Description);
        }

        [Fact]
        public async Task Cannot_Update_Catalog_For_NonExists_Catalog()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();
            await CatalogRepository.AddAsync(catalog);
            await DbContext.CommitAsync();

            var catalog2 = new NoteCatalogDao
            {
                Name = "GasLog2",
                FormatType = NoteContentFormatType.PlainText,
                Schema = "",
                IsDefault = false,
                Description = "testing note"
            };

            // Act
            var result = await CatalogRepository.UpdateAsync(catalog2);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Messages.Count > 0);
        }

        [Fact]
        public async Task Cannot_Update_Catalog_With_Duplicated_Name()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();
            await CatalogRepository.AddAsync(catalog);
            await DbContext.CommitAsync();

            var catalog2 = new NoteCatalogDao
            {
                Name = "GasLog2",
                FormatType = NoteContentFormatType.PlainText,
                Schema = "",
                IsDefault = false,
                Description = "testing note2"
            };
            await CatalogRepository.AddAsync(catalog2);
            await DbContext.CommitAsync();

            catalog.Name = catalog2.Name;

            // Act
            var result = await CatalogRepository.UpdateAsync(catalog);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.Messages.Count > 0);
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
