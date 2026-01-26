// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class NoteCatalogEfRepository : IRepository<NoteCatalogDao>
    {
        private readonly IHmmDataContext _dataContext;
        private readonly IEntityLookup _lookupRepository;
        private readonly ILogger _logger;

        public NoteCatalogEfRepository(
            IHmmDataContext dataContext,
            IEntityLookup lookupRepository,
            IDateTimeProvider dateTimeProvider,
            ILogger logger = null)
        {
            ArgumentNullException.ThrowIfNull(dataContext);
            ArgumentNullException.ThrowIfNull(lookupRepository);

            _dataContext = dataContext;
            _lookupRepository = lookupRepository;
            _logger = logger;
        }

        public async Task<ProcessingResult<PageList<NoteCatalogDao>>> GetEntitiesAsync(Expression<Func<NoteCatalogDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var entities = _dataContext.Set<NoteCatalogDao>();

            var result = query == null
                ? await PageList<NoteCatalogDao>.CreateAsync(entities, pageIdx, pageSize)
                : await PageList<NoteCatalogDao>.CreateAsync(entities.Where(query), pageIdx, pageSize);

            var processResult = ProcessingResult<PageList<NoteCatalogDao>>.Ok(result);
            processResult.LogMessages(_logger);
            return processResult;
        }

        public async Task<ProcessingResult<NoteCatalogDao>> GetEntityAsync(int id)
        {
            try
            {
                var catalog = await _dataContext.Set<NoteCatalogDao>().FindAsync(id);

                if (catalog == null)
                {
                    var result = ProcessingResult<NoteCatalogDao>.EmptyOk($"NoteCatalog with ID {id} not found");
                    result.LogMessages(_logger);
                    return result;
                }

                var successResult = ProcessingResult<NoteCatalogDao>.Ok(catalog);
                successResult.LogMessages(_logger);
                return successResult;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<NoteCatalogDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<NoteCatalogDao>> AddAsync(NoteCatalogDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                // Reset the id to 0 to make sure it is a new entity
                entity.Id = 0;
                _dataContext.Set<NoteCatalogDao>().Add(entity);

                var result = ProcessingResult<NoteCatalogDao>.Ok(entity, $"NoteCatalog '{entity.Name}' added to context (pending commit)");
                result.LogMessages(_logger);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<NoteCatalogDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<NoteCatalogDao>> UpdateAsync(NoteCatalogDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                if (entity.Id <= 0)
                {
                    var invalidResult = ProcessingResult<NoteCatalogDao>.Invalid($"Cannot update NoteCatalog with invalid id {entity.Id}");
                    invalidResult.LogMessages(_logger);
                    return invalidResult;
                }

                _dataContext.Set<NoteCatalogDao>().Update(entity);

                var result = ProcessingResult<NoteCatalogDao>.Ok(entity, $"NoteCatalog '{entity.Name}' updated in context (pending commit)");
                result.LogMessages(_logger);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<NoteCatalogDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<Unit>> DeleteAsync(NoteCatalogDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                _dataContext.Set<NoteCatalogDao>().Remove(entity);

                var result = ProcessingResult<Unit>.Ok(Unit.Value, $"NoteCatalog '{entity.Name}' (ID: {entity.Id}) marked for deletion (pending commit)");
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