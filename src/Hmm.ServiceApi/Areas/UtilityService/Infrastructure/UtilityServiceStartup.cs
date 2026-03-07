using Hmm.Utility.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hmm.ServiceApi.Areas.UtilityService.Infrastructure
{
    public class UtilityServiceStartup
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;

        public UtilityServiceStartup(IServiceCollection services, IConfiguration configuration)
        {
            _services = services;
            _configuration = configuration;
        }

        public void ConfigureServices()
        {
            _services.Configure<GeocodingSettings>(
                _configuration.GetSection(GeocodingSettings.SectionName));

            _services
                .AddHttpClient<IGeocodingService, NominatimGeocodingService>();
        }
    }
}
