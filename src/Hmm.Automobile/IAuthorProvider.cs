using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Base interface for providing author information for automobile operations.
    /// </summary>
    /// <remarks>
    /// <para>This interface defines the common contract for author resolution.</para>
    /// <para>Implementations include:</para>
    /// <list type="bullet">
    /// <item><description><see cref="IDefaultAuthorProvider"/> - For service/background operations using configured default author</description></item>
    /// <item><description><see cref="ICurrentUserAuthorProvider"/> - For HTTP requests using the authenticated user</description></item>
    /// </list>
    /// </remarks>
    public interface IAuthorProvider
    {
        /// <summary>
        /// Gets the author for automobile operations.
        /// </summary>
        /// <returns>
        /// A ProcessingResult containing the Author if resolved successfully,
        /// or an error if the author cannot be determined.
        /// </returns>
        Task<ProcessingResult<Author>> GetAuthorAsync();

        /// <summary>
        /// Gets the cached author without making a database call.
        /// Returns null if the author has not been loaded yet.
        /// </summary>
        /// <value>The cached Author instance, or null if not yet loaded.</value>
        Author CachedAuthor { get; }
    }
}
