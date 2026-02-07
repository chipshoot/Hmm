using Xunit;

namespace Hmm.Automobile.Tests
{
    public class NoteSubjectBuilderTests
    {
        #region BuildGasLogSubject Tests

        [Fact]
        public void BuildGasLogSubject_ReturnsExpectedFormat()
        {
            // Act
            var subject = NoteSubjectBuilder.BuildGasLogSubject(5);

            // Assert
            Assert.Equal("GasLog,AutomobileId:5", subject);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(9999)]
        public void BuildGasLogSubject_EncodesAutomobileId(int automobileId)
        {
            // Act
            var subject = NoteSubjectBuilder.BuildGasLogSubject(automobileId);

            // Assert
            Assert.StartsWith(AutomobileConstant.GasLogRecordSubject, subject);
            Assert.Contains($"{NoteSubjectBuilder.AutomobileIdKey}{NoteSubjectBuilder.KeyValueDelimiter}{automobileId}", subject);
        }

        [Fact]
        public void BuildGasLogSubject_UsesDefinedDelimiters()
        {
            // Act
            var subject = NoteSubjectBuilder.BuildGasLogSubject(10);

            // Assert
            Assert.Contains(NoteSubjectBuilder.ParameterDelimiter, subject);
            Assert.Contains(NoteSubjectBuilder.KeyValueDelimiter, subject);
        }

        #endregion

        #region TryParseAutomobileId Tests

        [Theory]
        [InlineData("GasLog,AutomobileId:5", 5)]
        [InlineData("GasLog,AutomobileId:42", 42)]
        [InlineData("GasLog,AutomobileId:9999", 9999)]
        public void TryParseAutomobileId_ValidSubject_ReturnsTrueAndId(string subject, int expectedId)
        {
            // Act
            var result = NoteSubjectBuilder.TryParseAutomobileId(subject, out var automobileId);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedId, automobileId);
        }

        [Fact]
        public void TryParseAutomobileId_RoundTrips_WithBuildGasLogSubject()
        {
            // Arrange
            const int originalId = 123;
            var subject = NoteSubjectBuilder.BuildGasLogSubject(originalId);

            // Act
            var result = NoteSubjectBuilder.TryParseAutomobileId(subject, out var parsedId);

            // Assert
            Assert.True(result);
            Assert.Equal(originalId, parsedId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TryParseAutomobileId_NullOrEmpty_ReturnsFalse(string subject)
        {
            // Act
            var result = NoteSubjectBuilder.TryParseAutomobileId(subject, out var automobileId);

            // Assert
            Assert.False(result);
            Assert.Equal(0, automobileId);
        }

        [Theory]
        [InlineData("GasLog")]
        [InlineData("Automobile")]
        [InlineData("SomeRandomString")]
        public void TryParseAutomobileId_NoAutomobileIdKey_ReturnsFalse(string subject)
        {
            // Act
            var result = NoteSubjectBuilder.TryParseAutomobileId(subject, out var automobileId);

            // Assert
            Assert.False(result);
            Assert.Equal(0, automobileId);
        }

        [Fact]
        public void TryParseAutomobileId_NonNumericValue_ReturnsFalse()
        {
            // Act
            var result = NoteSubjectBuilder.TryParseAutomobileId("GasLog,AutomobileId:abc", out var automobileId);

            // Assert
            Assert.False(result);
            Assert.Equal(0, automobileId);
        }

        [Fact]
        public void TryParseAutomobileId_MissingValue_ReturnsFalse()
        {
            // Act
            var result = NoteSubjectBuilder.TryParseAutomobileId("GasLog,AutomobileId:", out var automobileId);

            // Assert
            Assert.False(result);
            Assert.Equal(0, automobileId);
        }

        #endregion
    }
}
