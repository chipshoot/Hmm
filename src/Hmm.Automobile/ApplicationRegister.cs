using Hmm.Core;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Manages the registration, initialization, and configuration of the Automobile application module.
    ///
    /// <para><b>Responsibilities:</b></para>
    /// <list type="bullet">
    /// <item><description>Registers the Automobile module with the Hmm system</description></item>
    /// <item><description>Coordinates data seeding through ISeedingService</description></item>
    /// <item><description>Provides access to the default author via IDefaultAuthorProvider</description></item>
    /// </list>
    ///
    /// <para><b>Design Notes:</b></para>
    /// <list type="bullet">
    /// <item><description>Delegates seeding operations to ISeedingService for better separation of concerns</description></item>
    /// <item><description>Delegates default author management to IDefaultAuthorProvider</description></item>
    /// <item><description>Catalog lookups are handled by INoteCatalogProvider to avoid circular dependencies</description></item>
    /// <item><description>Implements IApplication interface for module registration pattern</description></item>
    /// <item><description>Uses strongly-typed configuration via IOptions pattern</description></item>
    /// </list>
    ///
    /// <para><b>Configuration Requirements:</b></para>
    /// <code>
    /// // In Startup.cs or Program.cs:
    /// services.Configure&lt;AutomobileSeedingOptions&gt;(
    ///     configuration.GetSection("Automobile"));
    ///
    /// // appsettings.json:
    /// {
    ///   "Automobile": {
    ///     "DefaultAuthorAccountName": "automobile-service",
    ///     "CreateDefaultAuthorIfMissing": true,
    ///     "AddSeedingEntity": true,
    ///     "SeedingDataFile": "path/to/seeding-data.json"
    ///   }
    /// }
    /// </code>
    /// </summary>
    public class ApplicationRegister : IApplication
    {
        private readonly AutomobileSeedingOptions _options;
        private readonly ILogger<ApplicationRegister> _logger;
        private readonly ISeedingService _seedingService;
        private readonly IDefaultAuthorProvider _authorProvider;

        /// <summary>
        /// Initializes a new instance of the ApplicationRegister class.
        /// </summary>
        /// <param name="options">Configuration options for automobile seeding.</param>
        /// <param name="seedingService">Service for seeding data from external sources.</param>
        /// <param name="authorProvider">Provider for the default author used by automobile operations.</param>
        /// <param name="logger">Logger instance for diagnostics and error tracking.</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null.</exception>
        public ApplicationRegister(
            IOptions<AutomobileSeedingOptions> options,
            ISeedingService seedingService,
            IDefaultAuthorProvider authorProvider,
            ILogger<ApplicationRegister> logger = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(seedingService);
            ArgumentNullException.ThrowIfNull(authorProvider);

            _options = options.Value ?? new AutomobileSeedingOptions();
            _seedingService = seedingService;
            _authorProvider = authorProvider;
            _logger = logger;
        }

        /// <summary>
        /// Gets the provider for accessing the default author for automobile operations.
        /// </summary>
        /// <remarks>
        /// <para>Use this provider to retrieve the default author from the database.</para>
        /// <para>The author is configured via <see cref="AutomobileSeedingOptions.DefaultAuthorAccountName"/>
        /// and can be auto-created if <see cref="AutomobileSeedingOptions.CreateDefaultAuthorIfMissing"/> is true.</para>
        /// </remarks>
        public IDefaultAuthorProvider AuthorProvider => _authorProvider;

        /// <summary>
        /// Registers the Automobile module with the Hmm system and optionally seeds initial data.
        /// </summary>
        /// <param name="lookupRepo">Repository for entity lookups and queries.</param>
        /// <returns>
        /// A ProcessingResult indicating success or failure of the registration.
        /// Success=true even if seeding is skipped (when not configured).
        /// </returns>
        /// <remarks>
        /// <para><b>Seeding Behavior:</b></para>
        /// <list type="bullet">
        /// <item><description>Checks configuration option AddSeedingEntity</description></item>
        /// <item><description>If true, delegates to ISeedingService to read and seed data</description></item>
        /// <item><description>Seeding service handles all entity creation and error collection</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when lookupRepo is null.</exception>
        public async Task<ProcessingResult<bool>> RegisterAsync(IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);

            try
            {
                // Check if seeding is enabled
                if (!_options.AddSeedingEntity)
                {
                    _logger?.LogInformation("Seeding is disabled for Automobile module");
                    return ProcessingResult<bool>.Ok(true, "Seeding disabled - no data loaded");
                }

                // Get seeding data file path
                var dataFileName = _options.SeedingDataFile;
                if (string.IsNullOrWhiteSpace(dataFileName))
                {
                    _logger?.LogWarning("Seeding is enabled but no data file specified");
                    return ProcessingResult<bool>.Ok(true, "No seeding data file configured");
                }

                // Delegate to seeding service
                _logger?.LogInformation("Starting seeding process from file: {FilePath}", dataFileName);
                var seedResult = await _seedingService.SeedDataAsync(dataFileName);

                if (!seedResult.Success)
                {
                    _logger?.LogError("Seeding failed: {Error}", seedResult.ErrorMessage);
                    return ProcessingResult<bool>.Fail(
                        $"Seeding failed: {seedResult.ErrorMessage}",
                        seedResult.ErrorType);
                }

                // Convert seeding result to registration result
                var registrationResult = ProcessingResult<bool>.Ok(
                    true,
                    $"Registration completed. {seedResult.Value} entities seeded.");

                // Preserve any warnings from seeding
                if (seedResult.HasWarning)
                {
                    foreach (var message in seedResult.Messages.Where(m => m.Type == MessageType.Warning))
                    {
                        registrationResult = registrationResult.WithWarning(message.Message);
                    }
                }

                _logger?.LogInformation("Application registration completed successfully");
                return registrationResult;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during application registration");
                return ProcessingResult<bool>.FromException(ex);
            }
        }
    }
}
