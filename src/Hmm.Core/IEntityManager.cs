using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core
{
    public interface IEntityManager<T> where T : Entity
    {
        /// <summary>
        /// Creates the specified Hmm entity with render information.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> object which contains all
        /// new entity information except entity id.</param>
        /// <returns>if entity successfully be created, return the entity with unique id,
        /// otherwise return null</returns>
        T Create(T entity);

        /// <summary>
        /// Creates the specified Hmm entity with render information asynchronously.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> object which contains all
        /// new entity information except entity id.</param>
        /// <returns>if entity successfully be created, return the entity with unique id,
        /// otherwise return null</returns>
        Task<T> CreateAsync(T entity);

        /// <summary>
        /// Updates the specified entity with new information.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> with update information and id </param>
        /// <returns>if entity has been updated successfully, return updated entity, otherwise return
        ///  null </returns>
        T Update(T entity);

        /// <summary>
        /// Updates the specified entity with new information asynchronously.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> with update information and id </param>
        /// <returns>if entity has been updated successfully, return updated entity, otherwise return
        ///  null </returns>
        Task<T> UpdateAsync(T entity);

        /// <summary>
        /// Get <see cref="Entity"/> list from data source
        /// </summary>
        IEnumerable<T> GetEntities(Expression<Func<T, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Get <see cref="Entity"/> list from data source asynchronously.
        /// </summary>
        Task<IEnumerable<T>> GetEntitiesAsync(Expression<Func<T, bool>> query = null, ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Get <see cref="Entity"/> with id
        /// </summary>
        /// <param name="id">Entity id</param>
        /// <returns>Entity with id, null if no entity found</returns>
        T GetEntityById(int id);

        /// <summary>
        /// Get <see cref="Entity"/> with id asynchronously.
        /// </summary>
        /// <param name="id">Entity id</param>
        /// <returns>Entity with id, null if no entity found</returns>
        Task<T> GetEntityByIdAsync(int id);

        /// <summary>
        /// The processing result of the interface, contains the flag for result (fail or success) and message
        /// </summary>
        ProcessingResult ProcessResult { get; }
    }
}