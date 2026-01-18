using Hmm.Automobile.DomainEntity;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Automobile;

/// <summary>
/// Service for seeding automobile-related data from JSON files.
/// Handles deserialization and creation of AutomobileInfo and GasDiscount entities.
/// </summary>
public class AutomobileSeedingService : ISeedingService
{
    private readonly IAutoEntityManager<AutomobileInfo> _automobileManager;
    private readonly IAutoEntityManager<GasDiscount> _discountManager;
    private readonly ILogger<AutomobileSeedingService> _logger;

    /// <summary>
    /// Initializes a new instance of the AutomobileSeedingService class.
    /// </summary>
    /// <param name="automobileManager">Manager for AutomobileInfo entities.</param>
    /// <param name="discountManager">Manager for GasDiscount entities.</param>
    /// <param name="logger">Logger for diagnostics and error tracking.</param>
    public AutomobileSeedingService(
        IAutoEntityManager<AutomobileInfo> automobileManager,
        IAutoEntityManager<GasDiscount> discountManager,
        ILogger<AutomobileSeedingService> logger)
    {
        ArgumentNullException.ThrowIfNull(automobileManager);
        ArgumentNullException.ThrowIfNull(discountManager);
        ArgumentNullException.ThrowIfNull(logger);

        _automobileManager = automobileManager;
        _discountManager = discountManager;
        _logger = logger;
    }

