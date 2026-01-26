// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories;

public class ContactEfRepository : IRepository<ContactDao>
{
    private readonly IHmmDataContext _dataContext;
    private readonly IEntityLookup _lookupRepository;
    private readonly ILogger _logger;

    public ContactEfRepository(IHmmDataContext dataContext, IEntityLookup lookupRepository, ILogger logger = null)
    {
        ArgumentNullException.ThrowIfNull(dataContext);
        ArgumentNullException.ThrowIfNull(lookupRepository);

        _dataContext = dataContext;
        _lookupRepository = lookupRepository;
        _logger = logger;
    }

    public async Task<ProcessingResult<PageList<ContactDao>>> GetEntitiesAsync(Expression<Func<ContactDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
    {
        var contactsResult = await _lookupRepository.GetEntitiesAsync<ContactDao>(query, resourceCollectionParameters);
        contactsResult.LogMessages(_logger);
        return contactsResult;
    }

    public async Task<ProcessingResult<ContactDao>> GetEntityAsync(int id)
    {
        try
        {
            var contact = await _dataContext.Set<ContactDao>().FindAsync(id);

            if (contact == null)
            {
                var result = ProcessingResult<ContactDao>.EmptyOk($"Contact with ID {id} not found");
                result.LogMessages(_logger);
                return result;
            }

            var successResult = ProcessingResult<ContactDao>.Ok(contact);
            successResult.LogMessages(_logger);
            return successResult;
        }
        catch (Exception ex)
        {
            var errorResult = ProcessingResult<ContactDao>.FromException(ex);
            errorResult.LogMessages(_logger);
            return errorResult;
        }
    }

    public async Task<ProcessingResult<ContactDao>> AddAsync(ContactDao entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Reset id to 0 to make sure it is a new entity
            entity.Id = 0;
            _dataContext.Set<ContactDao>().Add(entity);

            var result = ProcessingResult<ContactDao>.Ok(entity, $"Contact for '{entity.Contact}' added to context (pending commit)");
            result.LogMessages(_logger);
            return result;
        }
        catch (Exception ex)
        {
            var errorResult = ProcessingResult<ContactDao>.FromException(ex);
            errorResult.LogMessages(_logger);
            return errorResult;
        }
    }

    public async Task<ProcessingResult<ContactDao>> UpdateAsync(ContactDao entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            if (entity.Id <= 0)
            {
                var invalidResult = ProcessingResult<ContactDao>.Invalid($"Cannot update contact with invalid id {entity.Id}");
                invalidResult.LogMessages(_logger);
                return invalidResult;
            }

            _dataContext.Set<ContactDao>().Update(entity);

            var result = ProcessingResult<ContactDao>.Ok(entity, $"Contact for '{entity.Contact}' updated in context (pending commit)");
            result.LogMessages(_logger);
            return result;
        }
        catch (Exception ex)
        {
            var errorResult = ProcessingResult<ContactDao>.FromException(ex);
            errorResult.LogMessages(_logger);
            return errorResult;
        }
    }

    public async Task<ProcessingResult<Unit>> DeleteAsync(ContactDao entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            // Check if the entity exists
            var existingContact = await _dataContext.Set<ContactDao>().FindAsync(entity.Id);
            if (existingContact == null)
            {
                var notFoundResult = ProcessingResult<Unit>.NotFound($"Contact with ID {entity.Id} not found");
                notFoundResult.LogMessages(_logger);
                return notFoundResult;
            }

            _dataContext.Set<ContactDao>().Remove(existingContact);

            var result = ProcessingResult<Unit>.Ok(Unit.Value, $"Contact for '{entity.Contact}' (ID: {entity.Id}) marked for deletion (pending commit)");
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