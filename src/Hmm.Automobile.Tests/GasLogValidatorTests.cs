using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using Hmm.Utility.Currency;
using Hmm.Utility.MeasureUnit;
using Hmm.Utility.Validation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasLogValidatorTests : AutoTestFixtureBase
    {
        private IHmmValidator<GasLog> _validator;
        private int _authorId;

        public GasLogValidatorTests()
        {
            SetupTestEnv();
        }

        #region Valid Entity Tests

        [Fact]
        public async Task ValidGasLog_CanPassValidation()
        {
            // Arrange
            var log = CreateValidGasLog();

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task ValidGasLog_WithDiscounts_CanPassValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Discounts = new List<GasDiscountInfo>
            {
                new()
                {
                    Program = new GasDiscount
                    {
                        AuthorId = _authorId,
                        Program = "Petro-Canada membership",
                        Amount = 0.08m.GetCad(),
                        DiscountType = GasDiscountType.PerLiter
                    },
                    Amount = 3.20m.GetCad()
                }
            };

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region AuthorId Validation Tests

        [Fact]
        public async Task GasLog_MustHaveValid_Author()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.AuthorId = 0; // Invalid

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_WithNonExistentAuthor_FailsValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.AuthorId = 99999; // Non-existent

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region Date Validation Tests

        [Fact]
        public async Task GasLog_MustHaveValid_Date_NotMinValue()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Date = DateTime.MinValue; // Invalid

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_MustHaveValid_Date_NotFuture()
        {
            // Arrange
            CurrentTime = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var log = CreateValidGasLog();
            log.Date = new DateTime(2024, 12, 25); // Future date

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_WithTodayDate_PassesValidation()
        {
            // Arrange
            CurrentTime = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var log = CreateValidGasLog();
            log.Date = CurrentTime;

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task GasLog_WithPastDate_PassesValidation()
        {
            // Arrange
            CurrentTime = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var log = CreateValidGasLog();
            log.Date = new DateTime(2024, 1, 1);

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region AutomobileId Validation Tests

        [Fact]
        public async Task GasLog_MustHaveValid_AutomobileId()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.AutomobileId = 0; // Invalid

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region Distance and Odometer Validation Tests

        [Theory]
        [InlineData(100, 200, true)]  // Distance < Odometer - valid
        [InlineData(100, 100, true)]  // Distance == Odometer - valid
        [InlineData(200, 100, false)] // Distance > Odometer - invalid
        [InlineData(0, 100, false)]   // Zero distance - invalid
        [InlineData(100, 0, false)]   // Zero odometer - invalid
        public async Task GasLog_MustHaveValid_DistanceAndOdometer(int distance, int odometer, bool expectValid)
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Distance = ((double)distance).GetKilometer();
            log.Odometer = ((double)odometer).GetKilometer();

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.Equal(expectValid, result.Success);
        }

        [Fact]
        public async Task GasLog_WithNegativeDistance_FailsValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Distance = new Dimension(-100, DimensionUnit.Kilometre);

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_WithNegativeOdometer_FailsValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Odometer = new Dimension(-1000, DimensionUnit.Kilometre);

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region Fuel Validation Tests

        [Fact]
        public async Task GasLog_MustHaveValid_Fuel()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Fuel = 0d.GetLiter(); // Zero - invalid

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_WithNegativeFuel_FailsValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Fuel = new Volume(-40, VolumeUnit.Liter);

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_WithPositiveFuel_PassesValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Fuel = 50d.GetLiter();

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region UnitPrice Validation Tests

        [Theory]
        [InlineData(0, true)]
        [InlineData(10, true)]
        [InlineData(-10, false)]
        public async Task GasLog_MustHaveValid_UnitPrice(decimal price, bool expectValid)
        {
            // Arrange
            var log = CreateValidGasLog();
            log.UnitPrice = price.GetCad();

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.Equal(expectValid, result.Success);
        }

        [Fact]
        public async Task GasLog_WithNullUnitPrice_FailsValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.UnitPrice = null;

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region Station Validation Tests

        [Fact]
        public async Task GasLog_MustHaveValid_Station()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Station = null; // Invalid

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_MustHaveValid_StationName()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Station = new GasStation { Name = "", City = "Vancouver", Country = "Canada" }; // Empty name - invalid

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_WithStationNameTooLong_FailsValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Station = new GasStation { Name = new string('A', 1001), City = "Vancouver", Country = "Canada" }; // 1001 chars - exceeds max

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_WithStationNameAt1000Chars_PassesValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Station = new GasStation { Name = new string('A', 1000), City = "Vancouver", Country = "Canada" };

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region CreateDate Validation Tests

        [Fact]
        public async Task GasLog_MustHaveValid_CreateDate_NotMinValue()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.CreateDate = DateTime.MinValue; // Invalid

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_MustHaveValid_CreateDate_NotFuture()
        {
            // Arrange
            CurrentTime = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var log = CreateValidGasLog();
            log.CreateDate = new DateTime(2025, 1, 1); // Future date

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region Discounts Validation Tests

        [Fact]
        public async Task GasLog_WithInvalidDiscount_FailsValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Discounts = new List<GasDiscountInfo>
            {
                new()
                {
                    Program = null, // Invalid - null program
                    Amount = 3.20m.GetCad()
                }
            };

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_WithInvalidDiscountAmount_FailsValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Discounts = new List<GasDiscountInfo>
            {
                new()
                {
                    Program = new GasDiscount
                    {
                        AuthorId = _authorId,
                        Program = "Test",
                        Amount = 0.05m.GetCad(),
                        DiscountType = GasDiscountType.PerLiter
                    },
                    Amount = new Money(-1m, CurrencyCodeType.Cad) // Invalid - negative
                }
            };

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task GasLog_WithEmptyDiscountList_PassesValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Discounts = new List<GasDiscountInfo>();

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task GasLog_WithMultipleValidDiscounts_PassesValidation()
        {
            // Arrange
            var log = CreateValidGasLog();
            log.Discounts = new List<GasDiscountInfo>
            {
                new()
                {
                    Program = new GasDiscount
                    {
                        AuthorId = _authorId,
                        Program = "Discount 1",
                        Amount = 0.05m.GetCad(),
                        DiscountType = GasDiscountType.PerLiter
                    },
                    Amount = 2.00m.GetCad()
                },
                new()
                {
                    Program = new GasDiscount
                    {
                        AuthorId = _authorId,
                        Program = "Discount 2",
                        Amount = 0.03m.GetCad(),
                        DiscountType = GasDiscountType.PerLiter
                    },
                    Amount = 1.20m.GetCad()
                }
            };

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region Multiple Validation Errors Tests

        [Fact]
        public async Task GasLog_WithMultipleErrors_FailsValidation()
        {
            // Arrange
            var log = new GasLog
            {
                AuthorId = 0, // Invalid
                Date = DateTime.MinValue, // Invalid
                AutomobileId = 0, // Invalid
                Distance = 0d.GetKilometer(), // Invalid
                Odometer = 0d.GetKilometer(), // Invalid
                Fuel = 0d.GetLiter(), // Invalid
                UnitPrice = new Money(-1m, CurrencyCodeType.Cad), // Invalid
                Station = null, // Invalid
                CreateDate = DateTime.MinValue // Invalid
            };

            // Act
            var result = await _validator.ValidateEntityAsync(log);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        private GasLog CreateValidGasLog()
        {
            return new GasLog
            {
                AuthorId = _authorId,
                Date = CurrentTime,
                AutomobileId = 1,
                Distance = 340d.GetKilometer(),
                Odometer = 30000d.GetKilometer(),
                Fuel = 40d.GetLiter(),
                UnitPrice = 1.34m.GetCad(),
                TotalPrice = 53.60m.GetCad(),
                Station = new GasStation
                {
                    Name = "Costco",
                    Address = "123 Main St",
                    City = "Vancouver",
                    Country = "Canada",
                    State = "BC"
                },
                CreateDate = CurrentTime,
                Discounts = new List<GasDiscountInfo>(),
                Comment = "Test gas log"
            };
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new GasLogValidator(LookupRepository, DateProvider);
            _authorId = TestDefaultAuthor.Id;
        }
    }
}
