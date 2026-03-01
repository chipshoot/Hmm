using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using Hmm.Utility.Validation;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class GasStationValidatorTests : AutoTestFixtureBase
    {
        private IHmmValidator<GasStation> _validator;
        private int _authorId;

        public GasStationValidatorTests()
        {
            SetupTestEnv();
        }

        #region Valid Entity Tests

        [Fact]
        public async Task ValidStation_CanPassValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                Address = "123 Main St",
                City = "Vancouver",
                Country = "Canada",
                State = "BC",
                ZipCode = "V6B 1A1",
                Description = "Costco gas station",
                IsActive = true
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task ValidStation_WithMinimalFields_CanPassValidation()
        {
            // Arrange - only required fields
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Shell",
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region AuthorId Validation Tests

        [Fact]
        public async Task Station_MustHaveValid_Author()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = 0, // Invalid - zero
                Name = "Costco Gas",
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Station_WithNonExistentAuthor_FailsValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = 99999, // Non-existent author
                Name = "Costco Gas",
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Station_WithNegativeAuthorId_FailsValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = -1, // Negative
                Name = "Costco Gas",
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region Name Validation Tests

        [Fact]
        public async Task Station_MustHaveValid_Name()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "", // Empty - invalid
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Station_WithNullName_FailsValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = null, // Null - invalid
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Station_WithNameExceeding100Chars_FailsValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = new string('A', 101), // 101 chars - exceeds max
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Station_WithNameAt100Chars_PassesValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = new string('A', 100), // Exactly 100 chars
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region Address Validation Tests

        [Fact]
        public async Task Station_WithAddressExceeding200Chars_FailsValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                Address = new string('A', 201), // 201 chars - exceeds max
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Station_WithAddressAt200Chars_PassesValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                Address = new string('A', 200), // Exactly 200 chars
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task Station_WithEmptyAddress_PassesValidation()
        {
            // Arrange - Address is optional
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                Address = "",
                City = "Vancouver",
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region City Validation Tests

        [Fact]
        public async Task Station_WithCityExceeding50Chars_FailsValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                City = new string('A', 51), // 51 chars - exceeds max
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Station_WithCityAt50Chars_PassesValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                City = new string('A', 50), // Exactly 50 chars
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region State Validation Tests

        [Fact]
        public async Task Station_WithStateExceeding50Chars_FailsValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                City = "Vancouver",
                Country = "Canada",
                State = new string('A', 51) // 51 chars - exceeds max
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Station_WithStateAt50Chars_PassesValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                City = "Vancouver",
                Country = "Canada",
                State = new string('A', 50) // Exactly 50 chars
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region ZipCode Validation Tests

        [Fact]
        public async Task Station_WithZipCodeExceeding20Chars_FailsValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                City = "Vancouver",
                Country = "Canada",
                ZipCode = new string('1', 21) // 21 chars - exceeds max
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Station_WithZipCodeAt20Chars_PassesValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                City = "Vancouver",
                Country = "Canada",
                ZipCode = new string('1', 20) // Exactly 20 chars
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region Description Validation Tests

        [Fact]
        public async Task Station_WithDescriptionExceeding500Chars_FailsValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                City = "Vancouver",
                Country = "Canada",
                Description = new string('A', 501) // 501 chars - exceeds max
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task Station_WithDescriptionAt500Chars_PassesValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = _authorId,
                Name = "Costco Gas",
                City = "Vancouver",
                Country = "Canada",
                Description = new string('A', 500) // Exactly 500 chars
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.True(result.Success);
        }

        #endregion

        #region Multiple Validation Errors Tests

        [Fact]
        public async Task Station_WithMultipleErrors_FailsValidation()
        {
            // Arrange
            var station = new GasStation
            {
                AuthorId = 0, // Invalid
                Name = "", // Invalid
                Address = new string('A', 201), // Too long
                City = new string('B', 51), // Too long
                Country = "Canada"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(station);

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new GasStationValidator(LookupRepository);
            _authorId = TestDefaultAuthor.Id;
        }
    }
}
