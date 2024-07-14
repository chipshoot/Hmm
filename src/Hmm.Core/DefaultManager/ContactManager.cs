using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;

namespace Hmm.Core.DefaultManager;

public class ContactManager: IContactManager
{
    public PageList<Contact> GetContacts(Expression<Func<Contact, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
    {
        throw new NotImplementedException();
    }

    public Task<PageList<Contact>> GetContactsAsync(Expression<Func<Contact, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
    {
        throw new NotImplementedException();
    }

    public bool IsContactExists(int id)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsContactExistsAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Contact GetContactById(int id)
    {
        throw new NotImplementedException();
    }

    public Task<Contact> GetContactByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public Contact Create(Contact contactInfo)
    {
        throw new NotImplementedException();
    }

    public Contact Update(Contact contactInfo)
    {
        throw new NotImplementedException();
    }

    public Task<Contact> UpdateAsync(Contact contactInfo)
    {
        throw new NotImplementedException();
    }

    public void DeActivate(int id)
    {
        throw new NotImplementedException();
    }

    public Task DeActivateAsync(int id)
    {
        throw new NotImplementedException();
    }

    public ProcessingResult ProcessResult { get; }
}