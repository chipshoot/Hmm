using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerializer;
using Hmm.Automobile.Validator;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Currency;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Hmm.Utility.Dal.Query;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasLogManagerTests : AutoTestFixtureBase
    {
        private IGasLogManager _manager;
        private IAutoEntityManager<AutomobileInfo> _autoManager;
        private IAutoEntityManager<GasDiscount> _discountManager;
        private Author _author;

        public GasLogManagerTests()
        {
            SetupDevEnv();
        }

        [Fact]
        public void CanAddGasLogHistory()
        {
            // Arrange
            const string comment = "This is a test gas log";
            var car = _autoManager.GetEntityById(1);
            var discount = _discountManager.GetEntities().FirstOrDefault();
            var logDate = new DateTime(2019, 12, 25);
            var gasLog = new GasLog
            {
                Date = logDate,
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
            var newGas = _manager.LogHistory(gasLog);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.True(newGas.Id >= 1, "newGas.Id >= 1");
            Assert.NotNull(newGas.Car);
            Assert.NotNull(newGas.Discounts);
            Assert.Equal(_author.Id, newGas.AuthorId);
            Assert.Equal(comment, newGas.Comment);
            Assert.Equal(Dimension.FromKilometer(12000), newGas.CurrentMeterReading);
            Assert.Equal(0.8m.GetCad(), newGas.Discounts.First().Amount);
            Assert.True(newGas.Discounts.Any());
            Assert.True(newGas.Discounts.FirstOrDefault()?.Amount.Amount == 0.8m);
            Assert.Equal(logDate, newGas.Date);
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
            Assert.NotNull(_author);

            // Act
            var updatedGasLog = _manager.Update(gas);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.NotNull(updatedGasLog);
            Assert.Equal(_author.Id, updatedGasLog.AuthorId);
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

        [Fact]
        public void CanCreateValidGasLog()
        {
            // Arrange
            const int newDistance = 12000;
            const string comment = "This is a test gas log";
            var car = _autoManager.GetEntityById(1);
            car.MeterReading = 11700;
            var discount = _discountManager.GetEntities(new ResourceCollectionParameters()).FirstOrDefault();
            var logDate = new DateTime(2019, 12, 25);
            var gasLog = new GasLog
            {
                Date = logDate,
                Car = car,
                Station = "Costco",
                Gas = Volume.FromLiter(40),
                Price = new Money(40.0),
                Distance = Dimension.FromKilometer(300),
                CurrentMeterReading = Dimension.FromKilometer(newDistance),
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
            var updCar = _autoManager.GetEntityById(1);

            // Assert
            Assert.True(_manager.ProcessResult.Success);
            Assert.True(newGas.Id >= 1, "newGas.Id >= 1");
            Assert.NotNull(newGas.Car);
            Assert.NotNull(newGas.Discounts);
            Assert.Equal(_author.Id, newGas.AuthorId);
            Assert.Equal(comment, newGas.Comment);
            Assert.Equal(Dimension.FromKilometer(12000), newGas.CurrentMeterReading);
            Assert.Equal(0.8m.GetCad(), newGas.Discounts.First().Amount);
            Assert.True(newGas.Discounts.Any());
            Assert.True(newGas.Discounts.FirstOrDefault()?.Amount.Amount == 0.8m);
            Assert.Equal(logDate, newGas.Date);
            Assert.Equal(CurrentTime, newGas.CreateDate);
            Assert.Equal(updCar.MeterReading, newDistance);
        }

        [Theory]
        [InlineData(100, 1000, -1000, false)]
        [InlineData(100, 1000, 2000, false)]
        [InlineData(100, 1000, 1000, false)]
        [InlineData(100, 3000, 1000, true)]
        public void CannotCreateInvalidGasLog(int curDistance, int logMeterReading, int autoMeterReading, bool expectProcessingSuccess)
        {
            // Arrange
            const string comment = "This is a test gas log";
            var car = _autoManager.GetEntityById(1);
            car.MeterReading = autoMeterReading;
            _autoManager.Update(car);
            var discount = _discountManager.GetEntities(new ResourceCollectionParameters()).FirstOrDefault();
            var logDate = new DateTime(2019, 12, 25);
            var gasLog = new GasLog
            {
                Date = logDate,
                Car = car,
                Station = "Costco",
                Gas = Volume.FromLiter(40),
                Price = new Money(40.0),
                Distance = Dimension.FromKilometer(curDistance),
                CurrentMeterReading = Dimension.FromKilometer(logMeterReading),
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
            Assert.Equal(expectProcessingSuccess, _manager.ProcessResult.Success);
            if (expectProcessingSuccess)
            {
                Assert.NotNull(newGas);
            }
            else
            {
                Assert.Null(newGas);
                Assert.NotEmpty(_manager.ProcessResult.MessageList);
                Assert.True(_manager.ProcessResult.HasError);
                var error = _manager.ProcessResult.MessageList.FirstOrDefault();
                Assert.NotNull(error);
                Assert.False(string.IsNullOrEmpty(error.Message));
                Assert.Equal(MessageType.Error, error.Type);
            }
        }

        [Fact]
        public void CanDeleteGasLog()
        {
            // Arrange
            const string comment = "This is a test gas log";
            var car = _autoManager.GetEntityById(1);
            var discount = _discountManager.GetEntities(new ResourceCollectionParameters()).FirstOrDefault();
            var logDate = new DateTime(2019, 12, 25);
            var gasLog = new GasLog
            {
                Date = logDate,
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
            var newGas = _manager.LogHistory(gasLog);
            newGas.IsDeleted = true;

            // Act
            var updGas = _manager.Update(newGas);
            var log = _manager.GetEntityById(newGas.Id);

            // Assert
            Assert.Null(updGas);
            Assert.Null(log);
        }

        private void SetupDevEnv()
        {
            InsertSeedRecords();
            _author = ApplicationRegister.DefaultAuthor;

            var noteManager = new HmmNoteManager(NoteRepository, new NoteValidator(NoteRepository), DateProvider);

            // setup automobile manager
            var autoCat = LookupRepo.GetEntities<NoteCatalog>()
                .FirstOrDefault(c => c.Name == AutomobileConstant.AutoMobileInfoCatalogName);
            Assert.NotNull(autoCat);
            var autoNoteSerializer = new AutomobileXmlNoteSerializer(Application, new NullLogger<AutomobileInfo>(), LookupRepo);
            _autoManager = new AutomobileManager(autoNoteSerializer, new AutomobileValidator(LookupRepo), noteManager, LookupRepo);

            // setup discount manager
            var discountCat = LookupRepo.GetEntities<NoteCatalog>()
                .FirstOrDefault(c => c.Name == AutomobileConstant.GasDiscountCatalogName);
            Assert.NotNull(discountCat);
            var discountNoteSerializer = new GasDiscountXmlNoteSerializer(Application, new NullLogger<GasDiscount>(), LookupRepo);
            _discountManager = new DiscountManager(discountNoteSerializer, new GasDiscountValidator(LookupRepo), noteManager, LookupRepo);

            // setup gas log manager
            var logCat = LookupRepo.GetEntities<NoteCatalog>()
                .FirstOrDefault(c => c.Name == AutomobileConstant.GasLogCatalogName);
            Assert.NotNull(logCat);
            var gasLogNoteSerializer = new GasLogXmlNoteSerializer(Application, new NullLogger<GasLog>(), _autoManager, _discountManager, LookupRepo);
            _manager = new GasLogManager(gasLogNoteSerializer, new GasLogValidator(LookupRepo, DateProvider), noteManager, _autoManager, LookupRepo, DateProvider);

            // Insert car
            var car = new AutomobileInfo
            {
                Brand = "AutoBack",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "1234",
                Color = "Blue",
                Plate = "BCTT208"
            };
            var savedCar = _autoManager.Create(car);
            Assert.NotNull(savedCar);

            // Insert discount
            var discount = new GasDiscount
            {
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
            var car = _autoManager.GetEntities(new ResourceCollectionParameters()).FirstOrDefault();
            Assert.NotNull(car);
            var discount = _discountManager.GetEntities(new ResourceCollectionParameters()).FirstOrDefault();
            Assert.NotNull(discount);

            var gasLog = new GasLog
            {
                Date = new DateTime(2021, 8, 9),
                Car = car,
                Station = "Costco",
                Gas = Volume.FromLiter(40),
                Price = new Money(40.0),
                Distance = Dimension.FromKilometer(300),
                CurrentMeterReading = Dimension.FromKilometer(23500),
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