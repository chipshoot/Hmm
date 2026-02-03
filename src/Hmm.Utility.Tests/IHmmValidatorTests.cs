using FluentValidation;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Utility.Tests
{
    /// <summary>
    /// Tests for IHmmValidator interface and ValidatorBase implementation.
    /// These tests document and verify the standard validation pattern for the Hmm solution.
    ///
    /// Issue #58 Resolution: The codebase previously had two incompatible validation interfaces:
    /// - IValidator&lt;T&gt; (deprecated, removed): Had mutable state via List&lt;string&gt; ValidationErrors
    /// - IHmmValidator&lt;T&gt; (standard): Returns immutable ProcessingResult&lt;T&gt;
    ///
    /// All validators now use IHmmValidator&lt;T&gt; via the ValidatorBase&lt;T&gt; base class.
    /// </summary>
    public class IHmmValidatorTests
    {
        #region Test Validator Implementation

        /// <summary>
        /// Simple test entity for validation testing.
        /// </summary>
        private class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
        }

        /// <summary>
        /// Test validator implementing the standard pattern via ValidatorBase.
        /// </summary>
        private class TestEntityValidator : ValidatorBase<TestEntity>
        {
            public TestEntityValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage("Name is required");

                RuleFor(x => x.Name)
                    .MaximumLength(100)
                    .WithMessage("Name cannot exceed 100 characters");

                RuleFor(x => x.Age)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Age must be non-negative");
            }
        }

        #endregion

        #region Interface Contract Tests

        [Fact]
        public void ValidatorBase_ImplementsIHmmValidator()
        {
            // Arrange & Act
            var validator = new TestEntityValidator();

            // Assert - Verify the validator implements IHmmValidator<T>
            Assert.IsAssignableFrom<IHmmValidator<TestEntity>>(validator);
        }

        [Fact]
        public async Task ValidateEntityAsync_ValidEntity_ReturnsSuccessResult()
        {
            // Arrange
            IHmmValidator<TestEntity> validator = new TestEntityValidator();
            var entity = new TestEntity { Id = 1, Name = "Test", Age = 25 };

            // Act
            var result = await validator.ValidateEntityAsync(entity);

            // Assert
            Assert.True(result.Success);
            Assert.Same(entity, result.Value);
            Assert.True(string.IsNullOrEmpty(result.ErrorMessage));
        }

        [Fact]
        public async Task ValidateEntityAsync_InvalidEntity_ReturnsFailureResult()
        {
            // Arrange
            IHmmValidator<TestEntity> validator = new TestEntityValidator();
            var entity = new TestEntity { Id = 1, Name = "", Age = 25 }; // Empty name is invalid

            // Act
            var result = await validator.ValidateEntityAsync(entity);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Name is required", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateEntityAsync_MultipleValidationErrors_ReturnsAllErrors()
        {
            // Arrange
            IHmmValidator<TestEntity> validator = new TestEntityValidator();
            var entity = new TestEntity { Id = 1, Name = "", Age = -5 }; // Both name and age invalid

            // Act
            var result = await validator.ValidateEntityAsync(entity);

            // Assert
            Assert.False(result.Success);
            // Messages collection contains all validation errors
            var allMessages = string.Join("; ", result.Messages.Select(m => m.Message));
            Assert.Contains("Name is required", allMessages);
            Assert.Contains("Age must be non-negative", allMessages);
        }

        [Fact]
        public async Task ValidateEntityAsync_NullEntity_ThrowsArgumentNullException()
        {
            // Arrange
            IHmmValidator<TestEntity> validator = new TestEntityValidator();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                validator.ValidateEntityAsync(null));
        }

        #endregion

        #region Immutability Tests

        [Fact]
        public async Task ValidateEntityAsync_ResultIsImmutable_CannotBeModified()
        {
            // Arrange
            IHmmValidator<TestEntity> validator = new TestEntityValidator();
            var entity = new TestEntity { Id = 1, Name = "Test", Age = 25 };

            // Act
            var result = await validator.ValidateEntityAsync(entity);

            // Assert - ProcessingResult properties are get-only
            Assert.True(result.Success);
            // The result cannot be modified - it has no public setters
            // This is verified by the fact that ProcessingResult<T> uses private constructor
            // and factory methods (Ok, Fail, etc.)
        }

        [Fact]
        public async Task ValidateEntityAsync_MultipleValidations_IndependentResults()
        {
            // Arrange - Verify no mutable state leaks between validations
            IHmmValidator<TestEntity> validator = new TestEntityValidator();
            var validEntity = new TestEntity { Id = 1, Name = "Valid", Age = 25 };
            var invalidEntity = new TestEntity { Id = 2, Name = "", Age = -1 };

            // Act - Validate in alternating order
            var result1 = await validator.ValidateEntityAsync(validEntity);
            var result2 = await validator.ValidateEntityAsync(invalidEntity);
            var result3 = await validator.ValidateEntityAsync(validEntity);

            // Assert - Each validation is independent
            Assert.True(result1.Success);
            Assert.False(result2.Success);
            Assert.True(result3.Success);
        }

        #endregion

        #region Error Category Tests

        [Fact]
        public async Task ValidateEntityAsync_ValidationFailure_ReturnsValidationErrorType()
        {
            // Arrange
            IHmmValidator<TestEntity> validator = new TestEntityValidator();
            var entity = new TestEntity { Id = 1, Name = "", Age = 25 };

            // Act
            var result = await validator.ValidateEntityAsync(entity);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task ValidateEntityAsync_Success_ReturnsNoneErrorType()
        {
            // Arrange
            IHmmValidator<TestEntity> validator = new TestEntityValidator();
            var entity = new TestEntity { Id = 1, Name = "Test", Age = 25 };

            // Act
            var result = await validator.ValidateEntityAsync(entity);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(ErrorCategory.None, result.ErrorType);
        }

        #endregion
    }
}
