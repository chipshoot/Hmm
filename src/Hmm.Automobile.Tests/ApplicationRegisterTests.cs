using System.Collections.Generic;
using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Automobile.Validator;
using Hmm.Core;
using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO;
using System.Linq;
using Hmm.Core.DomainEntity;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class ApplicationRegisterTests : AutoTestFixtureBase
    {
        private IApplication _application;
        private IAutoEntityManager<AutomobileInfo> _automobileManager;
        private IAutoEntityManager<GasDiscount> _discountManager;

        public ApplicationRegisterTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void Can_Get_Application_Object()
        {
            // Arrange, Act
            //var app = _application.GetApplication(LookupRepository);

            // Assert
            //Assert.NotNull(app);
            //Assert.NotNull(app.DefaultAuthor);
            //Assert.NotNull(app.NoteCatalogs);
            //Assert.True(app.NoteCatalogs.Any());
            //Assert.All(app.NoteCatalogs, item => Assert.NotNull(item.Render));
           // Assert.True(_application.ProcessingResult.Success);
        }

        [Fact]
        public void Can_Register_Application()
        {
            // Arrange, Act
            var success = _application.Register(_automobileManager, _discountManager, LookupRepository);

            // Assert
            Assert.True(success);
            Assert.True(_application.ProcessingResult.Success);
        }

        [Theory]
        [InlineData(NoteCatalogType.Automobile, AutomobileConstant.AutoMobileInfoCatalogName)]
        [InlineData(NoteCatalogType.GasDiscount, AutomobileConstant.GasDiscountCatalogName)]
        [InlineData(NoteCatalogType.GasLog, AutomobileConstant.GasLogCatalogName)]
        public void Can_Get_Right_NoteCatalog(NoteCatalogType catalog, string expectedName)
        {
            // Arrange, Act
            var noteCatalog = _application.GetCatalog(catalog, LookupRepository);

            // Assert
            Assert.NotNull(noteCatalog);
            Assert.Equal(expectedName, noteCatalog.Name);
            Assert.True(_application.ProcessingResult.Success);
        }

        [Theory]
        [InlineData("G:\\Projects2\\Hmm\\design\\seedData\\seedingCarDiscount.json")]
        public void Can_Get_Seeding_Automobile_And_Discount(string dataFileName)
        {
            // Arrange
            Assert.True(File.Exists(dataFileName));

            // Act
            var entities = ApplicationRegister.GetSeedingEntities(dataFileName);

            // Assert
            Assert.NotNull(entities);
            Assert.Equal(2, entities.Count());
        }

        private void SetupTestEnv()
        {
            InsertSeedRecords();
            var noteManager = new HmmNoteManager(NoteRepository, new NoteValidator(NoteRepository), DateProvider);

            // automobile manager
            var noteSerializer = new AutomobileXmlNoteSerialize(Application, new NullLogger<AutomobileInfo>(), LookupRepository);
            _automobileManager = new AutomobileManager(noteSerializer, new AutomobileValidator(LookupRepository), noteManager, LookupRepository);

            // gas discount manager
            var discountNoteSerializer = new GasDiscountXmlNoteSerialize(Application, new NullLogger<GasDiscount>(), LookupRepository);
            _discountManager = new DiscountManager(discountNoteSerializer, new GasDiscountValidator(LookupRepository), noteManager, LookupRepository);
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            var myConfig = (IConfiguration)config;
            _application = new ApplicationRegister(myConfig);
        }
    }
}