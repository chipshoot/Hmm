using Hmm.Utility.Misc;
using Xunit;
using System;

namespace Hmm.Utility.Tests
{
    public class ProcessingResultTests
    {
        [Theory]
        [InlineData("This is Info", MessageType.Info, true, false, false, false)]
        [InlineData("This is Warning", MessageType.Warning, false, true, false, false)]
        [InlineData("This is Error", MessageType.Error, false, false, true, false)]
        [InlineData("This is Fatal", MessageType.Fatal, false, false, true, true)]
        public void Can_Get_Right_Result_Flag(string message, MessageType messageType, bool hasInfo, bool hasWarning, bool hasError, bool hasFatal)
        {
            // Arrange & Act
            ProcessingResult<Unit> result = messageType switch
            {
                MessageType.Info => ProcessingResult<Unit>.Ok(Unit.Value).WithInfo(message),
                MessageType.Warning => ProcessingResult<Unit>.Ok(Unit.Value).WithWarning(message),
                MessageType.Error => ProcessingResult<Unit>.Fail(message),
                MessageType.Fatal => ProcessingResult<Unit>.Fail(message),
                _ => throw new ArgumentException("Invalid message type")
            };

            // Assert
            Assert.Equal(hasInfo, result.HasInfo);
            Assert.Equal(hasWarning, result.HasWarning);
            Assert.Equal(hasError, result.HasError);
            Assert.Equal(hasFatal, result.HasFatal);
        }

        [Fact]
        public void Ok_CreatesSuccessfulResult_WithValue()
        {
            // Arrange & Act
            var result = ProcessingResult<int>.Ok(42);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(42, result.Value);
            Assert.Empty(result.Messages);
            Assert.Equal(ErrorCategory.None, result.ErrorType);
        }

        [Fact]
        public void Ok_CreatesSuccessfulResult_WithValueAndInfoMessages()
        {
            // Arrange & Act
            var result = ProcessingResult<string>.Ok("test", "Info 1", "Info 2");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("test", result.Value);
            Assert.Equal(2, result.Messages.Count);
            Assert.All(result.Messages, m => Assert.Equal(MessageType.Info, m.Type));
            Assert.True(result.HasInfo);
        }

        [Fact]
        public void EmptyOk_CreatesSuccessfulResult_WithNoValue()
        {
            // Arrange & Act
            var result = ProcessingResult<string>.EmptyOk("No data found");

            // Assert
            Assert.True(result.Success);
            Assert.Null(result.Value);
            Assert.True(result.IsNotFound);
            Assert.Single(result.Messages);
            Assert.Equal("No data found", result.Messages[0].Message);
            Assert.Equal(MessageType.Info, result.Messages[0].Type);
        }

        [Fact]
        public void NotFound_CreatesFailureResult()
        {
            // Arrange & Act
            var result = ProcessingResult<string>.NotFound("Resource not found");

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Value);
            Assert.True(result.IsNotFound);
            Assert.Equal(ErrorCategory.NotFound, result.ErrorType);
            Assert.Single(result.Messages);
            Assert.Equal("Resource not found", result.ErrorMessage);
        }

        [Fact]
        public void Invalid_CreatesFailureResult_WithSingleError()
        {
            // Arrange & Act
            var result = ProcessingResult<int>.Invalid("Validation failed");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
            Assert.Single(result.Messages);
            Assert.Equal("Validation failed", result.ErrorMessage);
        }

        [Fact]
        public void Invalid_CreatesFailureResult_WithMultipleErrors()
        {
            // Arrange & Act
            var result = ProcessingResult<int>.Invalid("Error 1", "Error 2", "Error 3");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
            Assert.Equal(3, result.Messages.Count);
            Assert.All(result.Messages, m => Assert.Equal(MessageType.Error, m.Type));
        }

