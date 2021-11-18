using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.Validator;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class AutomobileValidatorTests : AutoTestFixtureBase
    {
        private IHmmValidator<AutomobileInfo> _validator;
        private Guid _authorId;

        public AutomobileValidatorTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void ValidAutomobileInfo_CanPassValidation()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "1234",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(auto, processResult);

            // Assert
            Assert.True(result);
            Assert.Empty(processResult.MessageList);
        }

        [Fact]
        public void AutoMustHaveValid_Author()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "1234",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(auto, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Fact]
        public void AutoMustHaveValid_Maker()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "1234",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(auto, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Fact]
        public void AutoMustHaveValid_Year()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "",
                Pin = "1234",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(auto, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Fact]
        public void AutoMustHaveValid_MeterReading()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = -100,
                Year = "2018",
                Pin = "1234",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(auto, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Fact]
        public void AutoMustHaveValid_Pin()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "",
                Plate = "BCTT208",
                Color = "Blue"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(auto, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Fact]
        public void AutoMustHaveValid_Plate()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "1234",
                Plate = "",
                Color = "Blue"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(auto, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        [Fact]
        public void AutoMustHaveValid_Color()
        {
            // Arrange
            var auto = new AutomobileInfo
            {
                AuthorId = _authorId,
                Brand = "Outback",
                Maker = "Subaru",
                MeterReading = 100,
                Year = "2018",
                Pin = "1234",
                Plate = "BCTT208",
                Color = ""
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(auto, processResult);

            // Assert
            Assert.False(result);
            Assert.Single(processResult.MessageList);
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new AutomobileValidator(LookupRepo);
            _authorId = ApplicationRegister.DefaultAuthor.Id;
        }
    }
}