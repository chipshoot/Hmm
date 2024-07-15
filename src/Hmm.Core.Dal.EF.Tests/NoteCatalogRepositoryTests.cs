using Hmm.Utility.TestHelp;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Xml.Linq;
using Hmm.Core.Map.DbEntity;
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
        public void Can_Add_NoteCatalog_To_DataSource()
        {
            // Arrange
            var catalog = new NoteCatalogDao
            {
                Name = "GasLog",
                Schema = _sampleSchema,
                FormatType = NoteContentFormatType.Xml,
                IsDefault = true,
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
            CatalogRepository.Add(new NoteCatalogDao
            {
                Name = "GasLog",
                Schema = _sampleSchema,
                FormatType = NoteContentFormatType.Xml,
                IsDefault = true,
                Description = "testing note",
            });

            var cat = new NoteCatalogDao
            {
                Name = "GasLog",
                Schema = "TestSchema",
                FormatType = NoteContentFormatType.Xml,
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
            var catalog = new NoteCatalogDao
            {
                Name = "GasLog",
                Schema = _sampleSchema,
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
            var catalog = new NoteCatalogDao
            {
                Name = "GasLog",
                Schema = _sampleSchema,
                IsDefault = true,
                Description = "testing note"
            };

            CatalogRepository.Add(catalog);

            var catalog2 = new NoteCatalogDao
            {
                Name = "GasLog2",
                Schema = _sampleSchema,
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

        //[Fact]
        //public void Cannot_Delete_Catalog_With_Note_Associated()
        //{
        //    // Arrange
        //    var catalog = new NoteCatalogDao
        //    {
        //        Name = "GasLog",
        //        Schema = "TestSchema",
        //        IsDefault = true,
        //        Description = "testing note"
        //    };
        //    var savedCatalog = CatalogRepository.Add(catalog);

        //    var note = new HmmNote
        //    {
        //        Subject = "Testing subject",
        //        Content = "Testing content",
        //        CreateDate = DateTime.Now,
        //        LastModifiedDate = DateTime.Now,
        //        Author = _author,
        //        Catalog = savedCatalog
        //    };
        //    NoteRepository.Add(note);

        //    // Act
        //    var result = CatalogRepository.Delete(catalog);

        //    // Assert
        //    Assert.False(result, "Error: deleted catalog with note attached to it");
        //    Assert.False(CatalogRepository.ProcessMessage.Success);
        //    Assert.Single(CatalogRepository.ProcessMessage.MessageList);
        //}

        [Fact]
        public void Can_Update_Catalog()
        {
            // Arrange - update name
            var catalog = new NoteCatalogDao
            {
                Name = "GasLog",
                FormatType = NoteContentFormatType.PlainText,
                Schema = "",
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
            var catalog = new NoteCatalogDao
            {
                Name = "GasLog",
                FormatType = NoteContentFormatType.Markdown,
                Schema = "",
                IsDefault = true,
                Description = "testing note"
            };

            CatalogRepository.Add(catalog);

            var catalog2 = new NoteCatalogDao
            {
                Name = "GasLog2",
                FormatType = NoteContentFormatType.PlainText,
                Schema = "",
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
            var catalog = new NoteCatalogDao
            {
                Name = "GasLog",
                FormatType = NoteContentFormatType.Xml,
                Schema = _sampleSchema,
                IsDefault = true,
                Description = "testing note"
            };
            CatalogRepository.Add(catalog);

            var catalog2 = new NoteCatalogDao
            {
                Name = "GasLog2",
                FormatType = NoteContentFormatType.PlainText,
                Schema = "",
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