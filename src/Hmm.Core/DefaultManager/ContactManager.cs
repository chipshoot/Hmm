//using System;
//using System.Linq.Expressions;
//using System.Threading.Tasks;
//using Hmm.Core.DomainEntity;
//using Hmm.Core.Map.DbEntity;
//using Hmm.Utility.Dal.Query;
//using Hmm.Utility.Dal.Repository;
//using Hmm.Utility.Misc;
//using Hmm.Utility.Validation;
//using Microsoft.EntityFrameworkCore.Metadata;

//namespace Hmm.Core.DefaultManager;

//public class ContactManager: IContactManager
//{
//    private readonly IRepository<ContactDao> _contactDaoRepository;
//    public ContactManager(IRepository<ContactDao> contactRepository, IMapper  mapper)
//    {
//        Guard.Against<ArgumentNullException>(contactRepository==null, nameof(contactRepository));
//        _contactDaoRepository= contactRepository;
//    }
//    public PageList<Contact> GetContacts(Expression<Func<Contact, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
//    {
        
//        throw new NotImplementedException();
//    }

//    public Task<PageList<Contact>> GetContactsAsync(Expression<Func<Contact, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null)
//    {
//        throw new NotImplementedException();
//    }

//    public bool IsContactExists(int id)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<bool> IsContactExistsAsync(int id)
//    {
//        throw new NotImplementedException();
//    }

//    public Contact GetContactById(int id)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<Contact> GetContactByIdAsync(int id)
//    {
//        throw new NotImplementedException();
//    }

//    public Contact Create(Contact contactInfo)
//    {
//        throw new NotImplementedException();
//    }

//    public Contact Update(Contact contactInfo)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<Contact> UpdateAsync(Contact contactInfo)
//    {
//        throw new NotImplementedException();
//    }

//    public void DeActivate(int id)
//    {
//        throw new NotImplementedException();
//    }

//    public Task DeActivateAsync(int id)
//    {
//        throw new NotImplementedException();
//    }

//    public ProcessingResult ProcessResult { get; }
//}