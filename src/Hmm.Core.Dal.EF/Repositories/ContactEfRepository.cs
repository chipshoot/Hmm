// Ignore Spelling: Ef

using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hmm.Utility.Dal.Repository;
using ContactDao = Hmm.Core.Dal.EF.DbEntity.ContactDao;

namespace Hmm.Core.Dal.EF.Repositories;

public class ContactEfRepository : IRepository<ContactDao>
{
    private readonly IHmmDataContext _dataContext;
    private readonly IEntityLookup _lookupRepository;

    public ContactEfRepository(IHmmDataContext dataContext, IEntityLookup lookupRepository)
    {
        Guard.Against<ArgumentNullException>(dataContext == null, nameof(dataContext));
        Guard.Against<ArgumentNullException>(lookupRepository == null, nameof(lookupRepository));

        _dataContext = dataContext;
        _lookupRepository = lookupRepository;
    }

    public PageList<ContactDao> GetEntities(Expression<Func<ContactDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
    {
        return _lookupRepository.GetEntities(query, resourceCollectionParameters);
    }

    public async Task<PageList<ContactDao>> GetEntitiesAsync(Expression<Func<ContactDao, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
    {
        var contacts = await _lookupRepository.GetEntitiesAsync(query, resourceCollectionParameters);
        return contacts;
    }

    public ContactDao GetEntity(int id)
    {
        ProcessMessage.Rest();
        try
        {
            return _dataContext.Contacts.Find(id);
        }
        catch (Exception e)
        {
            ProcessMessage.WrapException(e);
            return null;
        }
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

    public ContactDao Add(ContactDao entity)
    {
        Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

        ProcessMessage.Rest();
        try
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            _dataContext.Contacts.Add(entity);
            Flush();
            return entity;
        }
        catch (DataSourceException ex)
        {
            ProcessMessage.WrapException(ex);
            return null;
        }
    }

    public ContactDao Update(ContactDao entity)
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
            Flush();
            var updateContactDb = _lookupRepository.GetEntity<ContactDao>(entity.Id);
            return updateContactDb;
        }
        catch (DataSourceException ex)
        {
            ProcessMessage.WrapException(ex);
            return null;
        }
    }

    public bool Delete(ContactDao entity)
    {
        Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

        ProcessMessage.Rest();
        try
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            _dataContext.Contacts.Remove(entity);
            Flush();
            return true;
        }
        catch (DataSourceException ex)
        {
            ProcessMessage.WrapException(ex);
            return false;
        }
    }

    public async Task<ContactDao> AddAsync(ContactDao entity)
    {
        Guard.Against<ArgumentNullException>(entity == null, nameof(entity));

        ProcessMessage.Rest();
        try
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            _dataContext.Contacts.Add(entity);
            await _dataContext.SaveAsync();
            return entity;
        }
        catch (DataSourceException ex)
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
        catch (DataSourceException ex)
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
        catch (DataSourceException ex)
        {
            ProcessMessage.WrapException(ex);
            return false;
        }
    }

    public void Flush()
    {
        _dataContext.Save();
    }

    public ProcessingResult ProcessMessage { get; } = new();
}