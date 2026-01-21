using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using Hmm.Utility.Currency;
using Hmm.Utility.Validation;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasDiscountInfoValidatorTests : AutoTestFixtureBase
    {
        private IHmmValidator<GasDiscountInfo> _validator;
        private int _authorId;

        public GasDiscountInfoValidatorTests()
        {
            SetupTestEnv();
        }

        #region Valid Entity Tests

        [Fact]
        public async Task ValidDiscountInfo_CanPassValidation()
        {
            // Arrange
            var discountInfo = new GasDiscountInfo
            {
                Program = new GasDiscount
                {
                    AuthorId = _authorId,
                    Program = "Petro-Canada membership",
                    Amount = new Money(0.8m, CurrencyCodeType.Cad),
                    DiscountType = GasDiscountType.PerLiter
                },
                Amount = new Money(3.20m, CurrencyCodeType.Cad)
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discountInfo);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task ValidDiscountInfo_WithZeroAmount_CanPassValidation()
        {
            // Arrange
            var discountInfo = new GasDiscountInfo
            {
                Program = new GasDiscount
                {
                    AuthorId = _authorId,
                    Program = "Free membership",
                    Amount = new Money(0m, CurrencyCodeType.Cad),
                    DiscountType = GasDiscountType.Flat
                },
                Amount = new Money(0m, CurrencyCodeType.Cad)
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discountInfo);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region Amount Validation Tests

        [Fact]
        public async Task DiscountInfo_MustHaveValid_Amount()
        {
            // Arrange
            var discountInfo = new GasDiscountInfo
            {
                Program = new GasDiscount
                {
                    AuthorId = _authorId,
                    Program = "Petro-Canada membership",
                    Amount = new Money(0.8m, CurrencyCodeType.Cad),
                    DiscountType = GasDiscountType.PerLiter
                },
                Amount = new Money(-1m, CurrencyCodeType.Cad) // Negative - invalid
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discountInfo);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task DiscountInfo_WithNullAmount_FailsValidation()
        {
            // Arrange
            var discountInfo = new GasDiscountInfo
            {
                Program = new GasDiscount
                {
                    AuthorId = _authorId,
                    Program = "Petro-Canada membership",
                    Amount = new Money(0.8m, CurrencyCodeType.Cad),
                    DiscountType = GasDiscountType.PerLiter
                },
                Amount = null // Null - invalid
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discountInfo);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(1.0)]
        [InlineData(50.0)]
        public async Task DiscountInfo_WithPositiveAmount_PassesValidation(decimal amount)
        {
            // Arrange
            var discountInfo = new GasDiscountInfo
            {
                Program = new GasDiscount
                {
                    AuthorId = _authorId,
                    Program = "Test Program",
                    Amount = new Money(0.8m, CurrencyCodeType.Cad),
                    DiscountType = GasDiscountType.PerLiter
                },
                Amount = new Money(amount, CurrencyCodeType.Cad)
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discountInfo);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region Program Validation Tests

        [Fact]
        public async Task DiscountInfo_MustHaveValid_Program()
        {
            // Arrange
            var discountInfo = new GasDiscountInfo
            {
                Program = null, // Null - invalid
                Amount = new Money(3.20m, CurrencyCodeType.Cad)
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discountInfo);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task DiscountInfo_WithValidProgram_PassesValidation()
        {
            // Arrange
            var discountInfo = new GasDiscountInfo
            {
                Program = new GasDiscount
                {
                    AuthorId = _authorId,
                    Program = "Shell V-Power Rewards",
                    Amount = new Money(0.05m, CurrencyCodeType.Cad),
                    DiscountType = GasDiscountType.PerLiter
                },
                Amount = new Money(2.50m, CurrencyCodeType.Cad)
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discountInfo);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region Multiple Validation Errors Tests

        [Fact]
        public async Task DiscountInfo_WithMultipleErrors_FailsValidation()
        {
            // Arrange
            var discountInfo = new GasDiscountInfo
            {
                Program = null, // Invalid
                Amount = new Money(-1m, CurrencyCodeType.Cad) // Invalid
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discountInfo);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new GasDiscountInfoValidator(LookupRepository);
            _authorId = TestDefaultAuthor.Id;
        }
    }
}
