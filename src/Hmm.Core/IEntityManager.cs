using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface IEntityManager<T> where T : Entity
    {
        /// <summary>
        /// Creates the specified Hmm entity with render information asynchronously.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> object which contains all
        /// new entity information except entity id.</param>
        /// <returns>ProcessingResult containing the created entity with unique id, or error information if the operation failed</returns>
        Task<ProcessingResult<T>> CreateAsync(T entity);

        /// <summary>
        /// Updates the specified entity with new information asynchronously.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> with update information and id </param>
        /// <returns>ProcessingResult containing the updated entity, or error information if the operation failed</returns>
        Task<ProcessingResult<T>> UpdateAsync(T entity);

        /// <summary>
        /// Get <see cref="Entity"/> list from data source asynchronously.
        /// </summary>
        /// <returns>ProcessingResult containing the paginated list of entities, or error information if the operation failed</returns>
        Task<ProcessingResult<PageList<T>>> GetEntitiesAsync(Expression<Func<T, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Get <see cref="Entity"/> with id asynchronously.
        /// </summary>
        /// <param name="id">Entity id</param>
        /// <returns>ProcessingResult containing the entity if found, NotFound if entity doesn't exist, or error information if the operation failed</returns>
        Task<ProcessingResult<T>> GetEntityByIdAsync(int id);
    }
}