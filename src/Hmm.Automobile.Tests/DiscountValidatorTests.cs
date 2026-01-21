using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using Hmm.Utility.Currency;
using Hmm.Utility.Validation;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class DiscountValidatorTests : AutoTestFixtureBase
    {
        private IHmmValidator<GasDiscount> _validator;
        private int _authorId;

        public DiscountValidatorTests()
        {
            SetupTestEnv();
        }

        #region Valid Entity Tests

        [Fact]
        public async Task ValidDiscount_CanPassValidation()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = "Petro-Canada membership",
                Amount = new Money(0.8m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter,
                IsActive = true
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task ValidDiscount_WithZeroAmount_CanPassValidation()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = "Free membership",
                Amount = new Money(0m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.Flat
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region AuthorId Validation Tests

        [Fact]
        public async Task Discount_MustHaveValid_Author()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = 0, // Invalid - zero
                Program = "Petro-Canada membership",
                Amount = new Money(0.8m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Discount_WithNonExistentAuthor_FailsValidation()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = 99999, // Non-existent author
                Program = "Petro-Canada membership",
                Amount = new Money(0.8m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Discount_WithNegativeAuthorId_FailsValidation()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = -1, // Negative
                Program = "Petro-Canada membership",
                Amount = new Money(0.8m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region Program Validation Tests

        [Fact]
        public async Task Discount_MustHaveValid_Program()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = "", // Empty - invalid
                Amount = new Money(0.8m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Discount_WithNullProgram_FailsValidation()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = null, // Null - invalid
                Amount = new Money(0.8m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Discount_WithWhitespaceProgram_FailsValidation()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = "   ", // Whitespace - invalid
                Amount = new Money(0.8m, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region Amount Validation Tests

        [Fact]
        public async Task Discount_MustHaveValid_Amount()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = "Petro-Canada membership",
                Amount = new Money(-1m, CurrencyCodeType.Cad), // Negative - invalid
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Discount_WithNullAmount_FailsValidation()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = "Petro-Canada membership",
                Amount = null, // Null - invalid
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(1.0)]
        [InlineData(100.0)]
        public async Task Discount_WithPositiveAmount_PassesValidation(decimal amount)
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = "Test Program",
                Amount = new Money(amount, CurrencyCodeType.Cad),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region Multiple Validation Errors Tests

        [Fact]
        public async Task Discount_WithMultipleErrors_FailsValidation()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = 0, // Invalid
                Program = "", // Invalid
                Amount = new Money(-1m, CurrencyCodeType.Cad), // Invalid
                DiscountType = GasDiscountType.PerLiter
            };

            // Act
            var result = await _validator.ValidateEntityAsync(discount);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new GasDiscountValidator(LookupRepository);
            _authorId = TestDefaultAuthor.Id;
        }
    }
}