        [Fact]
        public void Conflict_CreatesFailureResult()
        {
            // Arrange & Act
            var result = ProcessingResult<string>.Conflict("Duplicate key");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.Conflict, result.ErrorType);
            Assert.Equal("Duplicate key", result.ErrorMessage);
        }

        [Fact]
        public void Unauthorized_CreatesFailureResult()
        {
            // Arrange & Act
            var result = ProcessingResult<string>.Unauthorized("Access denied");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.Unauthorized, result.ErrorType);
            Assert.Equal("Access denied", result.ErrorMessage);
        }

        [Fact]
        public void FromException_CreatesFailureResult()
        {
            // Arrange
            var exception = new InvalidOperationException("Test exception");

            // Act
            var result = ProcessingResult<string>.FromException(exception);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ServerError, result.ErrorType);
            Assert.Contains("Test exception", result.ErrorMessage);
        }

        [Fact]
        public void WithWarning_AddsWarningMessage_ReturnsNewResult()
        {
            // Arrange
            var result = ProcessingResult<int>.Ok(42);

            // Act
            var resultWithWarning = result.WithWarning("Warning message");

            // Assert
            Assert.True(resultWithWarning.Success);
            Assert.Equal(42, resultWithWarning.Value);
            Assert.Single(resultWithWarning.Messages);
            Assert.True(resultWithWarning.HasWarning);
            Assert.Equal("Warning message", resultWithWarning.Messages[0].Message);
        }

        [Fact]
        public void WithInfo_AddsInfoMessage_ReturnsNewResult()
        {
            // Arrange
            var result = ProcessingResult<string>.Ok("test");

            // Act
            var resultWithInfo = result.WithInfo("Info message");

            // Assert
            Assert.True(resultWithInfo.Success);
            Assert.Equal("test", resultWithInfo.Value);
            Assert.Single(resultWithInfo.Messages);
            Assert.True(resultWithInfo.HasInfo);
            Assert.Equal("Info message", resultWithInfo.Messages[0].Message);
        }

        [Fact]
        public void WithError_AddsErrorMessage_ReturnsFailedResult()
        {
            // Arrange
            var result = ProcessingResult<int>.Ok(42);

            // Act
            var resultWithError = result.WithError("Error message");

            // Assert
            Assert.False(resultWithError.Success);
            Assert.Single(resultWithError.Messages);
            Assert.True(resultWithError.HasError);
            Assert.Equal("Error message", resultWithError.ErrorMessage);
        }

        [Fact]
        public void Combine_MergesResults_BothSuccessful()
        {
            // Arrange
            var result1 = ProcessingResult<string>.Ok("test").WithInfo("Info 1");
            var result2 = ProcessingResult<string>.Ok("test").WithInfo("Info 2");

            // Act
            var combined = result1.Combine(result2);

            // Assert
            Assert.True(combined.Success);
            Assert.Equal(2, combined.Messages.Count);
        }

        [Fact]
        public void Combine_MergesResults_OneFailure()
        {
            // Arrange
            var result1 = ProcessingResult<string>.Ok("test");
            var result2 = ProcessingResult<string>.Invalid("Validation failed");

            // Act
            var combined = result1.Combine(result2);

            // Assert
            Assert.False(combined.Success);
            Assert.Equal(ErrorCategory.ValidationError, combined.ErrorType);
            Assert.Single(combined.Messages);
        }

        [Fact]
        public void Combine_WithNull_ReturnsOriginal()
        {
            // Arrange
            var result = ProcessingResult<int>.Ok(42);

            // Act
            var combined = result.Combine(null);

            // Assert
            Assert.Equal(result, combined);
        }

        [Fact]
        public void GetWholeMessage_ReturnsEmpty_WhenNoMessages()
        {
            // Arrange
            var result = ProcessingResult<int>.Ok(42);

            // Act
            var message = result.GetWholeMessage();

            // Assert
            Assert.Equal(string.Empty, message);
        }

        [Fact]
        public void GetWholeMessage_ReturnsSingleMessage()
        {
            // Arrange
            var result = ProcessingResult<int>.Invalid("Error message");

            // Act
            var message = result.GetWholeMessage();

            // Assert
            Assert.Equal("Error: Error message", message);
        }

        [Fact]
        public void GetWholeMessage_ReturnsMultipleMessages_Formatted()
        {
            // Arrange
            var result = ProcessingResult<int>.Invalid("Error 1", "Error 2");

            // Act
            var message = result.GetWholeMessage();

            // Assert
            Assert.Contains("Error: Error 1", message);
            Assert.Contains("Error: Error 2", message);
            Assert.Contains("|", message);
        }

        [Fact]
        public void ImplicitBoolConversion_ReturnsSuccess()
        {
            // Arrange
            var successResult = ProcessingResult<int>.Ok(42);
            var failureResult = ProcessingResult<int>.Invalid("Error");

            // Act & Assert
            Assert.True(successResult);
            Assert.False(failureResult);
        }

        [Fact]
        public void IsNotFound_ReturnsTrueForNotFound()
        {
            // Arrange & Act
            var notFoundResult = ProcessingResult<string>.NotFound();
            var emptyOkResult = ProcessingResult<string>.EmptyOk();
            var okResult = ProcessingResult<string>.Ok("test");

            // Assert
            Assert.True(notFoundResult.IsNotFound);
            Assert.True(emptyOkResult.IsNotFound);
            Assert.False(okResult.IsNotFound);
        }

        [Fact]
        public void Deleted_CreatesFailureResult()
        {
            // Arrange & Act
            var result = ProcessingResult<string>.Deleted("Resource deleted");

            // Assert
            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.Deleted, result.ErrorType);
            Assert.Equal("Resource deleted", result.ErrorMessage);
        }

        [Fact]
        public void Messages_AreImmutable()
        {
            // Arrange
            var result = ProcessingResult<int>.Ok(42).WithInfo("Info message");

            // Act & Assert
            Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<ReturnMessage>>(result.Messages);
            Assert.Single(result.Messages);
        }
    }
}