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

            // Swappable AI engine: named engines in config, selected per request
            // (default / purpose route / explicit override), each provider a
            // drop-in. Add a new provider by registering its typed HttpClient +
            // surfacing it as IReceiptExtractionProvider, plus an AiEngines entry.
            _services.Configure<AiEngineOptions>(
                _configuration.GetSection(AiEngineOptions.SectionName));

            _services.AddHttpClient<AnthropicReceiptExtractionProvider>();
            _services.AddScoped<IReceiptExtractionProvider>(
                sp => sp.GetRequiredService<AnthropicReceiptExtractionProvider>());

            _services.AddScoped<IReceiptExtractionProviderRegistry, ReceiptExtractionProviderRegistry>();
            _services.AddScoped<IAiEngineSelector, AiEngineSelector>();
            _services.AddScoped<IReceiptExtractionService, ReceiptExtractionService>();
        }
    }
}
