// Ignore Spelling: Repo Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class AuthorEfRepository : IRepository<AuthorDao>
    {
        private readonly IHmmDataContext _dataContext;
        private readonly IEntityLookup _lookupRepo;
        private readonly ILogger _logger;

        public AuthorEfRepository(IHmmDataContext dataContext, IEntityLookup lookupRepo, ILogger logger = null)
        {
            ArgumentNullException.ThrowIfNull(dataContext);
            ArgumentNullException.ThrowIfNull(lookupRepo);

            _dataContext = dataContext;
            _lookupRepo = lookupRepo;
            _logger = logger;
        }

        public async Task<ProcessingResult<PageList<AuthorDao>>> GetEntitiesAsync(Expression<Func<AuthorDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var authorsResult = await _lookupRepo.GetEntitiesAsync<AuthorDao>(query, resourceCollectionParameters);
            authorsResult.LogMessages(_logger);
            return authorsResult;
        }

        public async Task<ProcessingResult<AuthorDao>> GetEntityAsync(int id)
        {
            try
            {
                var author = await _dataContext.Set<AuthorDao>().FindAsync(id);

                if (author == null)
                {
                    var result = ProcessingResult<AuthorDao>.EmptyOk($"Author with ID {id} not found");
                    result.LogMessages(_logger);
                    return result;
                }

                var successResult = ProcessingResult<AuthorDao>.Ok(author);
                successResult.LogMessages(_logger);
                return successResult;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<AuthorDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<AuthorDao>> AddAsync(AuthorDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                // Check for duplicate AccountName
                var existingAuthorsResult = await _lookupRepo.GetEntitiesAsync<AuthorDao>(
                    a => a.AccountName == entity.AccountName);
                if (existingAuthorsResult.Success && existingAuthorsResult.Value?.Count > 0)
                {
                    var invalidResult = ProcessingResult<AuthorDao>.Invalid(
                        $"Author with AccountName '{entity.AccountName}' already exists");
                    invalidResult.LogMessages(_logger);
                    return invalidResult;
                }

                // Reset id to 0 to make sure it is a new entity
                entity.Id = 0;
                _dataContext.Set<AuthorDao>().Add(entity);

                var result = ProcessingResult<AuthorDao>.Ok(entity, $"Author '{entity.AccountName}' added to context (pending commit)");
                result.LogMessages(_logger);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<AuthorDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<AuthorDao>> UpdateAsync(AuthorDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                if (entity.Id <= 0)
                {
                    var invalidResult = ProcessingResult<AuthorDao>.Invalid($"Cannot update author with invalid id {entity.Id}");
                    invalidResult.LogMessages(_logger);
                    return invalidResult;
                }

                // Check for duplicate AccountName (excluding the current entity)
                var existingAuthorsResult = await _lookupRepo.GetEntitiesAsync<AuthorDao>(
                    a => a.AccountName == entity.AccountName && a.Id != entity.Id);
                if (existingAuthorsResult.Success && existingAuthorsResult.Value?.Count > 0)
                {
                    var invalidResult = ProcessingResult<AuthorDao>.Invalid(
                        $"Another author with AccountName '{entity.AccountName}' already exists");
                    invalidResult.LogMessages(_logger);
                    return invalidResult;
                }

                _dataContext.Set<AuthorDao>().Update(entity);

                var result = ProcessingResult<AuthorDao>.Ok(entity, $"Author '{entity.AccountName}' updated in context (pending commit)");
                result.LogMessages(_logger);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<AuthorDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<Unit>> DeleteAsync(AuthorDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                _dataContext.Set<AuthorDao>().Remove(entity);

                var result = ProcessingResult<Unit>.Ok(Unit.Value, $"Author '{entity.AccountName}' (ID: {entity.Id}) marked for deletion (pending commit)");
                result.LogMessages(_logger);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<Unit>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public void Flush()
        {
            _dataContext.Commit();
        }
    }
}