using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Utility.Dal.Repository
{
    /// <summary>
    /// The <see cref="IGenericRepository{T, TIdentity}"/> interface defines a standard contract that repository
    /// components should implement for CRUD.
    /// </summary>
    /// <typeparam name="T">The entity type we want to managed in the repository</typeparam>
    /// <typeparam name="TIdentity">the type of entity identity</typeparam>
    public interface IGenericRepository<T, in TIdentity> where T : AbstractEntity<TIdentity>
    {
        /// <summary>
        /// Gets the list of entities that match criteria.
        /// </summary>
        /// <param name="query">The query to search the data source.</param>
        /// <returns>
        /// The list of entity that match the criteria
        /// </returns>
        IQueryable<T> GetEntities(Expression<Func<T, bool>> query = null);

        /// <summary>
        /// Gets the list of entities that match criteria asynchronously.
        /// </summary>
        /// <param name="query">The query to search the data source.</param>
        /// <returns>
        /// The list of entity that match the criteria
        /// </returns>
        Task<IEnumerable<T>> GetEntitiesAsync(Expression<Func<T, bool>> query = null);

        /// <summary>
        /// Get one entity by id
        /// </summary>
        /// <param name="id">The id of the entity</param>
        /// <returns>The entity with id if entity found in data source, otherwise null</returns>
        T GetEntity(TIdentity id);

        /// <summary>
        /// Get one entity by id asynchronously
        /// </summary>
        /// <param name="id">The id of the entity</param>
        /// <returns>The entity with id if entity found in data source, otherwise null</returns>
        Task<T> GetEntityAsync(TIdentity id);

        /// <summary>
        /// Adds the entity to data source.
        /// </summary>
        /// <param name="entity">the entity which will be added</param>
        /// <returns>The new added entity with id</returns>
        T Add(T entity);

        /// <summary>
        /// Updates the specified entity of data source.
        /// </summary>
        /// <param name="entity">The entity which will be updated.</param>
        /// <returns>The new added entity with id</returns>
        T Update(T entity);

        /// <summary>
        /// Deletes the specified entity from data source.
        /// </summary>
        /// <param name="entity">The entity which will be removed.</param>
        /// <returns>True if delete successfully, otherwise false</returns>
        bool Delete(T entity);

        /// <summary>
        /// Adds the entity to data source asynchronously.
        /// </summary>
        /// <param name="entity">the entity which will be added</param>
        /// <returns>The new added entity with id</returns>
        Task<T> AddAsync(T entity);

        /// <summary>
        /// Updates the specified entity of data source asynchronously.
        /// </summary>
        /// <param name="entity">The entity which will be updated.</param>
        /// <returns>The new added entity with id</returns>
        Task<T> UpdateAsync(T entity);

        /// <summary>
        /// Deletes the specified entity from data source asynchronously.
        /// </summary>
        /// <param name="entity">The entity which will be removed.</param>
        /// <returns>True if delete successfully, otherwise false</returns>
        Task<bool> DeleteAsync(T entity);

        /// <summary>
        /// Flushes the cached data to database.
        /// </summary>
        void Flush();

        /// <summary>
        /// Gets the process message.
        /// </summary>
        /// <value>
        /// The process message.
        /// </value>
        ProcessingResult ProcessMessage { get; }
    }
}