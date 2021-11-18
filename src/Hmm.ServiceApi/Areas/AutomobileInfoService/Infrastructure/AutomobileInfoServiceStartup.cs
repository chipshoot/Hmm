using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerializer;
using Hmm.Automobile.Validator;
using Hmm.Core;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure
{
    public class AutomobileInfoServiceStartup
    {
        private readonly IServiceCollection _services;

        public AutomobileInfoServiceStartup(IServiceCollection services)
        {
            _services = services;
        }

        public void ConfigureServices()
        {
            try
            {
                _services
                    .AddSingleton<IApplication, ApplicationRegister>()
                    .AddScoped<IHmmValidator<AutomobileInfo>, AutomobileValidator>()
                    .AddScoped<IHmmValidator<GasDiscount>, GasDiscountValidator>()
                    .AddScoped<IHmmValidator<GasLog>, GasLogValidator>()
                    .AddScoped<INoteSerializer<AutomobileInfo>, AutomobileXmlNoteSerializer>()
                    .AddScoped<INoteSerializer<GasDiscount>, GasDiscountXmlNoteSerializer>()
                    .AddScoped<INoteSerializer<GasLog>, GasLogXmlNoteSerializer>()
                    .AddScoped<IAutoEntityManager<AutomobileInfo>, AutomobileManager>()
                    .AddScoped<IAutoEntityManager<GasDiscount>, DiscountManager>()
                    .AddScoped<IAutoEntityManager<GasLog>, GasLogManager>()
                    .AddTransient<IStartupFilter, AutomobileAppStartupFilter>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}