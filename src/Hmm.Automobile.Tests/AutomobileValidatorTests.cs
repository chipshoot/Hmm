using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using Hmm.Utility.Validation;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutomobileValidatorTests : AutoTestFixtureBase
    {
        private IHmmValidator<AutomobileInfo> _validator;
        private int _authorId;

        public AutomobileValidatorTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public async Task ValidAutomobileInfo_CanPassValidation()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = 2018,
                VIN = "1HGBH41JXMN109186",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(auto);

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AutoMustHaveValid_Author()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = 0, // Invalid author
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = 2018,
                VIN = "1HGBH41JXMN109186",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(auto);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task AutoMustHaveValid_Brand()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "", // Invalid - empty
                Maker = "Subaru",
                MeterReading = 100,
                Year = 2018,
                VIN = "1HGBH41JXMN109186",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(auto);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task AutoMustHaveValid_Maker()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "", // Invalid - empty
                MeterReading = 100,
                Year = 2018,
                VIN = "1HGBH41JXMN109186",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(auto);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task AutoMustHaveValid_Year()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = 0, // Invalid - zero year
                VIN = "1HGBH41JXMN109186",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(auto);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task AutoMustHaveValid_MeterReading()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = -100, // Invalid - negative
                Year = 2018,
                VIN = "1HGBH41JXMN109186",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(auto);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task AutoMustHaveValid_VIN()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = 2018,
                VIN = "", // Invalid - empty
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(auto);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task AutoMustHaveValid_Plate()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = 2018,
                VIN = "1HGBH41JXMN109186",
                Plate = "", // Invalid - empty
                Color = "Blue"
            };

            // Act
            var result = await _validator.ValidateEntityAsync(auto);

            // Assert
            Assert.False(result.Success);
        }

        [Fact]
        public async Task AutoMustHaveValid_Color()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = 2018,
                VIN = "1HGBH41JXMN109186",
                Plate = "BCTT208",
                Color = "" // Invalid - empty
            };

            // Act
            var result = await _validator.ValidateEntityAsync(auto);

            // Assert
            Assert.False(result.Success);
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new AutomobileValidator(LookupRepository);
            _authorId = TestDefaultAuthor.Id;
        }
    }
}
