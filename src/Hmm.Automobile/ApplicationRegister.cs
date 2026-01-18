// Ignore Spelling: Repo

using Hmm.Automobile.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    /// <item><description>Provides access to NoteCatalog instances for different entity types</description></item>
    /// <item><description>Manages the default author for automobile-related notes</description></item>
    /// </list>
    /// 
    /// <para><b>Design Notes:</b></para>
    /// <list type="bullet">
    /// <item><description>Uses caching to minimize database queries for frequently accessed catalogs</description></item>
    /// <item><description>Delegates seeding operations to ISeedingService for better separation of concerns</description></item>
    /// <item><description>Implements IApplication interface for module registration pattern</description></item>
    /// </list>
    /// 
    /// <para><b>Configuration Requirements:</b></para>
    /// <code>
    /// {
    ///   "Automobile": {
    ///     "Seeding": {
    ///       "AddSeedingEntity": "true",
    ///       "SeedingDataFile": "path/to/seeding-data.json"
    ///     }
    ///   }
    /// }
    /// </code>
    /// </summary>
    public class ApplicationRegister : IApplication
    {
        private static Author _appAuthor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApplicationRegister> _logger;
        private readonly ISeedingService _seedingService;
        
        // Catalog cache fields
        private NoteCatalog _automobileCatalog;
        private NoteCatalog _gasDiscountCatalog;
        private NoteCatalog _gasLogCatalog;
        private NoteCatalog _gasStationCatalog;

        /// <summary>
        /// Initializes a new instance of the ApplicationRegister class.
        /// </summary>
        /// <param name="configuration">Application configuration containing seeding and module settings.</param>
        /// <param name="seedingService">Service for seeding data from external sources.</param>
        /// <param name="logger">Logger instance for diagnostics and error tracking.</param>
        /// <exception cref="ArgumentNullException">Thrown when required dependencies are null.</exception>
        public ApplicationRegister(
            IConfiguration configuration,
            ISeedingService seedingService,
            ILogger<ApplicationRegister> logger = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(seedingService);
            
            _configuration = configuration;
            _seedingService = seedingService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the default author for automobile-related notes.
        /// This author is used when no specific author is provided for automobile operations.
        /// </summary>
        /// <remarks>
        /// <para><b>Thread Safety Warning:</b> This property uses lazy initialization with a static field,
        /// which may have race conditions in highly concurrent scenarios. Consider using Lazy&lt;T&gt; 
        /// or dependency injection for the author instead.</para>
        /// 
        /// <para>The default author has a GUID-based account name to ensure uniqueness and is 
        /// automatically activated.</para>
        /// </remarks>
        /// <value>An Author instance configured as the default automobile author.</value>
        public static Author DefaultAuthor
        {
            get
            {
                return _appAuthor ??= new Author
                {
                    AccountName = "03D9D3DE-0C3C-4775-BEC3-6B698B696837",
                    Description = "Automobile default author",
                    Role = AuthorRoleType.Author,
                    IsActivated = true
                };
            }
        }

        /// <summary>
        /// Registers the Automobile module with the Hmm system and optionally seeds initial data.
        /// </summary>
        /// <param name="automobileMan">Manager for AutomobileInfo entities (not used - kept for interface compatibility).</param>
        /// <param name="discountMan">Manager for GasDiscount entities (not used - kept for interface compatibility).</param>
        /// <param name="lookupRepo">Repository for entity lookups and queries.</param>
        /// <returns>
        /// A ProcessingResult indicating success or failure of the registration.
        /// Success=true even if seeding is skipped (when not configured).
        /// </returns>
        /// <remarks>
        /// <para><b>Seeding Behavior:</b></para>
        /// <list type="bullet">
        /// <item><description>Checks configuration key "Automobile:Seeding:AddSeedingEntity"</description></item>
        /// <item><description>If true, delegates to ISeedingService to read and seed data</description></item>
        /// <item><description>Seeding service handles all entity creation and error collection</description></item>
        /// </list>
        /// 
        /// <para><b>Note:</b> automobileMan and discountMan parameters are kept for backward compatibility
        /// but are no longer used. The ISeedingService injected in the constructor handles entity creation.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when lookupRepo is null.</exception>
        public async Task<ProcessingResult<bool>> RegisterAsync(
            IAutoEntityManager<AutomobileInfo> automobileMan,
            IAutoEntityManager<GasDiscount> discountMan,
            IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);

            try
            {
                // Check if seeding is enabled
                var addSeedRecords = bool.Parse(_configuration["Automobile:Seeding:AddSeedingEntity"] ?? "false");
                if (!addSeedRecords)
                {
                    _logger?.LogInformation("Seeding is disabled for Automobile module");
                    return ProcessingResult<bool>.Ok(true, "Seeding disabled - no data loaded");
                }

                // Get seeding data file path
                var dataFileName = _configuration["Automobile:Seeding:SeedingDataFile"];
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

        /// <summary>
        /// Asynchronously retrieves a NoteCatalog for the specified entity type.
        /// Catalogs are cached after first retrieval to minimize database queries.
        /// </summary>
        /// <param name="entityType">The type of entity for which to retrieve the catalog.</param>
        /// <param name="lookupRepo">Repository for entity lookups.</param>
        /// <returns>
        /// The NoteCatalog for the specified entity type, or null if not found.
        /// </returns>
        /// <remarks>
        /// <para><b>Caching Strategy:</b></para>
        /// <list type="bullet">
        /// <item><description>First call queries the database and caches the result</description></item>
        /// <item><description>Subsequent calls return the cached instance</description></item>
        /// <item><description>Cache is per-instance (not static) and never expires</description></item>
        /// <item><description>To refresh, create a new ApplicationRegister instance</description></item>
        /// </list>
        /// </remarks>
        public async Task<NoteCatalog> GetCatalogAsync(NoteCatalogType entityType, IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);

            var catalogName = entityType switch
            {
                NoteCatalogType.Automobile => AutomobileConstant.AutoMobileInfoCatalogName,
                NoteCatalogType.GasDiscount => AutomobileConstant.GasDiscountCatalogName,
                NoteCatalogType.GasLog => AutomobileConstant.GasLogCatalogName,
                NoteCatalogType.GasStation => AutomobileConstant.GasStationCatalogName,
                _ => null
            };

            if (string.IsNullOrEmpty(catalogName))
            {
                _logger?.LogWarning("Unknown catalog type requested: {EntityType}", entityType);
                return null;
            }

            // Check cached catalogs first
            var cachedCatalog = entityType switch
            {
                NoteCatalogType.Automobile => _automobileCatalog,
                NoteCatalogType.GasDiscount => _gasDiscountCatalog,
                NoteCatalogType.GasLog => _gasLogCatalog,
                NoteCatalogType.GasStation => _gasStationCatalog,
                _ => null
            };

            if (cachedCatalog != null)
            {
                return cachedCatalog;
            }

            // Fetch from repository
            _logger?.LogDebug("Fetching catalog from database: {CatalogName}", catalogName);
            var catalogsResult = await lookupRepo.GetEntitiesAsync<NoteCatalog>(c => c.Name == catalogName);
            if (!catalogsResult.Success || catalogsResult.Value == null)
            {
                _logger?.LogWarning("Failed to retrieve catalog: {CatalogName}", catalogName);
                return null;
            }

            var catalog = catalogsResult.Value.FirstOrDefault();
            if (catalog != null)
            {
                // Cache the result
                switch (entityType)
                {
                    case NoteCatalogType.Automobile:
                        _automobileCatalog = catalog;
                        break;
                    case NoteCatalogType.GasDiscount:
                        _gasDiscountCatalog = catalog;
                        break;
                    case NoteCatalogType.GasLog:
                        _gasLogCatalog = catalog;
                        break;
                    case NoteCatalogType.GasStation:
                        _gasStationCatalog = catalog;
                        break;
                }
                
                _logger?.LogDebug("Catalog cached: {CatalogName}", catalogName);
            }

            return catalog;
        }

        /// <summary>
        /// Synchronously retrieves a NoteCatalog for the specified entity type.
        /// </summary>
        /// <param name="entityType">The type of entity for which to retrieve the catalog.</param>
        /// <param name="lookupRepo">Repository for entity lookups.</param>
        /// <returns>The NoteCatalog for the specified entity type, or null if not found.</returns>
        /// <remarks>
        /// <para><b>Warning:</b> This method uses GetAwaiter().GetResult() which can cause deadlocks
        /// in certain synchronization contexts (e.g., ASP.NET, WPF, WinForms).</para>
        /// </remarks>
        [Obsolete("Use GetCatalogAsync instead to avoid potential deadlocks")]
        public NoteCatalog GetCatalog(NoteCatalogType entityType, IEntityLookup lookupRepo)
        {
            return GetCatalogAsync(entityType, lookupRepo).GetAwaiter().GetResult();
        }
    }
}
