using Hmm.Utility.Dal.Exceptions;
using System;
using Xunit;

namespace Hmm.Utility.Tests
{
    /// <summary>
    /// Tests for UniqueConstraintViolationHelper.
    /// These tests verify the detection of unique constraint violations from various
    /// database exceptions (PostgreSQL, SQL Server) as part of Issue #43 fix.
    /// </summary>
    public class UniqueConstraintViolationHelperTests
    {
        #region IsUniqueConstraintViolation Tests

        [Fact]
        public void IsUniqueConstraintViolation_NullException_ReturnsFalse()
        {
            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsUniqueConstraintViolation_GenericException_ReturnsFalse()
        {
            // Arrange
            var ex = new Exception("Something went wrong");

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(ex);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsUniqueConstraintViolation_MessageContainsUniqueConstraint_ReturnsTrue()
        {
            // Arrange
            var ex = new Exception("duplicate key value violates unique constraint \"uq_authors_accountname\"");

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(ex);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUniqueConstraintViolation_MessageContainsDuplicateKey_ReturnsTrue()
        {
            // Arrange
            var ex = new Exception("Cannot insert duplicate key row in object 'dbo.Authors'");

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(ex);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUniqueConstraintViolation_MessageContainsViolatesUnique_ReturnsTrue()
        {
            // Arrange
            var ex = new Exception("ERROR: violates unique constraint");

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(ex);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUniqueConstraintViolation_NestedExceptionWithUniqueViolation_ReturnsTrue()
        {
            // Arrange
            var innerEx = new Exception("duplicate key value violates unique constraint");
            var outerEx = new Exception("Operation failed", innerEx);

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(outerEx);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUniqueConstraintViolation_DeeplyNestedUniqueViolation_ReturnsTrue()
        {
            // Arrange
            var level3 = new Exception("duplicate key");
            var level2 = new Exception("Inner error", level3);
            var level1 = new Exception("Outer error", level2);

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(level1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUniqueConstraintViolation_CaseInsensitive_ReturnsTrue()
        {
            // Arrange
            var ex = new Exception("DUPLICATE KEY VALUE VIOLATES UNIQUE CONSTRAINT");

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(ex);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("unique constraint violation")]
        [InlineData("UNIQUE CONSTRAINT")]
        [InlineData("Duplicate Key Value")]
        [InlineData("cannot insert duplicate key")]
        [InlineData("Violates Unique")]
        [InlineData("unique index violation")]
        public void IsUniqueConstraintViolation_VariousUniqueMessages_ReturnsTrue(string message)
        {
            // Arrange
            var ex = new Exception(message);

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(ex);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("Foreign key constraint violation")]
        [InlineData("Check constraint failed")]
        [InlineData("Connection timeout")]
        [InlineData("Invalid operation")]
        [InlineData("")]
        public void IsUniqueConstraintViolation_NonUniqueMessages_ReturnsFalse(string message)
        {
            // Arrange
            var ex = new Exception(message);

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(ex);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetConstraintName Tests

        [Fact]
        public void GetConstraintName_NullException_ReturnsNull()
        {
            // Act
            var result = UniqueConstraintViolationHelper.GetConstraintName(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetConstraintName_GenericException_ReturnsNull()
        {
            // Arrange
            var ex = new Exception("Error");

            // Act
            var result = UniqueConstraintViolationHelper.GetConstraintName(ex);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateDuplicateErrorMessage Tests

        [Fact]
        public void CreateDuplicateErrorMessage_Author_ReturnsFormattedMessage()
        {
            // Act
            var result = UniqueConstraintViolationHelper.CreateDuplicateErrorMessage("Author", "AccountName", "john.doe");

            // Assert
            Assert.Equal("A author with AccountName 'john.doe' already exists.", result);
        }

        [Fact]
        public void CreateDuplicateErrorMessage_Tag_ReturnsFormattedMessage()
        {
            // Act
            var result = UniqueConstraintViolationHelper.CreateDuplicateErrorMessage("Tag", "Name", "important");

            // Assert
            Assert.Equal("A tag with Name 'important' already exists.", result);
        }

        [Fact]
        public void CreateDuplicateErrorMessage_MixedCase_NormalizesEntityType()
        {
            // Act
            var result = UniqueConstraintViolationHelper.CreateDuplicateErrorMessage("AUTHOR", "AccountName", "test");

            // Assert
            Assert.Equal("A author with AccountName 'test' already exists.", result);
        }

        [Fact]
        public void CreateDuplicateErrorMessage_EmptyValue_IncludesEmptyQuotes()
        {
            // Act
            var result = UniqueConstraintViolationHelper.CreateDuplicateErrorMessage("Tag", "Name", "");

            // Assert
            Assert.Equal("A tag with Name '' already exists.", result);
        }

        #endregion

        #region Integration-like Tests (Simulated Exceptions)

        /// <summary>
        /// Simulates the exception chain that would occur with a PostgreSQL unique violation:
        /// DataSourceException -> DbUpdateException -> PostgresException
        /// We test with a wrapper that has the right message pattern.
        /// </summary>
        [Fact]
        public void IsUniqueConstraintViolation_SimulatedPostgresExceptionChain_ReturnsTrue()
        {
            // Arrange - simulate the chain: DataSource wrapping inner with postgres-like message
            var postgresLikeEx = new Exception("duplicate key value violates unique constraint \"uq_authors_accountname\"");
            var dbUpdateLikeEx = new Exception("An error occurred while saving the entity changes.", postgresLikeEx);
            var dataSourceEx = new DataSourceExceptionSimulator("Error occurred", dbUpdateLikeEx);

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(dataSourceEx);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUniqueConstraintViolation_SimulatedSqlServerExceptionChain_ReturnsTrue()
        {
            // Arrange - simulate SQL Server unique constraint message
            var sqlLikeEx = new Exception("Cannot insert duplicate key row in object 'dbo.Authors' with unique index 'IX_Authors_AccountName'");
            var dbUpdateLikeEx = new Exception("An error occurred while saving the entity changes.", sqlLikeEx);

            // Act
            var result = UniqueConstraintViolationHelper.IsUniqueConstraintViolation(dbUpdateLikeEx);

            // Assert
            Assert.True(result);
        }

        #endregion

        /// <summary>
        /// Simple exception class that simulates DataSourceException for testing.
        /// </summary>
        private class DataSourceExceptionSimulator : Exception
        {
            public DataSourceExceptionSimulator(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }
    }
}
