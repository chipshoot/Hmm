using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerializer;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Xml.Linq;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutomobileXmlNoteSerializerTests : AutoTestFixtureBase
    {
        private INoteSerializer<AutomobileInfo> _noteSerializer;

        public AutomobileXmlNoteSerializerTests()
        {
            SetupDevEnv();
        }

        [Theory]
        [InlineData("<Automobile><Maker>Subaru</Maker><Brand>Outback</Brand><Year>2017</Year><Color>Blue</Color><Pin>135</Pin><Plate>BCTT208</Plate><MeterReading>1234535</MeterReading></Automobile>")]
        public void Can_Parse_Automobile_String_Content(string contentText)
        {
            // Arrange
            var xmlDoc = XDocument.Parse(string.Format(NoteXmlTextBase, XmlNamespace, contentText));

            var auto = new AutomobileInfo
            {
                Maker = "Subaru",
                Brand = "Outback",
                Year = "2017",
                Color = "Blue",
                Pin = "135",
                Plate = "BCTT208",
                MeterReading = 1234535
            };

            // Act
            var note = _noteSerializer.GetNote(auto);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(note);
            Assert.Equal(AutomobileConstant.AutoMobileRecordSubject, note.Subject);
            Assert.Equal(note.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
        }

        [Fact]
        public void Can_Get_Right_Xml_With_Note_Content_Contains_Invalid_Char_To_DataSource()
        {
            // Arrange - note with null content
            var xmlDoc = XDocument.Parse($"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{XmlNamespace}\"><Content><Automobile><Maker>Subaru</Maker><Brand>&lt;Outback&gt;</Brand><Year>2017</Year><Color>Blue</Color><Pin>135</Pin><Plate>BCTT208</Plate><MeterReading>1234535</MeterReading></Automobile></Content></Note>");

            // Note: Outback is surrounding by bracket to test escape special character in class
            var auto = new AutomobileInfo
            {
                Maker = "Subaru",
                Brand = "<Outback>",
                Year = "2017",
                Color = "Blue",
                Pin = "135",
                Plate = "BCTT208",
                MeterReading = 1234535
            };

            // Act
            var note = _noteSerializer.GetNote(auto);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(note);
            Assert.Equal(note.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
            Assert.Contains("&lt;", note.Content);
            Assert.Contains("&gt;", note.Content);
        }

        [Fact]
        public void Can_Get_Automobile_By_Parse_Valid_Note_Xml()
        {
            // Arrange
            var xmlDoc = XDocument.Parse($"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{XmlNamespace}\"><Content><Automobile><Maker>Subaru</Maker><Brand>Outback</Brand><Year>2017</Year><Color>Blue</Color><Pin>135</Pin><Plate>BCTT208</Plate><MeterReading>1234535</MeterReading></Automobile></Content></Note>");
            var note = new HmmNote
            {
                Id = 1,
                Author = DefaultAuthor,
                Subject = AutomobileConstant.AutoMobileRecordSubject,
                Content = xmlDoc.ToString(SaveOptions.DisableFormatting),
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            var autoExpected = new AutomobileInfo
            {
                Id = 1,
                Maker = "Subaru",
                Brand = "Outback",
                Year = "2017",
                Color = "Blue",
                Pin = "135",
                Plate = "BCTT208",
                MeterReading = 1234535,
            };

            // Act
            var auto = _noteSerializer.GetEntity(note);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(auto);
            Assert.Equal(autoExpected.Id, auto.Id);
            Assert.Equal(autoExpected.Maker, auto.Maker);
            Assert.Equal(autoExpected.Brand, auto.Brand);
            Assert.Equal(autoExpected.Year, auto.Year);
            Assert.Equal(autoExpected.Color, auto.Color);
            Assert.Equal(autoExpected.Pin, auto.Pin);
            Assert.Equal(autoExpected.Plate, auto.Plate);
            Assert.Equal(DefaultAuthor.Id.ToString(), auto.AuthorId.ToString());
        }

        [Fact]
        public void Can_Get_Right_Xml_With_Null_Note_Content_To_DataSource()
        {
            // Arrange - note with null content
            var auto = new AutomobileInfo
            {
                Maker = "Subaru",
                Brand = "Outback",
                Year = "2017",
                Color = "Blue",
                Pin = "135",
                MeterReading = 1234535
            };
            var xmlDoc = XDocument.Parse(
                $"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{XmlNamespace}\"><Content><Automobile><Maker>Subaru</Maker><Brand>Outback</Brand><Year>2017</Year><Color>Blue</Color><Pin>135</Pin><Plate /><MeterReading>1234535</MeterReading></Automobile></Content></Note>");

            // Act
            var note = _noteSerializer.GetNote(auto);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.Equal(note.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
            Assert.NotNull(note);
        }

        [Theory]
        [InlineData("Not a automobile content", "Content is not xml", false, false, true)]
        [InlineData("<Automobile><Brand>Outback</Brand><Year>2017</Year><Color>Blue</Color><Pin>135</Pin><Plate>BCTT208</Plate><MeterReading>1234535</MeterReading></Automobile>", "Content is missing automobile element from schema definition", true, true, false)]
        [InlineData("<Automobile><Dealer>Subaru Mississauga</Dealer><Maker>Subaru</Maker><Brand>Outback</Brand><Year>2017</Year><Color>Blue</Color><Pin>135</Pin><Plate>BCTT208</Plate><MeterReading>1234535</MeterReading></Automobile>", "Content contains more element than schema definition", true, true, false)]
        [InlineData("<Automobile><Maker>Subaru</Maker><Brand>Outback</Brand><Year>2017</Year><Color>Blue</Color><Pin>135</Pin><Plate>BCTT208</Plate><MeterReading>1234535</MeterReading></Automobile>", "Content meet schema definition", true, false, false)]
        public void Can_Valid_Xml_Content_Against_Schema(string content, string comment, bool expectSuccess, bool expectWarning, bool expectError)
        {
            // Arrange
            var noteContent = string.Format(NoteXmlTextBase, XmlNamespace, content);
            var note = new HmmNote
            {
                Id = 1,
                Author = DefaultAuthor,
                Subject = AutomobileConstant.AutoMobileRecordSubject,
                Content = noteContent,
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Description = comment
            };

            // Act
            var car = _noteSerializer.GetEntity(note);

            // Assert
            Assert.Equal(expectSuccess, _noteSerializer.ProcessResult.Success);
            Assert.Equal(expectWarning, _noteSerializer.ProcessResult.HasWarning);
            Assert.Equal(expectError, _noteSerializer.ProcessResult.HasError);
            if (_noteSerializer.ProcessResult.Success)
            {
                Assert.NotNull(car);
            }
            else
            {
                Assert.Null(car);
            }
        }

        [Fact]
        public void Get_Null_For_Null_Note()
        {
            // Arrange & Act
            var note = _noteSerializer.GetNote(null);

            // Assert
            Assert.Null(note);
            Assert.True(_noteSerializer.ProcessResult.MessageList.Count > 0);
        }

        private void SetupDevEnv()
        {
            InsertSeedRecords();
            var schemaStr = File.ReadAllText("Automobile.xsd");
            var catalog = new NoteCatalog { Schema = schemaStr };
            _noteSerializer = new AutomobileXmlNoteSerializer(XmlNamespace, catalog, new NullLogger<AutomobileXmlNoteSerializer>());
        }
    }
}