using Hmm.Cheatsheet;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Core;
using Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure;
using Hmm.Utility.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Hmm.ServiceApi.Core.Tests
{
    public class CheatsheetServiceStartupTests
    {
        private static ServiceCollection Configured()
        {
            var services = new ServiceCollection();
            new CheatsheetServiceStartup(services).ConfigureServices();
            return services;
        }

        [Theory]
        [InlineData(typeof(ICheatsheetCatalogProvider), typeof(CheatsheetCatalogProvider))]
        [InlineData(typeof(ICheatsheetManager), typeof(CheatsheetManager))]
        public void ConfigureServices_RegistersCheatsheetServices(Type serviceType, Type implementationType)
        {
            var descriptor = Assert.Single(Configured(), d => d.ServiceType == serviceType);

            Assert.Equal(implementationType, descriptor.ImplementationType);
        }

        [Fact]
        public void ConfigureServices_RegistersValidatorAsTransient()
        {
            var descriptor = Assert.Single(
                Configured(),
                d => d.ServiceType == typeof(IHmmValidator<CheatsheetCard>));

            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        [Fact]
        public void ConfigureServices_RegistersNoteSerializer()
        {
            var descriptor = Assert.Single(
                Configured(),
                d => d.ServiceType == typeof(INoteSerializer<CheatsheetCard>));

            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void ConfigureServices_RegistersTheCatalogStartupFilter()
        {
            Assert.Contains(
                Configured(),
                d => d.ImplementationType == typeof(CheatsheetAppStartupFilter));
        }
    }
}
