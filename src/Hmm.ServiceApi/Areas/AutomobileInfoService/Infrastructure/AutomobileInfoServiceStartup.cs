using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerialize;
using Hmm.Automobile.Validator;
using Hmm.Core;
using Hmm.ServiceApi.Services;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure
{
    public class AutomobileInfoServiceStartup
    {
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;

        public AutomobileInfoServiceStartup(IServiceCollection services, IConfiguration configuration)
        {
            _services = services;
            _configuration = configuration;
        }

        public void ConfigureServices()
        {
            try
            {
                _services
                    // Infrastructure services
                    .TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                _services
                    // Author providers:
                    // - DefaultAuthorProvider: Scoped (depends on IAuthorManager which is Scoped)
                    // - CurrentUserAuthorProvider: Scoped for HTTP requests (uses authenticated user)
                    // - IAuthorProvider: Resolves to CurrentUserAuthorProvider for managers (uses current user)
                    .AddScoped<IDefaultAuthorProvider, DefaultAuthorProvider>()
                    .AddScoped<ICurrentUserAuthorProvider, CurrentUserAuthorProvider>()
                    .AddScoped<IAuthorProvider>(sp => sp.GetRequiredService<ICurrentUserAuthorProvider>())

                    // Note catalog provider (Scoped - depends on IEntityLookup which is Scoped)
                    .AddScoped<INoteCatalogProvider, NoteCatalogProvider>()

                    // Seeding service (Scoped - depends on IAutoEntityManager which is Scoped)
                    .AddScoped<ISeedingService, AutomobileSeedingService>()

                    // Application registration (Scoped - depends on ISeedingService and IDefaultAuthorProvider which are Scoped)
                    .AddScoped<IApplication, ApplicationRegister>()

                    // Validators
                    .AddScoped<IHmmValidator<AutomobileInfo>, AutomobileValidator>()
                    .AddScoped<IHmmValidator<GasDiscount>, GasDiscountValidator>()
                    .AddScoped<IHmmValidator<GasLog>, GasLogValidator>()
                    .AddScoped<IHmmValidator<GasStation>, GasStationValidator>()

                    // Note serializers
                    .AddScoped<INoteSerializer<AutomobileInfo>, AutomobileJsonNoteSerialize>()
                    .AddScoped<INoteSerializer<GasDiscount>, GasDiscountJsonNoteSerialize>()
                    .AddScoped<INoteSerializer<GasLog>, GasLogJsonNoteSerialize>()
                    .AddScoped<INoteSerializer<GasStation>, GasStationJsonNoteSerialize>()

                    // Entity managers (use IAuthorProvider which resolves to CurrentUserAuthorProvider)
                    .AddScoped<IAutoEntityManager<AutomobileInfo>, AutomobileManager>()
                    .AddScoped<IAutoEntityManager<GasDiscount>, DiscountManager>()
                    .AddScoped<IGasLogManager, GasLogManager>()
                    .AddScoped<GasStationManager>()
                    .AddScoped<IAutoEntityManager<GasStation>>(sp => sp.GetRequiredService<GasStationManager>())

                    // Geocoding service
                    ;

                _services.Configure<GeocodingSettings>(
                    _configuration.GetSection(GeocodingSettings.SectionName));

                _services
                    .AddHttpClient<IGeocodingService, NominatimGeocodingService>()
                    .Services

                    // Startup filter
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