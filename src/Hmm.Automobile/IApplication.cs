using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Defines the contract for automobile module registration.
    /// </summary>
    /// <remarks>
    /// For catalog lookups, use <see cref="INoteCatalogProvider"/> instead.
    /// This separation avoids circular dependencies in the DI container.
    /// </remarks>
    public interface IApplication
    {
        /// <summary>
        /// Registers the automobile module and optionally seeds initial data.
        /// </summary>
        /// <param name="lookupRepo">Repository for entity lookups.</param>
        /// <returns>A ProcessingResult indicating success or failure of the registration.</returns>
        Task<ProcessingResult<bool>> RegisterAsync(IEntityLookup lookupRepo);
    }
}
