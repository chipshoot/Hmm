using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Provides access to the default (service-level) author for automobile operations.
    /// </summary>
    /// <remarks>
    /// <para>Use this interface for:</para>
    /// <list type="bullet">
    /// <item><description>Background services and seeding operations</description></item>
    /// <item><description>System-level operations not tied to a specific user</description></item>
    /// <item><description>Fallback when no user context is available</description></item>
    /// </list>
    /// <para>For user-specific operations in HTTP requests, use <see cref="ICurrentUserAuthorProvider"/> instead.</para>
    /// </remarks>
    public interface IDefaultAuthorProvider : IAuthorProvider
    {
        /// <summary>
        /// Gets the configured default author for automobile operations.
        /// </summary>
        /// <returns>
        /// A ProcessingResult containing the Author if found/created successfully,
        /// or an error if the author cannot be resolved.
        /// </returns>
        /// <remarks>
        /// The implementation should cache the author after first retrieval to avoid
        /// repeated database queries.
        /// </remarks>
        Task<ProcessingResult<Author>> GetDefaultAuthorAsync();
    }
}
