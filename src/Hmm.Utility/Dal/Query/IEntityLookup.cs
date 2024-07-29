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
        /// The list of entity that match the criteria
        /// </returns>
        Task<PageList<T>> GetEntitiesAsync<T>(Expression<Func<T, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Gets the int id entity by its id asynchronously.
        /// </summary>
        /// <returns>The entity that get id</returns>
        Task<T> GetEntityAsync<T>(int id) where T : Entity;

        /// <summary>
        /// Hole query processing result
        /// </summary>
        ProcessingResult ProcessResult { get; }
    }
}