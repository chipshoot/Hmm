using Hmm.Core.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface IContactManager
    {
        PageList<Contact> GetContacts(Expression<Func<Contact, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        Task<PageList<Contact>> GetContactsAsync(Expression<Func<Contact, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        bool IsContactExists(int id);

        Task<bool> IsContactExistsAsync(int id);

        Contact GetContactById(int id);

        Task<Contact> GetContactByIdAsync(int id);
        Contact Create(Contact contactInfo);


        Contact Update(Contact contactInfo);

        Task<Contact> UpdateAsync(Contact contactInfo);

        /// <summary>
        /// Set the flag to deactivate author to make it invisible for system.
        /// author may be associated with note, so we not want to delete everything
        /// </summary>
        /// <param name="id">The id of author whose activate flag will be set</param>
        void DeActivate(int id);

        Task DeActivateAsync(int id);

        ProcessingResult ProcessResult { get; }
    }
}