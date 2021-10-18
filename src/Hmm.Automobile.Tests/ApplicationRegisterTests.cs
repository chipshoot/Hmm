using Hmm.Core.DefaultManager;
using Hmm.Core.DefaultManager.Validator;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Xunit;

namespace Hmm.Automobile.Tests
{
    public class ApplicationRegisterTests : AutoTestFixtureBase
    {
        private IApplication _applicationRegister;

        public ApplicationRegisterTests()
        {
            SetupTestEnv();
        }

        [Fact]
        public void Can_Get_Application_Object()
        {
            // Arrange, Act
            var app = _applicationRegister.GetApplication();

            // Assert
            Assert.NotNull(app);
            Assert.NotNull(app.DefaultAuthor);
            Assert.NotNull(app.NoteCatalogs);
            Assert.True(app.NoteCatalogs.Any());
            Assert.All(app.NoteCatalogs, item => Assert.NotNull(item.Render));
            Assert.True(_applicationRegister.ProcessingResult.Success);
        }

        [Fact]
        public void Can_Register_Application()
        {
            // Arrange, Act
            var success = _applicationRegister.Register();

            // Assert
            Assert.True(success);
            Assert.True(_applicationRegister.ProcessingResult.Success);
        }
        private void SetupTestEnv()
        {
            InsertSeedRecords();
            var systemManager = new SubsystemManager(SubsystemRepository, new SubsystemValidator(AuthorRepository));
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            var myConfig = (IConfiguration)config;
            _applicationRegister = new ApplicationRegister(systemManager, LookupRepo, myConfig);
        }
    }
}