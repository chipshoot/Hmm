using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasLogValidatorTests : AutoTestFixtureBase
    {
        private IHmmValidator<GasLog> _validator;
        private Guid _authorId;

        public GasLogValidatorTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void ValidGasLogCanPassValidation()
        {
            // Arrange
            var log = new GasLog
            {
                AuthorId = _authorId,
                Date = DateProvider.UtcNow,
                Car = new AutomobileInfo(),
                CurrentMeterReading = 30000.GetKilometer(),
                Distance = 340.GetKilometer(),
                Gas = 40d.GetLiter(),
                Price = 1.34m.GetCad(),
                Station = "Costco",
                CreateDate = DateProvider.UtcNow,
                Discounts = new List<GasDiscountInfo>
                {
                    new()
                    {
                        Program = new GasDiscount{Amount = 0.8m.GetCad(), Program = "Petro-Canada membership", DiscountType = GasDiscountType.PerLiter, AuthorId = _authorId},
                        Amount = 0.8m.GetCad()
                    }
                },
                Comment = "Test gas log",
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(log, processResult);

            // Assert
            Assert.True(result);
            Assert.Empty(processResult.MessageList);
        }

        [Fact]
        public void GasLogMustHaveValid_Author()
        {
            // Arrange
            var log = new GasLog
            {
                Date = DateProvider.UtcNow,
                Car = new AutomobileInfo(),
                CurrentMeterReading = 30000.GetKilometer(),
                Distance = 340.GetKilometer(),
                Gas = 40d.GetLiter(),
                Price = 1.34m.GetCad(),
                Station = "Costco",
                CreateDate = DateProvider.UtcNow,
                Discounts = new List<GasDiscountInfo>(),
                Comment = "Test gas log"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(log, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Theory]
        [InlineData("0001-01-01")]
        [InlineData("2021-11-20")]
        public void GasLogMustHaveValid_Date(string date)
        {
            // Arrange
            CurrentTime = new DateTime(2021, 9, 9);
            var logDate = DateTime.Parse(date);
            var log = new GasLog
            {
                AuthorId = _authorId,
                Date = logDate,
                Car = new AutomobileInfo(),
                CurrentMeterReading = 30000.GetKilometer(),
                Distance = 340.GetKilometer(),
                Gas = 40d.GetLiter(),
                Price = 1.34m.GetCad(),
                Station = "Costco",
                CreateDate = DateProvider.UtcNow,
                Discounts = new List<GasDiscountInfo>(),
                Comment = "Test gas log"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(log, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Fact]
        public void GasLogMustHaveValid_Automobile()
        {
            // Arrange
            var log = new GasLog
            {
                AuthorId = _authorId,
                Date = DateProvider.UtcNow,
                Car = null,
                CurrentMeterReading = 30000.GetKilometer(),
                Distance = 340.GetKilometer(),
                Gas = 40d.GetLiter(),
                Price = 1.34m.GetCad(),
                Station = "Costco",
                CreateDate = DateProvider.UtcNow,
                Discounts = new List<GasDiscountInfo>(),
                Comment = "Test gas log"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(log, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Theory]
        [InlineData(100, 200, true, 0)]
        [InlineData(100, 100, true, 0)]
        [InlineData(100, 50, false, 2)]
        [InlineData(0, 50, false, 2)]
        public void GasLogMustHaveValid_Distance(int distance, int meterReading, bool exceptResult, int errorCount)
        {
            // Arrange
            var log = new GasLog
            {
                AuthorId = _authorId,
                Date = DateProvider.UtcNow,
                Car = new AutomobileInfo(),
                Distance = distance.GetKilometer(),
                CurrentMeterReading = meterReading.GetKilometer(),
                Gas = 40d.GetLiter(),
                Price = 1.34m.GetCad(),
                Station = "Costco",
                CreateDate = DateProvider.UtcNow,
                Discounts = new List<GasDiscountInfo>(),
                Comment = "Test gas log"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(log, processResult);

            // Assert
            Assert.Equal(exceptResult, result);
            Assert.Equal(errorCount, processResult.MessageList.Count);
        }

        [Fact]
        public void GasLogMustHaveValid_Gas()
        {
            // Arrange
            var log = new GasLog
            {
                AuthorId = _authorId,
                Date = DateProvider.UtcNow,
                Car = new AutomobileInfo(),
                Distance = 100.GetKilometer(),
                CurrentMeterReading = 1000.GetKilometer(),
                Gas = 0d.GetLiter(),
                Price = 1.34m.GetCad(),
                Station = "Costco",
                CreateDate = DateProvider.UtcNow,
                Discounts = new List<GasDiscountInfo>(),
                Comment = "Test gas log"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(log, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(10, true)]
        [InlineData(-10, false)]
        public void GasLogMustHaveValid_Price(decimal price, bool exceptResult)
        {
            // Arrange
            var log = new GasLog
            {
                AuthorId = _authorId,
                Date = DateProvider.UtcNow,
                Car = new AutomobileInfo(),
                Distance = 100.GetKilometer(),
                CurrentMeterReading = 1000.GetKilometer(),
                Gas = 30d.GetLiter(),
                Price = price.GetCad(),
                Station = "Costco",
                CreateDate = DateProvider.UtcNow,
                Discounts = new List<GasDiscountInfo>(),
                Comment = "Test gas log"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(log, processResult);

            // Assert
            Assert.Equal(exceptResult, result);
            if (result)
            {
                Assert.Empty(processResult.MessageList);
            }
            else
            {
                Assert.Single(processResult.MessageList);
            }
        }

        [Fact]
        public void GasLogMustHaveValid_Station()
        {
            // Arrange
            var log = new GasLog
            {
                AuthorId = _authorId,
                Date = DateProvider.UtcNow,
                Car = new AutomobileInfo(),
                CurrentMeterReading = 30000.GetKilometer(),
                Distance = 340.GetKilometer(),
                Gas = 40d.GetLiter(),
                Price = 1.34m.GetCad(),
                Station = "",
                CreateDate = DateProvider.UtcNow,
                Discounts = new List<GasDiscountInfo>
                {
                    new()
                    {
                        Program = new GasDiscount{Amount = 0.8m.GetCad(), Program = "Petro-Canada membership", DiscountType = GasDiscountType.PerLiter, AuthorId = _authorId},
                        Amount = 0.8m.GetCad()
                    }
                },
                Comment = "Test gas log",
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(log, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new GasLogValidator(LookupRepo, DateProvider);
            _authorId = ApplicationRegister.DefaultAuthor.Id;
        }
    }
}