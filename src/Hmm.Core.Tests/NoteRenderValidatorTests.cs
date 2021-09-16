using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Xunit;

namespace Hmm.Core.Tests
{
    public class NoteRenderValidatorTests : TestFixtureBase
    {
        private readonly NoteRenderValidator _validator;

        public NoteRenderValidatorTests()
        {
            InsertSeedRecords();
            _validator = new NoteRenderValidator();
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(15, true)]
        [InlineData(401, false)]
        public void NoteRender_Must_Has_Valid_Name_Length(int nameLen, bool expected)
        {
            // Arrange
            var render = new NoteRender
            {
                Name = GetRandomString(nameLen),
                Namespace = "Default NameSpace"
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(render, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
            }
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, false)]
        [InlineData(15, true)]
        [InlineData(1001, false)]
        public void NoteRender_Must_Has_Valid_NameSpace_Length(int namespaceLen, bool expected)
        {
            // Arrange
            var render = new NoteRender
            {
                Name = "Test name",
                Namespace = GetRandomString(namespaceLen)
            };

            // Act
            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(render, processResult);

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
            var render = new NoteRender
            {
                Name = "Test name",
                Namespace = "Test NameSpace",
                Description = GetRandomString(descLen)
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(render, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
            }
        }
    }
}