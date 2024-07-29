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
            var savedRec = await CatalogRepository.AddAsync(catalog);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id > 0, "savedRec.Id > 0");
            Assert.True(catalog.Id == savedRec.Id, "cat.Id == savedRec.Id");
            Assert.True(CatalogRepository.ProcessMessage.Success);
        }

        [Fact]
        public async Task CanNot_Add_Already_Existed_NoteCatalog_To_DataSource()
        {
            // Arrange
            await CatalogRepository.AddAsync(SampleDataGenerator.GetCatalogDao());

            var cat = SampleDataGenerator.GetCatalogDao();

            // Act
            var savedRec = await CatalogRepository.AddAsync(cat);

            // Assert
            Assert.Null(savedRec);
            Assert.True(cat.Id <= 0, "cat.Id <=0");
            Assert.False(CatalogRepository.ProcessMessage.Success);
            Assert.Single(CatalogRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public async Task Can_Delete_Note_Catalog_From_DataSource()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();
            await CatalogRepository.AddAsync(catalog);

            // Act
            var result = await CatalogRepository.DeleteAsync(catalog);

            // Assert
            Assert.True(result);
            Assert.True(CatalogRepository.ProcessMessage.Success);
        }

        [Fact]
        public async Task Cannot_Delete_NonExists_Catalog_From_DataSource()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();

            await CatalogRepository.AddAsync(catalog);

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
            Assert.False(result);
            Assert.False(CatalogRepository.ProcessMessage.Success);
            Assert.Single(CatalogRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public async Task Cannot_Delete_Catalog_With_Note_Associated()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();
            var savedCatalog = await CatalogRepository.AddAsync(catalog);

            var author = new AuthorDao
            {
                AccountName = "glog",
                ContactInfo = SampleDataGenerator.GetContactDao(),
                Description = "testing user",
                IsActivated = true
            };
            await AuthorRepository.AddAsync(author);

            var note = new HmmNoteDao
            {
                Subject = "Testing subject",
                Content = "Testing content",
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Author = author,
                Catalog = savedCatalog
            };
            await NoteRepository.AddAsync(note);

            // Act
            var result =await CatalogRepository.DeleteAsync(catalog);

            // Assert
            Assert.False(result, "Error: deleted catalog with note attached to it");
            Assert.False(CatalogRepository.ProcessMessage.Success);
            Assert.Single(CatalogRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public async Task Can_Update_Catalog()
        {
            // Arrange - update name
            var catalog = SampleDataGenerator.GetCatalogDao();
            await CatalogRepository.AddAsync(catalog);
            catalog.Name = "GasLog2";

            // Act
            var result = await CatalogRepository.UpdateAsync(catalog);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("GasLog2", result.Name);

            // Arrange - update description
            catalog.Description = "new testing note";

            // Act
            result = await CatalogRepository.UpdateAsync(catalog);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("new testing note", result.Description);
        }

        [Fact]
        public async Task Cannot_Update_Catalog_For_NonExists_Catalog()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();
            await CatalogRepository.AddAsync(catalog);

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
            Assert.Null(result);
            Assert.False(CatalogRepository.ProcessMessage.Success);
            Assert.Single(CatalogRepository.ProcessMessage.MessageList);
        }

        [Fact]
        public async Task Cannot_Update_Catalog_With_Duplicated_Name()
        {
            // Arrange
            var catalog = SampleDataGenerator.GetCatalogDao();
            await CatalogRepository.AddAsync(catalog);

            var catalog2 = new NoteCatalogDao
            {
                Name = "GasLog2",
                FormatType = NoteContentFormatType.PlainText,
                Schema = "",
                IsDefault = false,
                Description = "testing note2"
            };
            await CatalogRepository.AddAsync(catalog2);

            catalog.Name = catalog2.Name;

            // Act
            var result = await CatalogRepository.UpdateAsync(catalog);

            // Assert
            Assert.Null(result);
            Assert.False(CatalogRepository.ProcessMessage.Success);
            Assert.Single(CatalogRepository.ProcessMessage.MessageList);
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