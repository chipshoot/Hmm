using Hmm.Automobile;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Specifications;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Services
{
    /// <summary>
    /// Provides the current authenticated user's author for automobile operations.
    /// </summary>
    /// <remarks>
    /// <para>This provider:</para>
    /// <list type="bullet">
    /// <item><description>Extracts user identity from JWT claims via IHttpContextAccessor</description></item>
    /// <item><description>Looks up or creates the Author based on the user's subject claim</description></item>
    /// <item><description>Caches the author for the duration of the HTTP request (scoped)</description></item>
    /// <item><description>Falls back to default author provider when no user is authenticated</description></item>
    /// </list>
    /// <para>Register this provider with scoped lifetime in DI.</para>
    /// </remarks>
    public class CurrentUserAuthorProvider : ICurrentUserAuthorProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorManager _authorManager;
        private readonly IDefaultAuthorProvider _defaultAuthorProvider;
        private readonly ILogger<CurrentUserAuthorProvider> _logger;

        private Author _cachedAuthor;
        private bool _initialized;

        public CurrentUserAuthorProvider(
            IHttpContextAccessor httpContextAccessor,
            IAuthorManager authorManager,
            IDefaultAuthorProvider defaultAuthorProvider,
            ILogger<CurrentUserAuthorProvider> logger = null)
        {
            ArgumentNullException.ThrowIfNull(httpContextAccessor);
            ArgumentNullException.ThrowIfNull(authorManager);
            ArgumentNullException.ThrowIfNull(defaultAuthorProvider);

            _httpContextAccessor = httpContextAccessor;
            _authorManager = authorManager;
            _defaultAuthorProvider = defaultAuthorProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public Author CachedAuthor => _cachedAuthor;

        /// <inheritdoc />
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        /// <inheritdoc />
        public string UserSubject => GetUserSubject();

        /// <inheritdoc />
        public Task<ProcessingResult<Author>> GetAuthorAsync() => GetCurrentUserAuthorAsync();

        /// <inheritdoc />
        public async Task<ProcessingResult<Author>> GetCurrentUserAuthorAsync()
        {
            // Return cached author if already resolved for this request
            if (_initialized && _cachedAuthor != null)
            {
                return ProcessingResult<Author>.Ok(_cachedAuthor);
            }

            // Check if user is authenticated
            if (!IsAuthenticated)
            {
                _logger?.LogDebug("No authenticated user, falling back to default author provider");
                var defaultResult = await _defaultAuthorProvider.GetAuthorAsync();
                if (defaultResult.Success)
                {
                    _cachedAuthor = defaultResult.Value;
                    _initialized = true;
                }
                return defaultResult;
            }

            var subject = UserSubject;
            if (string.IsNullOrWhiteSpace(subject))
            {
                _logger?.LogWarning("Authenticated user has no subject claim, falling back to default author");
                return await _defaultAuthorProvider.GetAuthorAsync();
            }

            return await ResolveUserAuthorAsync(subject);
        }

        private async Task<ProcessingResult<Author>> ResolveUserAuthorAsync(string subject)
        {
            _logger?.LogDebug("Resolving author for user subject: {Subject}", subject);

            // Try to find existing author by account name (which stores the subject)
            var spec = new AuthorByAccountNameSpecification(subject);
            var authorsResult = await _authorManager.GetEntitiesAsync(spec.ToExpression());
            if (!authorsResult.Success)
            {
                _logger?.LogError("Failed to query for user author: {Error}", authorsResult.ErrorMessage);
                return ProcessingResult<Author>.Fail(authorsResult.ErrorMessage, authorsResult.ErrorType);
            }

            var author = authorsResult.Value?.FirstOrDefault();
            if (author != null)
            {
                _logger?.LogDebug("Found existing author for user: {Subject} (Id: {Id})", subject, author.Id);
                _cachedAuthor = author;
                _initialized = true;
                return ProcessingResult<Author>.Ok(author);
            }

            // Author not found - create new author for the user
            return await CreateUserAuthorAsync(subject);
        }

        private async Task<ProcessingResult<Author>> CreateUserAuthorAsync(string subject)
        {
            _logger?.LogInformation("Creating new author for user subject: {Subject}", subject);

            var userName = GetUserName() ?? subject;

            var newAuthor = new Author
            {
                AccountName = subject,
                Description = $"User: {userName}",
                Role = AuthorRoleType.Author,
                IsActivated = true
            };

            var createResult = await _authorManager.CreateAsync(newAuthor);
            if (!createResult.Success)
            {
                _logger?.LogError("Failed to create author for user {Subject}: {Error}", subject, createResult.ErrorMessage);
                return ProcessingResult<Author>.Fail(
                    $"Failed to create author for user: {createResult.ErrorMessage}",
                    createResult.ErrorType);
            }

            _logger?.LogInformation("Created author for user: {Subject} (Id: {Id})", subject, createResult.Value.Id);
            _cachedAuthor = createResult.Value;
            _initialized = true;
            return ProcessingResult<Author>.Ok(createResult.Value);
        }

        private string GetUserSubject()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return null;
            }

            // Try standard subject claim first
            var subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user.FindFirstValue("sub");

            return subject;
        }

        private string GetUserName()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return null;
            }

            // Try various name claims
            return user.FindFirstValue(ClaimTypes.Name)
                ?? user.FindFirstValue("name")
                ?? user.FindFirstValue("preferred_username")
                ?? user.FindFirstValue(ClaimTypes.Email);
        }
    }
}
