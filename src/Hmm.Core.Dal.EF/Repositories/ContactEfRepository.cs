// Ignore Spelling: Ef

using Hmm.Core.Map.DbEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF.Repositories;

public class ContactEfRepository : IRepository<ContactDao>
{
    private readonly IHmmDataContext _dataContext;
    private readonly IEntityLookup _lookupRepository;

    public ContactEfRepository(IHmmDataContext dataContext, IEntityLookup lookupRepository, ILogger logger = null)
    {
        Guard.Against<ArgumentNullException>(dataContext == null, nameof(dataContext));
        Guard.Against<ArgumentNullException>(lookupRepository == null, nameof(lookupRepository));

        _dataContext = dataContext;
        _lookupRepository = lookupRepository;
        ProcessMessage = logger != null ? new ProcessingResult(logger) : new ProcessingResult();
    }

    public async Task<PageList<ContactDao>> GetEntitiesAsync(Expression<Func<ContactDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
    {
        var contacts = await _lookupRepository.GetEntitiesAsync(query, resourceCollectionParameters);
        return contacts;
    }

    public async Task<ContactDao> GetEntityAsync(int id)
    {
        ProcessMessage.Rest();
        try
        {
            var contact = await _dataContext.Contacts.FindAsync(id);
            return contact;
        }
        catch (Exception e)
        {
            ProcessMessage.WrapException(e);
            return null;
        }
    }

    public async Task<ContactDao> AddAsync(ContactDao entity)
    {
        Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

        ProcessMessage.Rest();
        try
        {
            // ReSharper disable once PossibleNullReferenceException
            // reset id to 0 to make sure it is a new entity
            entity.Id = 0;
            _dataContext.Contacts.Add(entity);
            await _dataContext.SaveAsync();
            return entity;
        }
        catch (Exception ex)
        {
            ProcessMessage.WrapException(ex);
            return null;
        }
    }

    public async Task<ContactDao> UpdateAsync(ContactDao entity)
    {
        Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

        ProcessMessage.Rest();
        try
        {
            // ReSharper disable once PossibleNullReferenceException
            if (entity.Id <= 0)
            {
                ProcessMessage.Success = false;
                ProcessMessage.AddErrorMessage($"Can not update contact with id {entity.Id}");
                return null;
            }

            _dataContext.Contacts.Update(entity);
            await _dataContext.SaveAsync();
            var updateContactDb = await _lookupRepository.GetEntityAsync<ContactDao>(entity.Id);
            return updateContactDb;
        }
        catch (Exception ex)
        {
            ProcessMessage.WrapException(ex);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(ContactDao entity)
    {
        Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

        ProcessMessage.Rest();
        try
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            _dataContext.Contacts.Remove(entity);
            await _dataContext.SaveAsync();
            return true;
        }
        catch (Exception ex)
        {
            ProcessMessage.WrapException(ex);
            return false;
        }
    }

    public void Flush()
    {
        _dataContext.Save();
    }

    public ProcessingResult ProcessMessage { get; }
}