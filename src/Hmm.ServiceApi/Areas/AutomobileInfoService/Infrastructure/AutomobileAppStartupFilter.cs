using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure
{
    public class AutomobileAppStartupFilter : IStartupFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public AutomobileAppStartupFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            var app = _serviceProvider.GetService<IApplication>();
            using var scope = _serviceProvider.CreateScope();
            var systemMan = scope.ServiceProvider.GetService<ISubsystemManager>();
            var automobileMan = scope.ServiceProvider.GetService<IAutoEntityManager<AutomobileInfo>>();
            var discountMan = scope.ServiceProvider.GetService<IAutoEntityManager<GasDiscount>>();
            var lookupRepo = scope.ServiceProvider.GetService<IEntityLookup>();
            if (app != null)
            {
                var success = app.Register(systemMan, automobileMan, discountMan, lookupRepo);
                if (!success)
                {
                }
            }

            return next;
        }
    }
}