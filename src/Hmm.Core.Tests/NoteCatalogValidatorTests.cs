using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Xunit;

namespace Hmm.Core.Tests
{
    public class NoteCatalogValidatorTests : TestFixtureBase
    {
        private NoteCatalogValidator _validator;

        public NoteCatalogValidatorTests()
        {
            SetupTestEnv();
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(15, true)]
        [InlineData(201, false)]
        public void NoteCatalog_Must_Has_Valid_Name_Length(int nameLen, bool expected)
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = GetRandomString(nameLen),
                Render = new NoteRender(),
                Schema = ""
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(catalog, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
            }
        }

        [Fact]
        public void NoteCatalog_Must_Has_Valid_Render_Length()
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test name",
                Render = new NoteRender(),
                Schema = "Test schema"
            };

            // Act
            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(catalog, processResult);

            // Assert
            Assert.True(result);

            // Arrange
            catalog = new NoteCatalog
            {
                Name = "Test name",
                Schema = "Test schema"
            };

            // Act
            processResult = new ProcessingResult();
            result = _validator.IsValidEntity(catalog, processResult);

            // Assert
            Assert.False(result);
            Assert.NotEmpty(processResult.MessageList[0].Message);
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(15, true)]
        public void NoteCatalog_Must_Has_Valid_Schema_Length(int schemaLen, bool expected)
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test name",
                Render = new NoteRender(),
                Schema = GetRandomString(schemaLen)
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(catalog, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
            }
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(1005, false)]
        public void NoteCatalog_Must_Has_Valid_Description_Length(int descLen, bool expected)
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test name",
                Render = new NoteRender(),
                Schema = "Test schema",
                Description = GetRandomString(descLen)
            };

            // Act

            var processResult = new ProcessingResult();
            var result = _validator.IsValidEntity(catalog, processResult);

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
            _validator = new NoteCatalogValidator();
        }
    }
}