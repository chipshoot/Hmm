// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
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
            Guard.Against<ArgumentNullException>(dataContext == null, nameof(dataContext));
            Guard.Against<ArgumentNullException>(lookupRepository == null, nameof(lookupRepository));

            _dataContext = dataContext;
            _lookupRepository = lookupRepository;
            _logger = logger;
        }

        public async Task<ProcessingResult<PageList<TagDao>>> GetEntitiesAsync(Expression<Func<TagDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var entities = _dataContext.Tags;

            var result = query == null
                ? await PageList<TagDao>.CreateAsync(entities, pageIdx, pageSize)
                : await PageList<TagDao>.CreateAsync(entities.Where(query), pageIdx, pageSize);

            var processResult = ProcessingResult<PageList<TagDao>>.Ok(result);
            if (_logger != null)
            {
                processResult.LogMessages(_logger);
            }
            return processResult;
        }

        public async Task<ProcessingResult<PageList<HmmNoteDao>>> GetNoteByTagAsync(int tagId, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            try
            {
                var tag = await _dataContext.Tags.Include(t => t.Notes).AsNoTracking().FirstOrDefaultAsync(t => t.Id == tagId);

                if (tag == null)
                {
                    var notFoundResult = ProcessingResult<PageList<HmmNoteDao>>.NotFound($"Tag with ID {tagId} not found");
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
                var tag = await _dataContext.Tags
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
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                _dataContext.Tags.Add(entity);
                await _dataContext.SaveAsync();

                var result = ProcessingResult<TagDao>.Ok(entity, $"Tag '{entity.Name}' created successfully");
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
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                if (entity.Id <= 0)
                {
                    var invalidResult = ProcessingResult<TagDao>.Invalid($"Cannot update Tag with invalid id {entity.Id}");
                    invalidResult.LogMessages(_logger);
                    return invalidResult;
                }

                _dataContext.Tags.Update(entity);
                await _dataContext.SaveAsync();

                var updatedTagResult = await _lookupRepository.GetEntityAsync<TagDao>(entity.Id);
                if (!updatedTagResult.Success)
                {
                    var errorResult = ProcessingResult<TagDao>.Fail(
                        $"Tag updated but failed to retrieve: {updatedTagResult.ErrorMessage}",
                        updatedTagResult.ErrorType);
                    errorResult.LogMessages(_logger);
                    return errorResult;
                }

                var result = ProcessingResult<TagDao>.Ok(updatedTagResult.Value, $"Tag '{entity.Name}' updated successfully");
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
            Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

            try
            {
                _dataContext.Tags.Remove(entity);
                await _dataContext.SaveAsync();

                var result = ProcessingResult<Unit>.Ok(Unit.Value, $"Tag '{entity.Name}' (ID: {entity.Id}) deleted successfully");
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