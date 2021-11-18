using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerializer;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.MeasureUnit;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Xml.Linq;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class DiscountXmlNoteSerializerTests : AutoTestFixtureBase
    {
        private INoteSerializer<GasDiscount> _noteSerializer;
        private Author _author;

        public DiscountXmlNoteSerializerTests()
        {
            SetupDevEnv();
        }

        [Theory]
        [InlineData("<GasDiscount><Program>Petro-Canada membership</Program><Amount><Money><Value>0.8</Value><Code>CAD</Code></Money></Amount><DiscountType>PerLiter</DiscountType><IsActive>true</IsActive><Comment /></GasDiscount>")]
        public void Can_Parse_Discount_String_Content(string contentText)
        {
            // Arrange
            var xmlDoc = XDocument.Parse(string.Format(NoteXmlTextBase, XmlNamespace, contentText));

            var discount = new GasDiscount
            {
                Program = "Petro-Canada membership",
                Amount = 0.8m.GetCad(),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true
            };

            // Act
            var note = _noteSerializer.GetNote(discount);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(note);
            Assert.Equal(AutomobileConstant.GasDiscountRecordSubject, note.Subject);
            Assert.Equal(note.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
        }

        [Fact]
        public void Can_Get_Right_Xml_With_Note_Content_Contains_Invalid_Char_To_DataSource()
        {
            // Arrange - note with null content
            var xmlDoc = XDocument.Parse($"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{XmlNamespace}\"><Content><GasDiscount><Program>&lt;Costco&gt; membership</Program><Amount><Money><Value>0.2</Value><Code>CAD</Code></Money></Amount><DiscountType>PerLiter</DiscountType><IsActive>true</IsActive><Comment /></GasDiscount></Content></Note>");

            var discount = new GasDiscount
            {
                //AuthorId = DefaultAuthor.Id,
                Program = "<Costco> membership",
                Amount = 0.2m.GetCad(),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true
            };

            // Act
            var note = _noteSerializer.GetNote(discount);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(note);
            Assert.Equal(note.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
            Assert.Contains("&lt;", note.Content);
            Assert.Contains("&gt;", note.Content);
        }

        [Fact]
        public void Can_Get_Discount_By_Parse_Valid_Note_Xml()
        {
            // Arrange
            var xmlDoc = XDocument.Parse(
                $"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{XmlNamespace}\"><Content><GasDiscount><Program>&lt;Petro&gt;-\"Canada\" membership</Program><Amount><Money><Value>0.8</Value><Code>CAD</Code></Money></Amount><DiscountType>PerLiter</DiscountType><IsActive>true</IsActive><Comment /></GasDiscount></Content></Note>");
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.GasDiscountRecordSubject,
                Content = xmlDoc.ToString(SaveOptions.DisableFormatting),
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            var discountExpected = new GasDiscount
            {
                Id = 1,
                //AuthorId = DefaultAuthor.Id,
                Program = "<Petro>-\"Canada\" membership",
                Amount = 0.8m.GetCad(),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true
            };

            // Act
            var discount = _noteSerializer.GetEntity(note);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(discount);
            Assert.Equal(discountExpected.Id, discount.Id);
            Assert.Equal(discountExpected.Program, discount.Program);
            Assert.Equal(discountExpected.Amount, discount.Amount);
            Assert.Equal(discountExpected.DiscountType, discount.DiscountType);
            Assert.Equal(discountExpected.IsActive, discount.IsActive);
            Assert.Equal(_author.Id.ToString(), discount.AuthorId.ToString());
        }

        [Fact]
        public void Can_Get_Right_Xml_With_Null_Note_Content_To_DataSource()
        {
            // Arrange - note with null content
            var discount = new GasDiscount
            {
                //AuthorId = DefaultAuthor.Id,
                Program = "Petro-Canada membership",
                Amount = 0.8m.GetCad(),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true
            };
            var xmlDoc = XDocument.Parse(
                $"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{XmlNamespace}\"><Content><GasDiscount><Program>Petro-Canada membership</Program><Amount><Money><Value>0.8</Value><Code>CAD</Code></Money></Amount><DiscountType>PerLiter</DiscountType><IsActive>true</IsActive><Comment /></GasDiscount></Content></Note>");

            // Act
            var note = _noteSerializer.GetNote(discount);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.Equal(note.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
            Assert.NotNull(note);
        }

        [Theory]
        [InlineData("Not a discount content", "Content is not xml", false, false, true)]
        [InlineData("<GasDiscount><Amount><Money><Value>0.8</Value><Code>CAD</Code></Money></Amount><DiscountType>PerLiter</DiscountType><IsActive>true</IsActive><Comment /></GasDiscount>", "Content is missing automobile element from schema definition", true, true, false)]
        [InlineData("<GasDiscount><IsOftenUse>false</IsOftenUse><Program>Petro-Canada membership</Program><Amount><Money><Value>0.8</Value><Code>CAD</Code></Money></Amount><DiscountType>PerLiter</DiscountType><IsActive>true</IsActive><Comment /></GasDiscount>", "Content contains more element than schema definition", true, true, false)]
        [InlineData("<GasDiscount><Program>Petro-Canada membership</Program><Amount><Money><Value>0.8</Value><Code>CAD</Code></Money></Amount><DiscountType>PerLiter</DiscountType><IsActive>true</IsActive><Comment /></GasDiscount>", "Valid discount xml", true, false, false)]
        public void Can_Valid_Xml_Content_Against_Schema(string content, string comment, bool expectSuccess, bool expectWarning, bool expectError)
        {
            // Arrange
            var noteContent = string.Format(NoteXmlTextBase, XmlNamespace, content);
            var note = new HmmNote
            {
                Id = 1,
                Author = _author,
                Subject = AutomobileConstant.AutoMobileRecordSubject,
                Content = noteContent,
                CreateDate = DateTime.Now,
                LastModifiedDate = DateTime.Now,
                Description = comment
            };

            // Act
            var discount = _noteSerializer.GetEntity(note);

            // Assert
            Assert.Equal(expectSuccess, _noteSerializer.ProcessResult.Success);
            Assert.Equal(expectWarning, _noteSerializer.ProcessResult.HasWarning);
            Assert.Equal(expectError, _noteSerializer.ProcessResult.HasError);
            if (_noteSerializer.ProcessResult.Success)
            {
                Assert.NotNull(discount);
            }
            else
            {
                Assert.Null(discount);
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
            _noteSerializer = new GasDiscountXmlNoteSerializer(Application, new NullLogger<GasDiscount>(), LookupRepo);
            _author = ApplicationRegister.DefaultAuthor;
        }
    }
}