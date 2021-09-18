using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Xunit;

namespace Hmm.Core.Tests
{
    public class SubsystemValidatorTests : TestFixtureBase
    {
        private SubsystemValidator _validator;

        public SubsystemValidatorTests()
        {
            SetupTestEnv();
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(15, true)]
        [InlineData(201, false)]
        public void Subsystem_Must_Has_Valid_Name_Length(int nameLen, bool expected)
        {
            // Arrange
            var sys = new Subsystem
            {
                Name = GetRandomString(nameLen),
                Description = "Default Subsystem"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(sys, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
            }
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(1005, false)]
        public void NoteRender_Must_Has_Valid_Description_Length(int descLen, bool expected)
        {
            // Arrange
            var sys = new Subsystem
            {
                Name = "Test name",
                Description = GetRandomString(descLen)
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(sys, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
            }
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            _validator = new SubsystemValidator();
        }
    }
}