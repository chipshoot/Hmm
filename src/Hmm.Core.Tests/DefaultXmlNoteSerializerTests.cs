using Hmm.Core.DomainEntity;
using Hmm.Core.NoteSerializer;
using Hmm.Utility.TestHelp;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Hmm.Core.Tests
{
    public class DefaultXmlNoteSerializerTests : TestFixtureBase
    {
        private INoteSerializer<HmmNote> _noteSerializer;
        private Author _user;

        public DefaultXmlNoteSerializerTests()
        {
            SetupTestEnv();
        }

        [Theory]
        [InlineData("Test content",
            "<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{0}\"><Content>{1}</Content></Note>", false, true)]
        [InlineData("<InnerText xmlns=\"http://schema.hmm.com/2020\"><Content>Test content</Content></InnerText>",
            "<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{0}\"><Content>{1}</Content></Note>", true, false)]
        public void Can_Parse_Note_String_Content(string contentText, string xmlContent, bool hasWarning, bool hasError)
        {
            // Arrange
            var xmlDoc = XDocument.Parse(string.Format(xmlContent, CoreConstants.DefaultNoteNamespace, contentText));

            var note = new HmmNote
            {
                Author = _user,
                Subject = "Testing note",
                Content = contentText
            };

            // Act
            var newNote = _noteSerializer.GetNote(note);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.Equal(hasWarning, _noteSerializer.ProcessResult.HasWarning);
            Assert.Equal(hasError, _noteSerializer.ProcessResult.HasError);
            Assert.NotNull(newNote);
            Assert.Equal("Testing note", newNote.Subject);
            Assert.Equal(newNote.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
            Assert.Equal(newNote.CreateDate, newNote.LastModifiedDate);
        }

        [Fact]
        public void Can_Get_Right_Xml_With_Note_Content_Contains_Invalid_Char_To_DataSource()
        {
            // Arrange - note with null content
            var xmlDoc = XDocument.Parse($"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{CoreConstants.DefaultNoteNamespace}\"><Content>Testing content with &lt; and &gt;</Content></Note>");
            var note = new HmmNote
            {
                Author = _user,
                Subject = "Testing note",
                Content = "Testing content with < and >"
            };

            // Act
            var newNote = _noteSerializer.GetNote(note);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(newNote);
            Assert.Equal(newNote.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
            Assert.Contains("&lt;", newNote.Content);
            Assert.Contains("&gt;", newNote.Content);
        }

        [Fact]
        public void Can_Avoid_Duplicated_Parse_For_Valid_Note_Xml()
        {
            // Arrange
            var xmlDoc = XDocument.Parse($"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{CoreConstants.DefaultNoteNamespace}\"><Content>Test content</Content></Note>");

            var note = new HmmNote
            {
                Author = _user,
                Subject = "Testing note",
                Content = $"<Note xmlns=\"{CoreConstants.DefaultNoteNamespace}\"><Content>Test content</Content></Note>"
            };

            // Act
            var newNote = _noteSerializer.GetNote(note);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(newNote);
            Assert.Equal("Testing note", newNote.Subject);
            Assert.Equal(newNote.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
            Assert.Equal(newNote.CreateDate, newNote.LastModifiedDate);
        }

        [Fact]
        public void Can_Get_Right_Xml_With_Null_Note_Content_To_DataSource()
        {
            // Arrange - note with null content
            var note = new HmmNote
            {
                Author = _user,
                Subject = "Testing note",
                Content = null
            };
            var xmlDoc = XDocument.Parse($"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{CoreConstants.DefaultNoteNamespace}\"><Content></Content></Note>");

            // Act
            var newNote = _noteSerializer.GetNote(note);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.Equal(newNote.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
            Assert.NotNull(newNote);
        }

        [Fact]
        public void Get_Null_Fro_Null_Note()
        {
            // Arrange & Act
            var newNote = _noteSerializer.GetNote(null);

            // Assert
            Assert.Null(newNote);
            Assert.True(_noteSerializer.ProcessResult.MessageList.Count > 0);
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _user = LookupRepo.GetEntities<Author>().FirstOrDefault();
            var schemaStr = File.ReadAllText("NotebaseSchema.xsd");
            var catalog = new NoteCatalog { Schema = schemaStr };
            _noteSerializer = new TestDefaultXmlNoteSerializer(new NullLogger<TestDefaultXmlNoteSerializer>(), catalog);
        }

        private class TestDefaultXmlNoteSerializer : DefaultXmlNoteSerializer<HmmNote>
        {
            public TestDefaultXmlNoteSerializer(ILogger logger, NoteCatalog catalog) : base(logger)
            {
                Catalog = catalog;
            }
        }
    }
}