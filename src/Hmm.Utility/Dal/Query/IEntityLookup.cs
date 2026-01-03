using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Misc;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Utility.Dal.Query
{
    /// <summary>
    /// The interface is for quick check entity by its identity
    /// </summary>
    public interface IEntityLookup
    {
        /// <summary>
        /// Gets the list of entities that match criteria asynchronously.
        /// </summary>
        /// <param name="query">The query to search the data source.</param>
        /// <param name="resourceCollectionParameters">Resource collection control information, e.g. pagination, sort and search information.
        /// if null applied system will use default pagination setting for the searching</param>
        /// <returns>
        /// ProcessingResult containing the list of entities that match the criteria, or error information if the operation failed
        /// </returns>
        Task<ProcessingResult<PageList<T>>> GetEntitiesAsync<T>(Expression<Func<T, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Gets the entity by its id asynchronously.
        /// </summary>
        /// <param name="id">The entity id</param>
        /// <returns>ProcessingResult containing the entity if found, NotFound if entity doesn't exist, or error information if the operation failed</returns>
        Task<ProcessingResult<T>> GetEntityAsync<T>(int id) where T : Entity;
    }
}