using AutoMapper;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Hmm.Core.Map;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.TestHelp;
using Hmm.Utility.Validation;
using Moq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hmm.Core.Tests.ValidatorTests
{
    public class NoteCatalogValidatorTests : CoreTestFixtureBase
    {
        private readonly NoteCatalogValidator _validator;

        public NoteCatalogValidatorTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<HmmMappingProfile>();
            }, NullLoggerFactory.Instance);
            var mapper = config.CreateMapper();
            var catalogManager = new NoteCatalogManager(CatalogRepository, UnitOfWork, mapper, LookupRepository, Mock.Of<IHmmValidator<NoteCatalog>>());
            _validator = new NoteCatalogValidator(CatalogRepository);
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

            var result = await _validator.ValidateEntityAsync(catalog);

            // Assert
            Assert.Equal(expected, result.Success);
            if (!expected)
            {
                Assert.NotEmpty(result.Messages[0].Message);
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

            var result = await _validator.ValidateEntityAsync(catalog);

            // Assert
            Assert.Equal(expected, result.Success);
            if (!expected)
            {
                Assert.NotEmpty(result.Messages[0].Message);
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

            var result = await _validator.ValidateEntityAsync(catalog);

            // Assert
            Assert.Equal(expected, result.Success);
            if (!expected)
            {
                Assert.NotEmpty(result.Messages[0].Message);
            }
        }
    }
}