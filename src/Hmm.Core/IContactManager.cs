using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface IContactManager
    {
        Task<PageList<Contact>> GetContactsAsync(Expression<Func<Contact, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        Task<bool> IsContactExistsAsync(int id);

        Task<Contact> GetContactByIdAsync(int id);

        Task<Contact> CreateAsync(Contact contactInfo);

        Task<Contact> UpdateAsync(Contact contactInfo);

        /// <summary>
        /// Set the flag to deactivate contact to make it invisible for system.
        /// contact may be associated with author, so we not want to delete everything
        /// </summary>
        /// <param name="id">The id of author whose activate flag will be set</param>
        Task DeActivateAsync(int id);

        ProcessingResult ProcessResult { get; }
    }
}