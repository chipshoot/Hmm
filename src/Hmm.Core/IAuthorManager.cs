using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface IAuthorManager
    {
        Task<ProcessingResult<PageList<Author>>> GetEntitiesAsync(Expression<Func<Author, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        Task<bool> IsAuthorExistsAsync(int id);

        Task<ProcessingResult<Author>> GetAuthorByIdAsync(int id);

        Task<ProcessingResult<Author>> CreateAsync(Author authorInfo);

        Task<ProcessingResult<Author>> UpdateAsync(Author authorInfo);

        /// <summary>
        /// Set the flag to deactivate author to make it invisible for system.
        /// author may be associated with note, so we not want to delete everything
        /// </summary>
        /// <param name="id">The id of author whose activate flag will be set</param>
        Task<ProcessingResult<Unit>> DeActivateAsync(int id);
    }
}