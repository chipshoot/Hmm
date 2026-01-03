using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface ITagManager
    {
        Task<ProcessingResult<PageList<Tag>>> GetEntitiesAsync(Expression<Func<Tag, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        Task<bool> IsTagExistsAsync(int id);

        Task<ProcessingResult<Tag>> GetTagByIdAsync(int id);

        Task<ProcessingResult<Tag>> GetTagByNameAsync(string name);

        Task<ProcessingResult<Tag>> CreateAsync(Tag tag);

        Task<ProcessingResult<Tag>> UpdateAsync(Tag tag);

        /// <summary>
        /// Set the flag to deactivate tag to make it invisible for system.
        /// tag may be associated with note, so we not want to delete everything
        /// </summary>
        /// <param name="id">The id of tag whose activate flag will be set</param>
        Task<ProcessingResult<Unit>> DeActivateAsync(int id);
    }
}