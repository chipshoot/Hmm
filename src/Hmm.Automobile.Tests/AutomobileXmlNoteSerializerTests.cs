using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerializer;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutomobileXmlNoteSerializerTests : AutoTestFixtureBase
    {
        private INoteSerializer<AutomobileInfo> _noteSerializer;
        private Author _user;

        public AutomobileXmlNoteSerializerTests()
        {
            SetupDevEnv();
        }

        [Theory]
        [InlineData("<Automobile><Maker>Subaru</Maker><Brand>Outback</Brand><Year>2017</Year><Color>Blue</Color><Pin>135</Pin><Plate>BCTT208</Plate><MeterReading>1234535</MeterReading></Automobile>",
            "<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{0}\"><Content>{1}</Content></Note>")]
        public void Can_Parse_Automobile_String_Content(string contentText, string xmlContent)
        {
            // Arrange
            var xmlDoc = XDocument.Parse(string.Format(xmlContent, XmlNamespace, contentText));

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
                Author = _user,
                Subject = AutomobileConstant.AutoMobileRecordSubject,
                Content = xmlDoc.ToString(SaveOptions.DisableFormatting),
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            var autoExpected = new AutomobileInfo
            {
                Id = 1,
                AuthorId = _user.Id,
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
            Assert.Equal(autoExpected.AuthorId.ToString(), auto.AuthorId.ToString());
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

        [Fact]
        public void Get_Null_Fro_Null_Note()
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
            _user = LookupRepo.GetEntities<Author>().FirstOrDefault();
            var schemaStr = File.ReadAllText("Automobile.xsd");
            var catalog = new NoteCatalog { Schema = schemaStr };
            _noteSerializer = new AutomobileXmlNoteSerializer(XmlNamespace, catalog, new NullLogger<AutomobileXmlNoteSerializer>());
        }
    }
}