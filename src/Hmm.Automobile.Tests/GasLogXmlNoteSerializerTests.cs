using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerializer;
using Hmm.Core;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.MeasureUnit;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace Hmm.Automobile.Tests
{
    /// <summary>
    /// sample:
    /// <?xml version=\"1.0\" encoding=\"utf-16\" ?>
    /// <Note xmlns=\"{XmlNamespace}\">
    ///     <Content>
    ///         <GasLog>
    ///             <Date>2021-10-18T00:00:00.0000000</Date>
    ///             <Distance>
    ///                 <Dimension>
    ///                     <Value>215</Value>
    ///                     <Unit>Kilometre</Unit>
    ///                 </Dimension>
    ///             </Distance>
    ///             <CurrentMeterReading>
    ///                 <Dimension>
    ///                     <Value>13000</Value>
    ///                     <Unit>Kilometre</Unit>
    ///                 </Dimension>
    ///             </CurrentMeterReading>
    ///             <Gas>
    ///                 <Volume>
    ///                     <Value>34</Value>
    ///                     <Unit>Liter</Unit>
    ///                 </Volume>
    ///             </Gas>
    ///             <Price>
    ///                 <Money>
    ///                     <Value>1.34</Value>
    ///                     <Code>CAD</Code>
    ///                 </Money>
    ///             </Price>
    ///             <GasStation>Costco</GasStation>
    ///             <Discounts>
    ///                 <Discount>
    ///                     <Amount>
    ///                         <Money>
    ///                             <Value>0.8</Value>
    ///                             <Code>CAD</Code>
    ///                         </Money>
    ///                     </Amount>
    ///                     <Program>1</Program>
    ///                 </Discount>
    ///             </Discounts>
    ///             <Automobile>1</Automobile>
    ///             <Comment>&lt;This is a&gt; comment</Comment>
    ///             <CreateDate>2021-10-15T00:00:00.0000000</CreateDate>
    ///         </GasLog>
    ///     </Content>
    /// </Note>
    /// </summary>
    public class GasLogXmlNoteSerializerTests : AutoTestFixtureBase
    {
        private INoteSerializer<GasLog> _noteSerializer;
        private AutomobileInfo _defaultCar;
        private GasDiscount _defaultDiscount;

        public GasLogXmlNoteSerializerTests()
        {
            SetupDevEnv();
        }

        [Theory]
        [InlineData("<GasLog><Date>2021-10-15T00:00:00.0000000</Date><Distance><Dimension><Value>300</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>13000</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>34</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>1.34</Value><Code>CAD</Code></Money></Price><GasStation>Costco</GasStation><Discounts></Discounts><Automobile>1</Automobile><Comment></Comment><CreateDate>2021-10-15T00:00:00.0000000</CreateDate></GasLog>")]
        public void Can_Parse_GasLog_String_Content(string contentText)
        {
            // Arrange
            var xmlDoc = XDocument.Parse(string.Format(NoteXmlTextBase, XmlNamespace, contentText));
            CurrentTime = new DateTime(2021, 10, 15);

            var gasLog = new GasLog
            {
                AuthorId = DefaultAuthor.Id,
                Date = new DateTime(2021, 10, 15),
                Car = _defaultCar,
                Distance = 300d.GetKilometer(),
                CurrentMeterReading = 13000d.GetKilometer(),
                Gas = 34d.GetLiter(),
                Price = 1.34m.GetCad(),
                Station = "Costco",
                Discounts = new List<GasDiscountInfo>(),
                Comment = string.Empty,
                CreateDate = DateProvider.UtcNow
            };

            // Act
            var note = _noteSerializer.GetNote(gasLog);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(note);
            Assert.Equal(AutomobileConstant.GasLogRecordSubject, note.Subject);
            Assert.Equal(note.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
        }

        [Fact]
        public void Can_Get_Right_Xml_With_Note_Content_Contains_Invalid_Char_To_DataSource()
        {
            // Arrange - note with null content
            var xmlDoc = XDocument.Parse($"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{XmlNamespace}\"><Content><GasLog><Date>2021-10-18T00:00:00.0000000</Date><Distance><Dimension><Value>215</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>13000</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>34</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>1.34</Value><Code>CAD</Code></Money></Price><GasStation>Costco</GasStation><Discounts></Discounts><Automobile>1</Automobile><Comment>&lt;This is a&gt; comment</Comment><CreateDate>2021-10-15T00:00:00.0000000</CreateDate></GasLog></Content></Note>");

            var gasLog = new GasLog
            {
                AuthorId = DefaultAuthor.Id,
                Car = _defaultCar,
                Date = new DateTime(2021, 10, 18),
                Distance = 215d.GetKilometer(),
                CurrentMeterReading = 13000d.GetKilometer(),
                Gas = 34d.GetLiter(),
                Price = 1.34m.GetCad(),
                Station = "Costco",
                Discounts = new List<GasDiscountInfo>(),
                Comment = "<This is a> comment",
                CreateDate = new DateTime(2021, 10, 15)
            };

            // Act
            var note = _noteSerializer.GetNote(gasLog);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(note);
            Assert.Equal(note.Content, xmlDoc.ToString(SaveOptions.DisableFormatting));
            Assert.Contains("&lt;", note.Content);
            Assert.Contains("&gt;", note.Content);
        }

        [Fact]
        public void Can_Get_GasLog_By_Parse_Valid_Note_Xml()
        {
            // Arrange
            Assert.NotNull(_defaultCar);
            Assert.NotNull(_defaultDiscount);
            var xmlDoc = XDocument.Parse($"<?xml version=\"1.0\" encoding=\"utf-16\" ?><Note xmlns=\"{XmlNamespace}\"><Content><GasLog><Date>2021-10-18T00:00:00.0000000</Date><Distance><Dimension><Value>250</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>13500</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>50</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>1.30</Value><Code>CAD</Code></Money></Price><GasStation>Costco</GasStation><Discounts><Discount><Amount><Money><Value>0.8</Value><Code>CAD</Code></Money></Amount><Program>{_defaultDiscount.Id}</Program></Discount></Discounts><Automobile>{_defaultCar.Id}</Automobile><Comment>This is a comment</Comment><CreateDate>2021-10-17T00:00:00.0000000</CreateDate></GasLog></Content></Note>");
            var note = new HmmNote
            {
                Id = 1,
                Author = DefaultAuthor,
                Subject = AutomobileConstant.GasLogRecordSubject,
                Content = xmlDoc.ToString(SaveOptions.DisableFormatting),
                CreateDate = DateProvider.UtcNow,
                LastModifiedDate = DateProvider.UtcNow
            };

            var gasLogExpected = new GasLog
            {
                Id = 1,
                AuthorId = DefaultAuthor.Id,
                Date = new DateTime(2021, 10, 18),
                Car = _defaultCar,
                Distance = 250d.GetKilometer(),
                CurrentMeterReading = 13500d.GetKilometer(),
                Gas = 50d.GetLiter(),
                Price = 1.3m.GetCad(),
                Station = "Costco",
                Discounts = new List<GasDiscountInfo>
                {
                    new()
                    {
                        Amount = 0.8m.GetCad(),
                        Program = _defaultDiscount
                    }
                },
                Comment = "This is a comment",
                CreateDate = new DateTime(2021, 10, 17)
            };

            // Act
            var gasLog = _noteSerializer.GetEntity(note);

            // Assert
            Assert.True(_noteSerializer.ProcessResult.Success);
            Assert.NotNull(gasLog);
            Assert.Equal(gasLogExpected.Id, gasLog.Id);
            Assert.Equal(gasLogExpected.Date, gasLog.Date);
            Assert.Equal(gasLogExpected.Price, gasLog.Price);
            Assert.Equal(gasLogExpected.Distance, gasLog.Distance);
            Assert.Equal(gasLogExpected.Gas, gasLog.Gas);
            Assert.Equal(gasLogExpected.CurrentMeterReading, gasLog.CurrentMeterReading);
            Assert.Equal(gasLogExpected.AuthorId.ToString(), gasLog.AuthorId.ToString());
        }

        [Theory]
        [InlineData("Not a log content", "Content is not xml", false, false, true)]
        [InlineData("<GasLog><Distance><Dimension><Value>215</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>13000</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>34</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>1.34</Value><Code>CAD</Code></Money></Price><GasStation>Costco</GasStation><Discounts></Discounts><Automobile>1</Automobile><Comment>&lt;This is a&gt; comment</Comment><CreateDate>2021-10-15T00:00:00.0000000</CreateDate></GasLog>", "Content is missing element than schema definition", true, true, false)]
        [InlineData("<GasLog><Date>2021-10-18T00:00:00.0000000</Date><Special>This is special message</Special><Distance><Dimension><Value>215</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>13000</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>34</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>1.34</Value><Code>CAD</Code></Money></Price><GasStation>Costco</GasStation><Discounts></Discounts><Automobile>1</Automobile><Comment>&lt;This is a&gt; comment</Comment><CreateDate>2021-10-15T00:00:00.0000000</CreateDate></GasLog>", "Content contains more element than schema definition", true, true, false)]
        [InlineData("<GasLog><Date>2021-10-18T00:00:00.0000000</Date><Distance><Dimension><Value>215</Value><Unit>Kilometre</Unit></Dimension></Distance><CurrentMeterReading><Dimension><Value>13000</Value><Unit>Kilometre</Unit></Dimension></CurrentMeterReading><Gas><Volume><Value>34</Value><Unit>Liter</Unit></Volume></Gas><Price><Money><Value>1.34</Value><Code>CAD</Code></Money></Price><GasStation>Costco</GasStation><Discounts></Discounts><Automobile>1</Automobile><Comment>&lt;This is a&gt; comment</Comment><CreateDate>2021-10-15T00:00:00.0000000</CreateDate></GasLog>", "Valid gas log xml", true, false, false)]
        public void Can_Valid_Xml_Content_Against_Schema(string content, string comment, bool expectSuccess, bool expectWarning, bool expectError)
        {
            // Arrange
            CurrentTime = new DateTime(2021, 10, 18);
            var noteContent = string.Format(NoteXmlTextBase, XmlNamespace, content);
            var note = new HmmNote
            {
                Id = 1,
                Author = DefaultAuthor,
                Subject = AutomobileConstant.GasLogRecordSubject,
                Content = noteContent,
                CreateDate = DateProvider.UtcNow,
                LastModifiedDate = DateProvider.UtcNow,
                Description = comment
            };

            // Act
            var gasLog = _noteSerializer.GetEntity(note);

            // Assert
            Assert.Equal(expectSuccess, _noteSerializer.ProcessResult.Success);
            Assert.Equal(expectWarning, _noteSerializer.ProcessResult.HasWarning);
            Assert.Equal(expectError, _noteSerializer.ProcessResult.HasError);
            if (_noteSerializer.ProcessResult.Success)
            {
                Assert.NotNull(gasLog);
            }
            else
            {
                Assert.Null(gasLog);
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

            var noteManager = new HmmNoteManager(NoteRepository, new NoteValidator(NoteRepository), DateProvider);

            // setup automobile manager
            var autoCat = LookupRepo.GetEntities<NoteCatalog>()
                .FirstOrDefault(c => c.Name == AutomobileConstant.AutoMobileInfoCatalogName);
            Assert.NotNull(autoCat);
            var autoNoteSerializer = new AutomobileXmlNoteSerializer(XmlNamespace, autoCat, new NullLogger<AutomobileXmlNoteSerializer>());
            var autoManager = new AutomobileManager(autoNoteSerializer, noteManager, LookupRepo, DefaultAuthor);

            // Insert car
            var car = new AutomobileInfo
            {
                AuthorId = DefaultAuthor.Id,
                Brand = "AutoBack",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "1234"
            };
            var savedCar = autoManager.Create(car);
            Assert.NotNull(savedCar);

            // setup discount manager
            var discountCat = LookupRepo.GetEntities<NoteCatalog>()
                .FirstOrDefault(c => c.Name == AutomobileConstant.GasDiscountCatalogName);
            Assert.NotNull(discountCat);
            var discountNoteSerializer = new GasDiscountXmlNoteSerializer(XmlNamespace, discountCat, new NullLogger<GasDiscountXmlNoteSerializer>());
            var discountManager = new DiscountManager(discountNoteSerializer, noteManager, LookupRepo, DefaultAuthor);

            // Insert discount
            var discount = new GasDiscount
            {
                AuthorId = DefaultAuthor.Id,
                Amount = 0.8m.GetCad(),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true,
                Program = "Petro-Canada Membership"
            };
            _defaultDiscount = discountManager.Create(discount);
            Assert.NotNull(_defaultDiscount);

            // setup gas log xml note serialize
            var schemaStr = File.ReadAllText("GasLog.xsd");
            var catalog = new NoteCatalog { Schema = schemaStr };
            _noteSerializer = new GasLogXmlNoteSerializer(XmlNamespace, catalog, new NullLogger<GasLogXmlNoteSerializer>(), autoManager, discountManager);
            _defaultCar = autoManager.GetEntities().FirstOrDefault();
            Assert.NotNull(_defaultCar);
        }
    }
}