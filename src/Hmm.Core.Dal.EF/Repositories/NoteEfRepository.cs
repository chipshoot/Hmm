// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Hmm.Core.Dal.EF.Repositories
{
    public class NoteEfRepository : IVersionRepository<HmmNoteDao>
    {
        private readonly IHmmDataContext _dataContext;
        private readonly IEntityLookup _lookupRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger _logger;

        public NoteEfRepository(
            IHmmDataContext dataContext,
            IEntityLookup lookupRepository,
            IDateTimeProvider dateTimeProvider,
            ILogger logger = null)
        {
            ArgumentNullException.ThrowIfNull(dataContext);
            ArgumentNullException.ThrowIfNull(lookupRepository);
            ArgumentNullException.ThrowIfNull(dateTimeProvider);

            _dataContext = dataContext;
            _lookupRepository = lookupRepository;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        public async Task<ProcessingResult<PageList<HmmNoteDao>>> GetEntitiesAsync(Expression<Func<HmmNoteDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var (pageIdx, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var notes = query == null
                ? _dataContext.Set<HmmNoteDao>().Include(n => n.Author).Include(n => n.Catalog).Include(n=>n.Tags)
                : _dataContext.Set<HmmNoteDao>().Include(n => n.Author).Include(n => n.Catalog).Include(n=>n.Tags).Where(query);
            var result = resourceCollectionParameters == null
                ? await PageList<HmmNoteDao>.CreateAsync(notes, pageIdx, pageSize)
                : await PageList<HmmNoteDao>.CreateAsync(notes.ApplySort(resourceCollectionParameters.OrderBy), pageIdx, pageSize);

            var processResult = ProcessingResult<PageList<HmmNoteDao>>.Ok(result);
            if (_logger != null)
            {
                processResult.LogMessages(_logger);
            }
            return processResult;
        }

        public async Task<ProcessingResult<HmmNoteDao>> GetEntityAsync(int id)
        {
            try
            {
                var note = await _dataContext.Set<HmmNoteDao>()
                    .Include(n=>n.Tags)
                    .FirstOrDefaultAsync(n=>n.Id == id);

                if (note == null)
                {
                    var result = ProcessingResult<HmmNoteDao>.EmptyOk($"Note with ID {id} not found");
                    result.LogMessages(_logger);
                    return result;
                }

                var successResult = ProcessingResult<HmmNoteDao>.Ok(note);
                successResult.LogMessages(_logger);
                return successResult;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<HmmNoteDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<HmmNoteDao>> AddAsync(HmmNoteDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                // Check if we need to apply default catalog
                var catalogResult = await PropertyCheckingAsync(entity.Catalog);
                if (!catalogResult.Success)
                {
                    var errorResult = ProcessingResult<HmmNoteDao>.Fail($"Cannot find default note catalog: {catalogResult.ErrorMessage}", catalogResult.ErrorType);
                    errorResult.LogMessages(_logger);
                    return errorResult;
                }
                entity.Catalog = catalogResult.Value;

                entity.CreateDate = _dateTimeProvider.UtcNow;
                entity.LastModifiedDate = _dateTimeProvider.UtcNow;
                var savedAuthor = await _dataContext.Set<AuthorDao>().FindAsync(entity.Author.Id);
                if (savedAuthor != null)
                {
                    entity.Author = savedAuthor;
                }

                var savedCat = await _dataContext.Set<NoteCatalogDao>().FindAsync(entity.Catalog.Id);
                if (savedCat != null)
                {
                    entity.Catalog = savedCat;
                }

                // Reset the id to 0 to avoid EF core updating the record
                entity.Id = 0;
                _dataContext.Set<HmmNoteDao>().Add(entity);

                var result = ProcessingResult<HmmNoteDao>.Ok(entity, $"Note '{entity.Subject}' added to context (pending commit)");
                result.LogMessages(_logger);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<HmmNoteDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        private async Task<ProcessingResult<NoteCatalogDao>> PropertyCheckingAsync(NoteCatalogDao property)
        {
            try
            {
                var defaultNeeded = false;
                if (property == null)
                {
                    defaultNeeded = true;
                }
                else if (property.Id <= 0)
                {
                    defaultNeeded = true;
                }
                else
                {
                    var lookupResult = await _lookupRepository.GetEntityAsync<NoteCatalogDao>(property.Id);
                    if (!lookupResult.Success || lookupResult.Value == null)
                    {
                        defaultNeeded = true;
                    }
                }

                if (!defaultNeeded)
                {
                    return ProcessingResult<NoteCatalogDao>.Ok(property);
                }

                var defaultCatalog = await _dataContext.Set<NoteCatalogDao>().FirstOrDefaultAsync(c => c.IsDefault);
                if (defaultCatalog == null)
                {
                    return ProcessingResult<NoteCatalogDao>.NotFound("No default NoteCatalog found");
                }
                return ProcessingResult<NoteCatalogDao>.Ok(defaultCatalog);
            }
            catch (Exception ex)
            {
                return ProcessingResult<NoteCatalogDao>.FromException(ex);
            }
        }

        public async Task<ProcessingResult<HmmNoteDao>> UpdateAsync(HmmNoteDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                // Check if the note exists
                if (entity.Id <= 0)
                {
                    var invalidResult = ProcessingResult<HmmNoteDao>.Invalid($"Cannot update note with invalid id {entity.Id}");
                    invalidResult.LogMessages(_logger);
                    return invalidResult;
                }

                var existingNote = await _dataContext.Set<HmmNoteDao>().AsNoTracking().FirstOrDefaultAsync(n => n.Id == entity.Id);
                if (existingNote == null)
                {
                    var notFoundResult = ProcessingResult<HmmNoteDao>.NotFound($"Note with ID {entity.Id} not found");
                    notFoundResult.LogMessages(_logger);
                    return notFoundResult;
                }

                // Check if we need to apply default catalog
                var catalogResult = await PropertyCheckingAsync(entity.Catalog);
                if (!catalogResult.Success)
                {
                    var errorResult = ProcessingResult<HmmNoteDao>.Fail($"Cannot find default note catalog: {catalogResult.ErrorMessage}", catalogResult.ErrorType);
                    errorResult.LogMessages(_logger);
                    return errorResult;
                }
                entity.Catalog = catalogResult.Value;

                entity.LastModifiedDate = _dateTimeProvider.UtcNow;
                _dataContext.Set<HmmNoteDao>().Update(entity);

                var result = ProcessingResult<HmmNoteDao>.Ok(entity, $"Note '{entity.Subject}' updated in context (pending commit)");
                result.LogMessages(_logger);
                return result;
            }
            catch (Exception ex)
            {
                var errorResult = ProcessingResult<HmmNoteDao>.FromException(ex);
                errorResult.LogMessages(_logger);
                return errorResult;
            }
        }

        public async Task<ProcessingResult<Unit>> DeleteAsync(HmmNoteDao entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            try
            {
                // Check if the note exists
                var existingNote = await _dataContext.Set<HmmNoteDao>().FindAsync(entity.Id);
                if (existingNote == null)
                {
                    var notFoundResult = ProcessingResult<Unit>.NotFound($"Note with ID {entity.Id} not found");
                    notFoundResult.LogMessages(_logger);
                    return notFoundResult;
                }

                _dataContext.Set<HmmNoteDao>().Remove(existingNote);

                var result = ProcessingResult<Unit>.Ok(Unit.Value, $"Note '{entity.Subject}' (ID: {entity.Id}) marked for deletion (pending commit)");
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

        public bool HasPropertyChanged(HmmNoteDao note, string propertyName)
        {
            if (_dataContext is not DbContext dbContext)
            {
                return false;
            }

            var result = (from entry in dbContext.Entry(note).Properties
                          where entry.Metadata.Name == propertyName.Trim()
                          select entry.IsModified).FirstOrDefault();
            return result;
        }

        public void Flush()
        {
            _dataContext.Commit();
        }
    }
}