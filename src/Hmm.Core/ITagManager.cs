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
        Task<PageList<Tag>> GetEntitiesAsync(Expression<Func<Tag, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        Task<bool> TagExistsAsync(int id);

        Task<Tag> GetTagByIdAsync(int id);

        Task<Tag> GetTagByNameAsync(string name);

        Task<Tag> CreateAsync(Tag tag);

        Task<Tag> UpdateAsync(Tag tag);

        /// <summary>
        /// Set the flag to deactivate author to make it invisible for system.
        /// author may be associated with note, so we not want to delete everything
        /// </summary>
        /// <param name="id">The id of author whose activate flag will be set</param>
        Task DeActivateAsync(int id);

        ProcessingResult ProcessResult { get; }
    }
}