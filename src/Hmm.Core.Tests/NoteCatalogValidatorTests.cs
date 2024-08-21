using AutoMapper;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.Core.Tests
{
    public class NoteCatalogValidatorTests : CoreTestFixtureBase
    {
        private readonly NoteCatalogValidator _validator;

        public NoteCatalogValidatorTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<HmmMappingProfile>();
            });
            var mapper = config.CreateMapper();
            var catalogManager = new NoteCatalogManager(CatalogRepository, mapper);
            _validator = new NoteCatalogValidator(catalogManager);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(15, true)]
        [InlineData(201, false)]
        public async Task NoteCatalog_Must_Has_Valid_Name_Length(int nameLen, bool expected)
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = GetRandomString(nameLen),
                Schema = ""
            };

            // Act

            var processResult = new ProcessingResult();
            var result = await _validator.IsValidEntityAsync(catalog, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
            }
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(15, true)]
        public async Task NoteCatalog_Must_Has_Valid_Schema_Length(int schemaLen, bool expected)
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test name",
                Schema = GetRandomString(schemaLen)
            };

            // Act

            var processResult = new ProcessingResult();
            var result = await _validator.IsValidEntityAsync(catalog, processResult);

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
        public async Task NoteCatalog_Must_Has_Valid_Description_Length(int descLen, bool expected)
        {
            // Arrange
            var catalog = new NoteCatalog
            {
                Name = "Test name",
                Schema = "Test schema",
                Description = GetRandomString(descLen)
            };

            // Act

            var processResult = new ProcessingResult();
            var result = await _validator.IsValidEntityAsync(catalog, processResult);

            // Assert
            Assert.Equal(expected, result);
            if (!expected)
            {
                Assert.NotEmpty(processResult.MessageList[0].Message);
            }
        }
    }
}