    /// <summary>
    /// Seeds automobile and gas discount data from a JSON file.
    /// </summary>
    /// <param name="filePath">Full path to the JSON file containing seeding data.</param>
    /// <returns>
    /// ProcessingResult containing the count of successfully seeded entities.
    /// Returns failure if file doesn't exist or JSON is invalid.
    /// Individual entity creation failures are returned as warnings.
    /// </returns>
    /// <remarks>
    /// Expected JSON format:
    /// <code>
    /// {
    ///   "AutomobileInfos": [ { ... }, { ... } ],
    ///   "GasDiscounts": [ { ... }, { ... } ]
    /// }
    /// </code>
    /// </remarks>
    public async Task<ProcessingResult<int>> SeedDataAsync(string filePath)
    {
        try
        {
            // Validate file path
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return ProcessingResult<int>.Invalid("File path cannot be empty");
            }

            if (!File.Exists(filePath))
            {
                _logger.LogError("Seeding data file not found: {FilePath}", filePath);
                return ProcessingResult<int>.NotFound($"Seeding data file not found: {filePath}");
            }

            // Read and deserialize entities
            _logger.LogInformation("Reading seeding data from: {FilePath}", filePath);
            var entitiesResult = await ReadSeedingEntitiesAsync(filePath);
            
            if (!entitiesResult.Success)
            {
                return ProcessingResult<int>.Fail(
                    entitiesResult.ErrorMessage,
                    entitiesResult.ErrorType);
            }

            var entities = entitiesResult.Value;
            if (entities == null || !entities.Any())
            {
                _logger.LogWarning("No entities found in seeding data file");
                return ProcessingResult<int>.Ok(0, "No entities to seed");
            }

            // Seed the entities
            var seedResult = await SeedEntitiesAsync(entities);
            return seedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during seeding operation");
            return ProcessingResult<int>.FromException(ex);
        }
    }

    /// <summary>
    /// Reads and deserializes seeding entities from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <returns>ProcessingResult containing list of AutomobileBase entities.</returns>
    private async Task<ProcessingResult<List<AutomobileBase>>> ReadSeedingEntitiesAsync(string filePath)
    {
        try
        {
            // Read file asynchronously
            var jsonText = await File.ReadAllTextAsync(filePath);

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                return ProcessingResult<List<AutomobileBase>>.Ok(
                    new List<AutomobileBase>(),
                    "Empty seeding file");
            }

            // Deserialize with proper options
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            options.Converters.Add(new Utility.Json.MoneyJsonConverter());

            var root = JsonSerializer.Deserialize<SeedingEntityRoot>(jsonText, options);
            if (root == null)
            {
                return ProcessingResult<List<AutomobileBase>>.Fail(
                    "Failed to deserialize seeding data",
                    ErrorCategory.MappingError);
            }

            // Combine all entities
            var entities = new List<AutomobileBase>();
            
            if (root.AutomobileInfos != null)
            {
                entities.AddRange(root.AutomobileInfos);
            }

            if (root.GasDiscounts != null)
            {
                entities.AddRange(root.GasDiscounts);
            }

            _logger.LogInformation(
                "Loaded {TotalCount} entities from file ({AutoCount} automobiles, {DiscountCount} discounts)",
                entities.Count,
                root.AutomobileInfos?.Count() ?? 0,
                root.GasDiscounts?.Count() ?? 0);

            return ProcessingResult<List<AutomobileBase>>.Ok(entities);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while reading seeding data");
            return ProcessingResult<List<AutomobileBase>>.Fail(
                $"Invalid JSON format: {ex.Message}",
                ErrorCategory.MappingError);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O error while reading seeding file");
            return ProcessingResult<List<AutomobileBase>>.Fail(
                $"File read error: {ex.Message}",
                ErrorCategory.ServerError);
        }
    }

    /// <summary>
    /// Seeds the provided entities into the database.
    /// </summary>
    /// <param name="entities">List of entities to seed.</param>
    /// <returns>ProcessingResult containing count of successfully seeded entities.</returns>
    private async Task<ProcessingResult<int>> SeedEntitiesAsync(List<AutomobileBase> entities)
    {
        var errors = new List<string>();
        var successCount = 0;

        // Seed automobiles
        var automobiles = entities.OfType<AutomobileInfo>().ToList();
        _logger.LogInformation("Seeding {Count} automobiles", automobiles.Count);

        foreach (var automobile in automobiles)
        {
            try
            {
                var result = await _automobileManager.CreateAsync(automobile);
                if (!result.Success)
                {
                    var errorMsg = $"Failed to create automobile '{automobile.Brand} {automobile.Maker}': {result.ErrorMessage}";
                    errors.Add(errorMsg);
                    _logger.LogWarning("Failed to seed automobile: {Error}", result.ErrorMessage);
                }
                else
                {
                    successCount++;
                    _logger.LogDebug("Successfully seeded automobile: {Brand} {Maker}", 
                        automobile.Brand, automobile.Maker);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Exception while creating automobile '{automobile.Brand} {automobile.Maker}': {ex.Message}";
                errors.Add(errorMsg);
                _logger.LogError(ex, "Exception during automobile seeding");
            }
        }

        // Seed discounts
        var discounts = entities.OfType<GasDiscount>().ToList();
        _logger.LogInformation("Seeding {Count} gas discounts", discounts.Count);

        foreach (var discount in discounts)
        {
            try
            {
                var result = await _discountManager.CreateAsync(discount);
                if (!result.Success)
                {
                    var errorMsg = $"Failed to create discount '{discount.Program}': {result.ErrorMessage}";
                    errors.Add(errorMsg);
                    _logger.LogWarning("Failed to seed discount: {Error}", result.ErrorMessage);
                }
                else
                {
                    successCount++;
                    _logger.LogDebug("Successfully seeded discount: {Program}", discount.Program);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Exception while creating discount '{discount.Program}': {ex.Message}";
                errors.Add(errorMsg);
                _logger.LogError(ex, "Exception during discount seeding");
            }
        }

        // Build result
        _logger.LogInformation(
            "Seeding completed: {SuccessCount} entities created, {ErrorCount} errors",
            successCount, errors.Count);

        if (errors.Count > 0)
        {
            var result = ProcessingResult<int>.Ok(successCount, $"Seeded {successCount} entities with {errors.Count} errors");
            foreach (var error in errors)
            {
                result = result.WithWarning(error);
            }
            return result;
        }

        return ProcessingResult<int>.Ok(successCount, $"Successfully seeded {successCount} entities");
    }

    /// <summary>
    /// Internal DTO for deserializing seeding data from JSON.
    /// </summary>
    private class SeedingEntityRoot
    {
        /// <summary>
        /// Collection of AutomobileInfo entities to seed.
        /// </summary>
        public IEnumerable<AutomobileInfo> AutomobileInfos { get; set; }

        /// <summary>
        /// Collection of GasDiscount entities to seed.
        /// </summary>
        public IEnumerable<GasDiscount> GasDiscounts { get; set; }
    }
}