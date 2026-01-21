using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Provides access to the current authenticated user's author for automobile operations.
    /// </summary>
    /// <remarks>
    /// <para>Use this interface for:</para>
    /// <list type="bullet">
    /// <item><description>HTTP requests where a user is authenticated</description></item>
    /// <item><description>Operations that should be attributed to the logged-in user</description></item>
    /// <item><description>User-specific data access and creation</description></item>
    /// </list>
    /// <para>For service-level operations without user context, use <see cref="IDefaultAuthorProvider"/> instead.</para>
    /// <para>This provider should be registered with scoped lifetime in DI to ensure
    /// per-request caching of the user's author.</para>
    /// </remarks>
    public interface ICurrentUserAuthorProvider : IAuthorProvider
    {
        /// <summary>
        /// Gets the author for the currently authenticated user.
        /// </summary>
        /// <returns>
        /// A ProcessingResult containing the Author if the user is authenticated and
        /// the author was found/created successfully, or an error if:
        /// <list type="bullet">
        /// <item><description>No user is authenticated</description></item>
        /// <item><description>The user's subject claim is missing</description></item>
        /// <item><description>The author lookup/creation failed</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>If the user's author doesn't exist in the database, it will be
        /// automatically created on first access (if configured to do so).</para>
        /// <para>The author is cached for the duration of the HTTP request.</para>
        /// </remarks>
        Task<ProcessingResult<Author>> GetCurrentUserAuthorAsync();

        /// <summary>
        /// Gets a value indicating whether there is an authenticated user in the current context.
        /// </summary>
        /// <value>
        /// <c>true</c> if a user is authenticated; otherwise, <c>false</c>.
        /// </value>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the subject (unique identifier) of the currently authenticated user.
        /// </summary>
        /// <value>
        /// The user's subject claim value, or null if not authenticated.
        /// </value>
        string UserSubject { get; }
    }
}
