using Hmm.Cheatsheet;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.NoteSerialize;
using Hmm.Cheatsheet.Validator;
using Hmm.Core;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure
{
    /// <summary>
    /// Registers the cheatsheet module. Author resolution, IHmmNoteManager and
    /// IEntityLookup are already registered by the core Startup and the
    /// automobile module, so nothing here re-registers them.
    /// </summary>
    public class CheatsheetServiceStartup
    {
        private readonly IServiceCollection _services;

        public CheatsheetServiceStartup(IServiceCollection services)
        {
            _services = services;
        }

        public void ConfigureServices()
        {
            _services
                // Catalog lookup (Scoped - depends on IEntityLookup which is Scoped)
                .AddScoped<ICheatsheetCatalogProvider, CheatsheetCatalogProvider>()

                // Validator registered Transient for thread-safety, matching the
                // convention used for every other IHmmValidator in Startup.cs.
                .AddTransient<IHmmValidator<CheatsheetCard>, CheatsheetValidator>()

                // Note serializer
                .AddScoped<INoteSerializer<CheatsheetCard>, CheatsheetJsonNoteSerialize>()

                // Manager
                .AddScoped<ICheatsheetManager, CheatsheetManager>()

                // Ensures the cheatsheet NoteCatalog row exists before first use
                .AddTransient<IStartupFilter, CheatsheetAppStartupFilter>();
        }
    }
}
