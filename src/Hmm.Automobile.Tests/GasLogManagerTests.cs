using System;
using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerializer;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validation;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.MeasureUnit;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasLogManagerTests : AutoTestFixtureBase
    {
        private IAutoEntityManager<GasLog> _manager;
        private IAutoEntityManager<AutomobileInfo> _autoManager;
        private IAutoEntityManager<GasDiscount> _discountManager;
        private Author _defaultAuthor;

        public GasLogManagerTests()
        {
            SetupDevEnv();
        }

        [Fact]
        public void CanAddGasLog()
        {
            // Arrange
            const string comment = "This is a test gas log";
            var car = _autoManager.GetEntityById(1);
            var discount = _discountManager.GetEntities().FirstOrDefault();
            var gasLog = new GasLog
            {
                Car = car,
                Station = "Costco",
                Gas = Volume.FromLiter(40),
                Price = new Money(40.0),
                Distance = Dimension.FromKilometer(300),
                CurrentMeterReading = Dimension.FromKilometer(12000),
                Discounts = new List<GasDiscountInfo>
                        {
                            new()
                            {
                                Amount = new Money(0.8),
                                Program = discount
                            }
                        },
                Comment = comment
            };
            CurrentTime = new DateTime(2021, 9, 15);

            // Act
            var newGas = _manager.Create(gasLog);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.True(newGas.Id >= 1, "newGas.Id >= 1");
            Assert.NotNull(newGas.Car);
            Assert.NotNull(newGas.Discounts);
            Assert.Equal(_defaultAuthor.Id, newGas.AuthorId);
            Assert.Equal(comment, newGas.Comment);
            Assert.Equal(Dimension.FromKilometer(12000), newGas.CurrentMeterReading);
            Assert.Equal(0.8m.GetCad(), newGas.Discounts.First().Amount);
            Assert.True(newGas.Discounts.Any());
            Assert.True(newGas.Discounts.FirstOrDefault()?.Amount.Amount == 0.8m);
            Assert.Equal(CurrentTime, newGas.CreateDate);
        }

        [Theory]
        [InlineData(250)]
        public void CanUpdateGasLog(int distance)
        {
            // Arrange
            var gas = InsertSampleGasLog();
            Assert.NotNull(gas);
            var orgDistance = gas.Distance;
            var newDistance = Dimension.FromKilometer(distance);
            gas.Distance = newDistance;

            // Act
            var updatedGasLog = _manager.Update(gas);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(updatedGasLog);
            Assert.NotEqual(orgDistance, updatedGasLog.Distance);
            Assert.Equal(newDistance, updatedGasLog.Distance);
        }

        [Fact]
        public void CanNotUpdateGasLogAuthorId()
        {
            // Arrange
            var gas = InsertSampleGasLog();
            Assert.NotNull(gas);
            Assert.NotNull(_defaultAuthor);

            // Act
            var updatedGasLog = _manager.Update(gas);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(updatedGasLog);
            Assert.Equal(_defaultAuthor.Id, updatedGasLog.AuthorId);
        }

        [Fact]
        public void CanFindGasLog()
        {
            // Arrange
            var id = InsertSampleGasLog().Id;

            // Act
            var gasLog = _manager.GetEntityById(id);

            // Assert
            Assert.NotNull(gasLog);
            Assert.Equal("Costco", gasLog.Station);
            Assert.NotNull(gasLog.Car);
            Assert.NotNull(gasLog.Discounts);
            Assert.True(gasLog.Discounts.Any());
            var disc = gasLog.Discounts.FirstOrDefault();
            Assert.NotNull(disc);
            Assert.Equal("Petro-Canada Membership", disc.Program.Program);
        }

        private void SetupDevEnv()
        {
            InsertSeedRecords();

            // setup default author
            _defaultAuthor = AuthorRepository.GetEntities().FirstOrDefault();
            Assert.NotNull(_defaultAuthor);
            var noteManager = new HmmNoteManager(NoteRepository, new NoteValidator(NoteRepository), DateProvider);

            // setup automobile manager
            var autoCat = LookupRepo.GetEntities<NoteCatalog>()
                .FirstOrDefault(c => c.Name == AutomobileConstant.AutoMobileInfoCatalogName);
            Assert.NotNull(autoCat);
            var autoNoteSerializer = new AutomobileXmlNoteSerializer(XmlNamespace, autoCat, new NullLogger<AutomobileXmlNoteSerializer>());
            _autoManager = new AutomobileManager(autoNoteSerializer, noteManager, LookupRepo, _defaultAuthor);

            // setup discount manager
            var discountCat = LookupRepo.GetEntities<NoteCatalog>()
                .FirstOrDefault(c => c.Name == AutomobileConstant.GasDiscountCatalogName);
            Assert.NotNull(discountCat);
            var discountNoteSerializer = new GasDiscountXmlNoteSerializer(XmlNamespace, discountCat, new NullLogger<GasDiscountXmlNoteSerializer>());
            _discountManager = new DiscountManager(discountNoteSerializer, noteManager, LookupRepo, _defaultAuthor);

            // setup gas log manager
            var logCat = LookupRepo.GetEntities<NoteCatalog>()
                .FirstOrDefault(c => c.Name == AutomobileConstant.GasLogCatalogName);
            Assert.NotNull(logCat);
            var gasLogNoteSerializer = new GasLogXmlNoteSerializer(XmlNamespace, logCat, new NullLogger<GasLogXmlNoteSerializer>(), _autoManager, _discountManager);
            _manager = new GasLogManager(gasLogNoteSerializer, noteManager, LookupRepo, _defaultAuthor);

            // Insert car
            var car = new AutomobileInfo
            {
                AuthorId = _defaultAuthor.Id,
                Brand = "AutoBack",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "1234"
            };
            var savedCar = _autoManager.Create(car);
            Assert.NotNull(savedCar);

            // Insert discount
            var discount = new GasDiscount
            {
                AuthorId = _defaultAuthor.Id,
                Amount = 0.8m.GetCad(),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true,
                Program = "Petro-Canada Membership"
            };
            var savedDiscount = _discountManager.Create(discount);
            Assert.NotNull(savedDiscount);
        }

        private GasLog InsertSampleGasLog()
        {
            var car = _autoManager.GetEntities().FirstOrDefault();
            Assert.NotNull(car);
            var discount = _discountManager.GetEntities().FirstOrDefault();
            Assert.NotNull(discount);

            var gasLog = new GasLog
            {
                Car = car,
                Station = "Costco",
                Gas = Volume.FromLiter(40),
                Price = new Money(40.0),
                Distance = Dimension.FromKilometer(300),
                Discounts = new List<GasDiscountInfo>
                        {
                            new()
                            {
                                Amount = new Money(0.8),
                                Program = discount
                            }
                        }
            };

            var newLog = _manager.Create(gasLog);

            return newLog;
        }
    }
}