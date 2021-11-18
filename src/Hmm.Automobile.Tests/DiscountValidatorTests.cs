using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using Hmm.Utility.Currency;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class DiscountValidatorTests : AutoTestFixtureBase
    {
        private IHmmValidator<GasDiscount> _validator;
        private Guid _authorId;

        public DiscountValidatorTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void ValidDiscountCanPassValidation()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = "Petro-Canada membership",
                Amount = new Money(0.8),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(discount, processResult);

            // Assert
            Assert.True(result);
            Assert.Empty(processResult.MessageList);
        }

        [Fact]
        public void DiscountMustHaveValid_Author()
        {
            // Arrange
            var discount = new GasDiscount
            {
                Program = "Petro-Canada membership",
                Amount = new Money(0.8),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(discount, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Fact]
        public void DiscountMustHaveValid_Program()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = "",
                Amount = new Money(0.8),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(discount, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Fact]
        public void DiscountMustHaveValid_Amount()
        {
            // Arrange
            var discount = new GasDiscount
            {
                AuthorId = _authorId,
                Program = "Petro-Canada membership",
                Amount = new Money(-1),
                DiscountType = GasDiscountType.PerLiter
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(discount, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new GasDiscountValidator(LookupRepo);
            _authorId = ApplicationRegister.DefaultAuthor.Id;
        }
    }
}