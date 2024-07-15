using Hmm.Core.Map.DbEntity;
using Hmm.Utility.TestHelp;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Xunit;

namespace Hmm.Core.Dal.EF.Tests
{
    public class NoteRepositoryTests : DbTestFixtureBase, IAsyncLifetime
    {
        private const string DefaultNoteCatalogName = "DefaultNoteCatalog";
        private const string AlternativeNoteCatalogName = "RawTextNote";
        private AuthorDao _author;
        private NoteCatalogDao _catalog;

        [Fact]
        public void Can_Add_Note_To_DataSource()
        {
            // Arrange
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");

            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = _catalog,
                Description = "testing note",
                Subject = "testing note is here",
                Content = xmlDocument.InnerXml
            };

            // Act
            var savedRec = NoteRepository.Add(note);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id >= 1, "savedRec.Id>=1");
            Assert.True(savedRec.Id == note.Id, "savedRec.Id==note.Id");
        }

        [Fact]
        public void Cannot_Add_Null_Note()
        {
            // Arrange
            HmmNoteDao note = null;

            // Act
            // Asset
            // ReSharper disable once ExpressionIsAlwaysNull
            Assert.Throws<ArgumentNullException>(() => NoteRepository.Add(note));
        }

        // Todo: rethink the logic of this test, we may need ever note has an author.
        //[Theory]
        //[ClassData(typeof(AuthorTestData))]
        //public void Cannot_Add_Note_With_NonExist_Author(AuthorDao author)
        //{
        //    // Arrange - null author for note
        //    var xmlDoc = new XmlDocument();
        //    xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
        //    var note = new HmmNoteDao
        //    {
        //        Author = author,
        //        Catalog = _catalog,
        //        Description = "testing note",
        //        CreateDate = DateTime.Now,
        //        LastModifiedDate = DateTime.Now,
        //        Subject = "testing note is here",
        //        Content = xmlDoc.InnerXml
        //    };

        //    // Act
        //    var savedRec = NoteRepository.Add(note);

        //    // Assert
        //    Assert.Null(savedRec);
        //    Assert.False(NoteRepository.GetEntities().Any());
        //    Assert.False(NoteRepository.ProcessMessage.Success);
        //    Assert.Single(NoteRepository.ProcessMessage.MessageList);
        //}

        [Theory]
        [ClassData(typeof(CatalogTestData))]
        public void CanAddNoteWithNonExistCatalogDefaultCatalogApplied(NoteCatalogDao catalog)
        {
            // Arrange - null catalog for note
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = catalog,
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml
            };

            // Act
            var savedRec = NoteRepository.Add(note);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id > 0, "savedRec.Id>0");
            Assert.True(savedRec.Id == note.Id, "savedRec.Id==note.Id");
            Assert.NotNull(savedRec.Catalog);
            Assert.Equal(DefaultNoteCatalogName, savedRec.Catalog.Name);
        }

        [Fact]
        public void Can_Update_Note_Description()
        {
            // Arrange
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = _catalog,
                Description = "testing note",
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml,
            };
            NoteRepository.Add(note);

            // Act
            note.Description = "testing note2";
            var savedRec = NoteRepository.Update(note);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id >= 1, "savedRec.Id >= 1");
            Assert.True(savedRec.Id == note.Id, "savedRec.Id == note.Id");
            Assert.Equal("testing note2", savedRec.Description);
            Assert.Equal("testing note2", note.Description);
            Assert.True(NoteRepository.ProcessMessage.Success);
        }

        [Fact]
        public void Can_Update_Note_Subject()
        {
            // Arrange
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = _catalog,
                Description = "testing note",
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml,
            };
            NoteRepository.Add(note);
            Assert.True(NoteRepository.ProcessMessage.Success);

            // Act
            note.Subject = "This is new subject";
            var savedRec = NoteRepository.Update(note);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id >= 1, "savedRec.Id >=1");
            Assert.Equal("This is new subject", savedRec.Subject);
            Assert.Equal(note.Id, savedRec.Id);
            Assert.Equal("This is new subject", note.Subject);
            Assert.True(NoteRepository.ProcessMessage.Success);
        }

        [Fact]
        public void Can_Update_Note_Content()
        {
            // Arrange
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = _catalog,
                Description = "testing note",
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml,
            };
            NoteRepository.Add(note);
            Assert.True(NoteRepository.ProcessMessage.Success);

            // Act
            var newXml = new XmlDocument();
            newXml.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><GasLog></GasLog>");
            note.Content = newXml.InnerXml;
            var savedRec = NoteRepository.Update(note);

            // Assert
            Assert.NotNull(savedRec);
            Assert.True(savedRec.Id >= 1, "savedRec.Id >=1");
            Assert.True(savedRec.Id == note.Id, "savedRec.Id == note.Id");
            Assert.Equal(newXml.InnerXml, savedRec.Content);
            Assert.NotEqual(xmlDoc.InnerXml, savedRec.Content);
        }

        [Fact]
        public void Can_Update_Note_Catalog()
        {
            // Arrange
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = _catalog,
                Description = "testing note",
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml
            };
            NoteRepository.Add(note);
            Assert.True(NoteRepository.ProcessMessage.Success);

            // changed the note catalog
            note.Catalog = CatalogRepository.GetEntities().FirstOrDefault(cat => !cat.IsDefault);

            // Act
            var savedRec = NoteRepository.Update(note);

            // Assert
            Assert.NotNull(savedRec);
            Assert.NotNull(savedRec.Catalog);
            Assert.NotNull(note.Catalog);
            Assert.Equal(AlternativeNoteCatalogName, savedRec.Catalog.Name);
            Assert.Equal(AlternativeNoteCatalogName, note.Catalog.Name);
        }

        [Fact]
        public void Can_Update_Note_Catalog_To_Null_Catalog_Default_Catalog_Applied()
        {
            // Arrange - null catalog for note
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var catalog = CatalogRepository.GetEntities().FirstOrDefault(cat => !cat.IsDefault);
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = catalog,
                Description = "testing note",
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml
            };
            NoteRepository.Add(note);
            Assert.True(NoteRepository.ProcessMessage.Success);
            var savedRec = NoteRepository.GetEntities().FirstOrDefault();
            Assert.NotNull(savedRec);
            Assert.Equal(AlternativeNoteCatalogName, savedRec.Catalog.Name);

            // null the catalog
            note.Catalog = null;

            // Act
            savedRec = NoteRepository.Update(note);

            // Assert
            Assert.True(NoteRepository.ProcessMessage.Success);
            Assert.NotNull(savedRec);
            Assert.NotNull(savedRec.Catalog);
            Assert.Equal(DefaultNoteCatalogName, savedRec.Catalog.Name);
        }

        [Fact]
        public void Can_Update_NoteCatalog_To_Non_Exists_Catalog_Default_Catalog_Applied()
        {
            // Arrange - none exists catalog
            var catalog = new NoteCatalogDao
            {
                Id = 200,
                FormatType = NoteContentFormatType.Json,
                Schema = "",
                Name = "Gas Log",
                Description = "Testing catalog"
            };

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var initialCatalog = CatalogRepository.GetEntities().FirstOrDefault(cat => !cat.IsDefault);
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = initialCatalog,
                Description = "testing note",
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml
            };
            NoteRepository.Add(note);
            Assert.True(NoteRepository.ProcessMessage.Success);

            note.Catalog = catalog;

            // Act
            var savedRec = NoteRepository.Update(note);

            // Assert
            Assert.True(NoteRepository.ProcessMessage.Success);
            Assert.NotNull(savedRec);
            Assert.NotNull(savedRec.Catalog);
            Assert.Equal(DefaultNoteCatalogName, savedRec.Catalog.Name);
        }

        [Fact]
        public void Can_Update_NoteCatalog_To_Catalog_With_Invalid_Id_DefaultCatalog_Applied()
        {
            // Arrange - none exists catalog
            var catalog = new NoteCatalogDao
            {
                Id = -1,
                Name = "Gas Log",
                FormatType = NoteContentFormatType.Json,
                Schema = "",
                Description = "Testing catalog"
            };

            var initialCatalog = CatalogRepository.GetEntities().FirstOrDefault(cat => !cat.IsDefault);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = initialCatalog,
                Description = "testing note",
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml
            };
            NoteRepository.Add(note);
            Assert.True(NoteRepository.ProcessMessage.Success);

            note.Catalog = catalog;

            // Act
            var savedRec = NoteRepository.Update(note);

            // Assert
            Assert.True(NoteRepository.ProcessMessage.Success);
            Assert.NotNull(savedRec);
            Assert.NotNull(savedRec.Catalog);
            Assert.Equal(DefaultNoteCatalogName, savedRec.Catalog.Name);
        }

        [Fact]
        public void Cannot_Update_NonExits_Note()
        {
            // Arrange - non exists id
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = _catalog,
                Description = "testing note2",
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml,
            };
            NoteRepository.Add(note);

            // Act
            var orgId = note.Id;
            note.Id = 2;
            var savedRec = NoteRepository.Update(note);

            // Assert
            Assert.False(NoteRepository.ProcessMessage.Success);
            Assert.Null(savedRec);

            // Arrange - invalid id
            note.Id = 0;

            // Act
            savedRec = NoteRepository.Update(note);

            // Assert
            Assert.False(NoteRepository.ProcessMessage.Success);
            Assert.Null(savedRec);

            // do this to make clear up code pass
            note.Id = orgId;
        }

        [Fact]
        public void Can_Delete_Note()
        {
            // Arrange
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = _catalog,
                Description = "testing note",
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml,
            };
            NoteRepository.Add(note);
            Assert.True(NoteRepository.ProcessMessage.Success);

            // Act
            var result = NoteRepository.Delete(note);

            // Assert
            Assert.True(NoteRepository.ProcessMessage.Success);
            Assert.True(result);
            Assert.False(NoteRepository.GetEntities().Any());
        }

        [Fact]
        public void Cannot_Delete_NonExists_Note()
        {
            // Arrange
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-16\"?><root><time>2017-08-01</time></root>");
            var note = new HmmNoteDao
            {
                Author = _author,
                Catalog = _catalog,
                Description = "testing note",
                Subject = "testing note is here",
                Content = xmlDoc.InnerXml,
            };
            NoteRepository.Add(note);
            Assert.True(NoteRepository.ProcessMessage.Success);

            // change the note id to create a new note
            var orgId = note.Id;
            note.Id = 2;

            // Act
            var result = NoteRepository.Delete(note);

            // Assert
            Assert.False(NoteRepository.ProcessMessage.Success);
            Assert.False(result);

            // do this just to make clear up code pass
            note.Id = orgId;
        }

        //private class AuthorTestData : IEnumerable<object[]>
        //{
        //    public IEnumerator<object[]> GetEnumerator()
        //    {
        //        yield return [null];

        //        // Arrange - none exists author
        //        yield return
        //        [
        //            new AuthorDao
        //                    {
        //                        Id = 1,
        //                        AccountName = "jfang",
        //                        IsActivated = true,
        //                        Description = "testing author"
        //                    }
        //        ];

        //        // Arrange - author with invalid author id
        //        yield return
        //        [
        //            new AuthorDao
        //                    {
        //                        Id = 0,
        //                        AccountName = "jfang",
        //                        IsActivated = true,
        //                        Description = "testing author"
        //                    }
        //        ];
        //    }

        //    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        //}

        private class CatalogTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return [null];

                // Arrange - none exists author
                yield return
                [
                    new NoteCatalogDao
                            {
                                Id = 200,
                                Name = "Gas Log",
                                FormatType = NoteContentFormatType.PlainText,
                                Schema = "",
                                IsDefault = true,
                                Description = "Testing catalog"
                            }
                ];

                // Arrange - author with invalid author id
                yield return
                [
                    new NoteCatalogDao
                            {
                                Id = 0,
                                Name = "Gas Log",
                                FormatType = NoteContentFormatType.PlainText,
                                Schema = "",
                                Description = "Testing catalog"
                            }
                ];
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private void SetupTestingEnvironment()
        {
            var xDocument = new XDocument(
                new XElement("Root", new XElement("Child", "Value")));

            var catalog = new NoteCatalogDao
            {
                Name = DefaultNoteCatalogName,
                Schema = xDocument.ToString(),
                FormatType = NoteContentFormatType.Xml,
                IsDefault = true,
                Description = "default xml note",
            };
            _catalog = CatalogRepository.Add(catalog);
            var catalog2 = new NoteCatalogDao
            {
                Name = AlternativeNoteCatalogName,
                Schema = "",
                FormatType = NoteContentFormatType.PlainText,
                IsDefault = false,
                Description = "raw text note",
            };
            CatalogRepository.Add(catalog2);

            var contactDb = new ContactDao
            {
                Contact = """
                      { "FirstName": "John", "LastName": "Doe", "Emails": [ { "Address": "fchy@yahoo.com", "Type": "Personal", "IsPrimary": "false" }, { "Address": "fchy5979@gamil.com", "Type": "Personal", "IsPrimary": "true" }, { "Address": "fchy@outlook.com", "Type": "Work", "IsPrimary": "false" } ], "Phones": [ { "Type": "Home", "Number": "123-456-7890" }, { "Type": "Work", "Number": "456-789-0123" } ], "Addresses": [ { "Type": "Home", "Street": "123 Main St", "City": "Springfield", "State": "IL", "Zip": "62701" }, { "Type": "Work", "Street": "456 Elm St", "City": "Springfield", "State": "IL", "Zip": "62702" } ] }
                      """,
                Description = "testing contact",
                IsActivated = true
            };
            var contact = ContactRepository.Add(contactDb);
            var author = new AuthorDao
            {
                AccountName = "fchy",
                ContactInfo = contact,
                Description = "testing user",
                IsActivated = true
            };
            _author = AuthorRepository.Add(author);
        }

        public async Task InitializeAsync()
        {
            Transaction = await ((DbContext)DbContext).Database.BeginTransactionAsync();
            SetupTestingEnvironment();
        }

        public async Task DisposeAsync()
        {
            await Transaction.RollbackAsync();
            await Transaction.DisposeAsync();
        }
    }
}