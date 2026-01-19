using Hmm.Automobile;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure
{
    public class AutomobileAppStartupFilter : IStartupFilter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutomobileAppStartupFilter> _logger;

        public AutomobileAppStartupFilter(IServiceProvider serviceProvider, ILogger<AutomobileAppStartupFilter> logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            var app = _serviceProvider.GetService<IApplication>();
            using var scope = _serviceProvider.CreateScope();
            var lookupRepo = scope.ServiceProvider.GetService<IEntityLookup>();

            if (app != null && lookupRepo != null)
            {
                var result = app.RegisterAsync(lookupRepo).GetAwaiter().GetResult();
                if (!result.Success)
                {
                    _logger?.LogWarning("Automobile application registration failed: {Error}", result.ErrorMessage);
                }
            }

            return next;
        }
    }
}