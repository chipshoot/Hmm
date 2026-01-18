using System.Threading.Tasks;
using Hmm.Utility.Misc;

namespace Hmm.Automobile;

/// <summary>
/// Service interface for seeding automobile-related data from external sources.
/// </summary>
public interface ISeedingService
{
    /// <summary>
    /// Seeds automobile and gas discount data from a JSON file.
    /// </summary>
    /// <param name="filePath">Full path to the JSON file containing seeding data.</param>
    /// <returns>
    /// ProcessingResult containing the count of successfully seeded entities.
    /// Warnings contain any individual entity creation failures.
    /// </returns>
    Task<ProcessingResult<int>> SeedDataAsync(string filePath);
}