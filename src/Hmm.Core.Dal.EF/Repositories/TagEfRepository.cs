// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class TagEfRepository : ICompositeEntityRepository<TagDao, HmmNoteDao>
    {
        private readonly IHmmDataContext _dataContext;
        private readonly IEntityLookup _lookupRepository;
        private readonly ILogger _logger;

        public TagEfRepository(
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

        public async Task<ProcessingResult<PageList<TagDao>>> GetEntitiesAsync(Expression<Func<TagDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var entities = _dataContext.Set<TagDao>();

            var result = query == null
                ? await PageList<TagDao>.CreateAsync(entities, pageIdx, pageSize)
                : await PageList<TagDao>.CreateAsync(entities.Where(query), pageIdx, pageSize);

            var processResult = ProcessingResult<PageList<TagDao>>.Ok(result);
            processResult.LogMessages(_logger);
            return processResult;
        }

        public async Task<ProcessingResult<PageList<HmmNoteDao>>> GetNoteByTagAsync(int tagId, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var tag = await _dataContext.Set<TagDao>().Include(t => t.Notes).AsNoTracking().FirstOrDefaultAsync(t => t.Id == tagId);

                if (tag == null)
                {
                    var notFoundResult = ProcessingResult<PageList<HmmNoteDao>>.EmptyOk($"Tag with ID {tagId} not found");
                    notFoundResult.LogMessages(_logger);
                    return notFoundResult;
                }

                PageList<HmmNoteDao> notePage;
                if (resourceCollectionParameters != null)
                {
                    var noteList = tag.Notes
                        .Skip((resourceCollectionParameters.PageNumber - 1) * resourceCollectionParameters.PageSize)
                        .Take(resourceCollectionParameters.PageSize)
                        .Select(r => r.Note).ToList();
                    notePage = new PageList<HmmNoteDao>(noteList, tag.Notes.Count(), resourceCollectionParameters.PageNumber,
                        resourceCollectionParameters.PageSize);
                }
                else
                {
                    var noteList = tag.Notes.Select(r => r.Note).ToList();
                    notePage = new PageList<HmmNoteDao>(noteList, 1, 1, tag.Notes.Count());
                }

                var result = ProcessingResult<PageList<HmmNoteDao>>.Ok(notePage);
                result.LogMessages(_logger);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<PageList<HmmNoteDao>>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<TagDao>> GetEntityAsync(int id)
        {
            try
            {
                var tag = await _dataContext.Set<TagDao>()
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tag == null)
                {
                    var result = ProcessingResult<TagDao>.NotFound($"Tag with ID {id} not found");
                    result.LogMessages(_logger);
                    return result;
                }

                var successResult = ProcessingResult<TagDao>.Ok(tag);
                successResult.LogMessages(_logger);
                return successResult;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<TagDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<TagDao>> AddAsync(TagDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                // Check for duplicate tag name
                var existingTagsResult = await _lookupRepository.GetEntitiesAsync<TagDao>(
                    t => t.Name == entity.Name);
                if (existingTagsResult.Success && existingTagsResult.Value?.Count > 0)
                {
                    var invalidResult = ProcessingResult<TagDao>.Invalid(
                        $"Tag with name '{entity.Name}' already exists");
                    invalidResult.LogMessages(_logger);
                    return invalidResult;
                }

                _dataContext.Set<TagDao>().Add(entity);

                var result = ProcessingResult<TagDao>.Ok(entity, $"Tag '{entity.Name}' added to context (pending commit)");
                result.LogMessages(_logger);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<TagDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<TagDao>> UpdateAsync(TagDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                if (entity.Id <= 0)
                {
                    var invalidResult = ProcessingResult<TagDao>.Invalid($"Cannot update Tag with invalid id {entity.Id}");
                    invalidResult.LogMessages(_logger);
                    return invalidResult;
                }

                // Check for duplicate tag name (excluding current entity)
                var existingTagsResult = await _lookupRepository.GetEntitiesAsync<TagDao>(
                    t => t.Name == entity.Name && t.Id != entity.Id);
                if (existingTagsResult.Success && existingTagsResult.Value?.Count > 0)
                {
                    var invalidResult = ProcessingResult<TagDao>.Invalid(
                        $"Another tag with name '{entity.Name}' already exists");
                    invalidResult.LogMessages(_logger);
                    return invalidResult;
                }

                _dataContext.Set<TagDao>().Update(entity);

                var result = ProcessingResult<TagDao>.Ok(entity, $"Tag '{entity.Name}' updated in context (pending commit)");
                result.LogMessages(_logger);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<TagDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<Unit>> DeleteAsync(TagDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                _dataContext.Set<TagDao>().Remove(entity);

                var result = ProcessingResult<Unit>.Ok(Unit.Value, $"Tag '{entity.Name}' (ID: {entity.Id}) marked for deletion (pending commit)");
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