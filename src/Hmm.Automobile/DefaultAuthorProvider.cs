using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Specifications;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    /// <summary>
    /// Provides the default author for automobile operations by loading from the database.
    /// </summary>
    /// <remarks>
    /// <para>This provider:</para>
    /// <list type="bullet">
    /// <item><description>Loads the author by account name from configuration</description></item>
    /// <item><description>Optionally creates the author if it doesn't exist</description></item>
    /// <item><description>Caches the author after first retrieval</description></item>
    /// <item><description>Is thread-safe for concurrent access</description></item>
    /// </list>
    /// </remarks>
    public class DefaultAuthorProvider : IDefaultAuthorProvider
    {
        private readonly AutomobileSeedingOptions _options;
        private readonly IAuthorManager _authorManager;
        private readonly ILogger<DefaultAuthorProvider> _logger;

        private Author _cachedAuthor;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _initialized;

        public DefaultAuthorProvider(
            IOptions<AutomobileSeedingOptions> options,
            IAuthorManager authorManager,
            ILogger<DefaultAuthorProvider> logger = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(authorManager);

            _options = options.Value ?? new AutomobileSeedingOptions();
            _authorManager = authorManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public Author CachedAuthor => _cachedAuthor;

        /// <inheritdoc />
        public Task<ProcessingResult<Author>> GetAuthorAsync() => GetDefaultAuthorAsync();

        /// <inheritdoc />
        public async Task<ProcessingResult<Author>> GetDefaultAuthorAsync()
        {
            // Fast path: return cached author if already initialized
            if (_initialized && _cachedAuthor != null)
            {
                return ProcessingResult<Author>.Ok(_cachedAuthor);
            }

            // Slow path: initialize with lock
            await _initLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_initialized && _cachedAuthor != null)
                {
                    return ProcessingResult<Author>.Ok(_cachedAuthor);
                }

                return await InitializeAuthorAsync();
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task<ProcessingResult<Author>> InitializeAuthorAsync()
        {
            var accountName = _options.DefaultAuthorAccountName;

            if (string.IsNullOrWhiteSpace(accountName))
            {
                _logger?.LogError("DefaultAuthorAccountName is not configured");
                return ProcessingResult<Author>.Invalid(
                    "DefaultAuthorAccountName must be configured in Automobile settings");
            }

            _logger?.LogDebug("Looking up default author with account name: {AccountName}", accountName);

            // Try to find existing author by account name
            var spec = new AuthorByAccountNameSpecification(accountName);
            var authorsResult = await _authorManager.GetEntitiesAsync(spec.ToExpression());
            if (!authorsResult.Success)
            {
                _logger?.LogError("Failed to query for default author: {Error}", authorsResult.ErrorMessage);
                return ProcessingResult<Author>.Fail(authorsResult.ErrorMessage, authorsResult.ErrorType);
            }

            Author author = null;
            foreach (var a in authorsResult.Value)
            {
                author = a;
                break;
            }

            if (author != null)
            {
                _logger?.LogInformation("Default author found: {AccountName} (Id: {Id})", author.AccountName, author.Id);
                _cachedAuthor = author;
                _initialized = true;
                return ProcessingResult<Author>.Ok(author);
            }

            // Author not found - create if configured to do so
            if (!_options.CreateDefaultAuthorIfMissing)
            {
                _logger?.LogError("Default author '{AccountName}' not found and CreateDefaultAuthorIfMissing is false", accountName);
                return ProcessingResult<Author>.NotFound(
                    $"Default author with account name '{accountName}' not found. " +
                    "Either create the author in the database or set CreateDefaultAuthorIfMissing=true in configuration.");
            }

            _logger?.LogInformation("Creating default author: {AccountName}", accountName);

            var newAuthor = new Author
            {
                AccountName = accountName,
                Description = "Automobile module default author (auto-created)",
                Role = AuthorRoleType.Author,
                IsActivated = true
            };

            var createResult = await _authorManager.CreateAsync(newAuthor);
            if (!createResult.Success)
            {
                _logger?.LogError("Failed to create default author: {Error}", createResult.ErrorMessage);
                return ProcessingResult<Author>.Fail(
                    $"Failed to create default author: {createResult.ErrorMessage}",
                    createResult.ErrorType);
            }

            _logger?.LogInformation("Default author created successfully: {AccountName} (Id: {Id})",
                createResult.Value.AccountName, createResult.Value.Id);

            _cachedAuthor = createResult.Value;
            _initialized = true;
            return ProcessingResult<Author>.Ok(createResult.Value);
        }
    }
}
