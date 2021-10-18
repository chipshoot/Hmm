using Hmm.Automobile;
using Hmm.Automobile.DomainEntity;
using Hmm.Automobile.NoteSerializer;
using Hmm.Contract;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Hmm.Automobile.Validator;
using Hmm.Utility.Validation;

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
            var subsystemMan = _services.BuildServiceProvider().GetService<ISubsystemManager>();
            var lookupRepo = _services.BuildServiceProvider().GetService<IEntityLookup>();
            var config = _services.BuildServiceProvider().GetService<IConfiguration>();
            if (subsystemMan == null || lookupRepo == null || config == null)
            {
                throw new ApplicationException("Cannot get subsystem manager or entity lookup or configuration service");
            }

            try
            {
                // register automobile application to system
                var register = new ApplicationRegister(subsystemMan, lookupRepo, config);
                var success = register.Register();
                if (!success)
                {
                    throw new ApplicationException("Cannot register automobile management application");
                }

                var automobileCatalog = lookupRepo.GetEntities<NoteCatalog>()
                    .FirstOrDefault(c => c.Name == AutomobileConstant.AutoMobileInfoCatalogName);
                var gasDiscountCatalog = lookupRepo.GetEntities<NoteCatalog>()
                    .FirstOrDefault(c => c.Name == AutomobileConstant.GasDiscountCatalogName);
                var gasLogCatalog = lookupRepo.GetEntities<NoteCatalog>()
                    .FirstOrDefault(c => c.Name == AutomobileConstant.GasLogCatalogName);

                //_services.AddScoped<IHmmValidator<AutomobileInfo>, AutomobileValidator>();
                //_services.AddScoped<IHmmValidator<GasDiscount>, GasDiscountValidator>();
                //_services.AddScoped<IHmmValidator<GasLog>, GasLogValidator>();
                //_services.AddScoped<INoteSerializer<AutomobileInfo>>(s =>
                //   new AutomobileXmlNoteSerializer(HmmConstants.DefaultNoteNamespace, automobileCatalog,
                //       s.GetRequiredService<ILogger>()));
                //_services.AddScoped<INoteSerializer<GasDiscount>>(s =>
                //   new GasDiscountXmlNoteSerializer(HmmConstants.DefaultNoteNamespace, gasDiscountCatalog,
                //       s.GetRequiredService<ILogger>()));
                //_services.AddScoped<IAutoEntityManager<AutomobileInfo>, AutomobileManager>();
                //_services.AddScoped<IAutoEntityManager<GasDiscount>, DiscountManager>();
                //_services.AddScoped<INoteSerializer<GasLog>>(s =>
                //    new GasLogXmlNoteSerializer(HmmConstants.DefaultNoteNamespace,
                //        gasLogCatalog,
                //        s.GetRequiredService<ILogger>(),
                //        s.GetRequiredService<IAutoEntityManager<AutomobileInfo>>(),
                //        s.GetRequiredService<IAutoEntityManager<GasDiscount>>()));
                //_services.AddScoped<IAutoEntityManager<GasLog>, GasLogManager>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}