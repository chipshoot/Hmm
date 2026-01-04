// Ignore Spelling: Repo Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
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

            if (_logger != null && authorsResult.Success)
            {
                authorsResult.LogMessages(_logger);
            }

            return authorsResult;
        }

        public async Task<ProcessingResult<AuthorDao>> GetEntityAsync(int id)
        {
            try
            {
                var author = await _dataContext.Authors.FindAsync(id);

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
                // Reset id to 0 to make sure it is a new entity
                entity.Id = 0;
                _dataContext.Authors.Add(entity);
                await _dataContext.SaveAsync();

                var result = ProcessingResult<AuthorDao>.Ok(entity, $"Author '{entity.AccountName}' created successfully");
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

                _dataContext.Authors.Update(entity);
                await _dataContext.SaveAsync();

                var updatedAuthorResult = await _lookupRepo.GetEntityAsync<AuthorDao>(entity.Id);
                if (!updatedAuthorResult.Success)
                {
                    var errorResult = ProcessingResult<AuthorDao>.Fail(
                        $"Author updated but failed to retrieve: {updatedAuthorResult.ErrorMessage}",
                        updatedAuthorResult.ErrorType);
                    errorResult.LogMessages(_logger);
                    return errorResult;
                }

                var result = ProcessingResult<AuthorDao>.Ok(updatedAuthorResult.Value, $"Author '{entity.AccountName}' updated successfully");
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
                _dataContext.Authors.Remove(entity);
                await _dataContext.SaveAsync();

                var result = ProcessingResult<Unit>.Ok(Unit.Value, $"Author '{entity.AccountName}' (ID: {entity.Id}) deleted successfully");
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
            _dataContext.Save();
        }
    }
}