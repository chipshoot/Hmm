using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Utility.Dal.Repository
{
    /// <summary>
    /// The <see cref="IGenericRepository{T, TIdentity}"/> interface defines a standard contract that repository
    /// components should implement for CRUD operations using immutable ProcessingResult pattern.
    /// </summary>
    /// <typeparam name="T">The entity type we want to manage in the repository</typeparam>
    /// <typeparam name="TIdentity">the type of entity identity</typeparam>
    public interface IGenericRepository<T, in TIdentity> where T : AbstractEntity<TIdentity>
    {
        /// <summary>
        /// Gets the list of entities that match criteria asynchronously.
        /// </summary>
        /// <param name="query">The query to search the data source.</param>
        /// <param name="resourceCollectionParameters">Resource collection control information, e.g. pagination, sort and search information.
        /// if null applied system will use default pagination setting for the searching</param>
        /// <returns>
        /// ProcessingResult containing the paginated list of entities that match the criteria, or error information if the operation failed
        /// </returns>
        Task<ProcessingResult<PageList<T>>> GetEntitiesAsync(Expression<Func<T, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Get one entity by id asynchronously
        /// </summary>
        /// <param name="id">The id of the entity</param>
        /// <returns>ProcessingResult containing the entity if found, NotFound if entity doesn't exist, or error information if the operation failed</returns>
        Task<ProcessingResult<T>> GetEntityAsync(TIdentity id);

        /// <summary>
        /// Adds the entity to data source asynchronously.
        /// </summary>
        /// <param name="entity">the entity which will be added</param>
        /// <returns>ProcessingResult containing the newly added entity with id, or error information if the operation failed</returns>
        Task<ProcessingResult<T>> AddAsync(T entity);

        /// <summary>
        /// Updates the specified entity of data source asynchronously.
        /// </summary>
        /// <param name="entity">The entity which will be updated.</param>
        /// <returns>ProcessingResult containing the updated entity, or error information if the operation failed</returns>
        Task<ProcessingResult<T>> UpdateAsync(T entity);

        /// <summary>
        /// Deletes the specified entity from data source asynchronously.
        /// </summary>
        /// <param name="entity">The entity which will be removed.</param>
        /// <returns>ProcessingResult indicating success or failure of the deletion operation</returns>
        Task<ProcessingResult<Unit>> DeleteAsync(T entity);

        /// <summary>
        /// Flushes the cached data to database.
        /// </summary>
        void Flush();
    }
}