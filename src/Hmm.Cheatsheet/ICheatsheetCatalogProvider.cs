using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;

namespace Hmm.Cheatsheet
{
    /// <summary>
    /// Resolves the single NoteCatalog cheatsheet cards live under. Split out
    /// from the serializer so the serializer stays free of repository concerns,
    /// mirroring Hmm.Automobile.INoteCatalogProvider.
    /// </summary>
    public interface ICheatsheetCatalogProvider
    {
        /// <summary>
        /// Returns the cheatsheet catalog, or null when it does not exist yet.
        /// </summary>
        Task<NoteCatalog> GetCatalogAsync();
    }
}
