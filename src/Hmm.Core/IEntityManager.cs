using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Misc;
using System.Collections.Generic;

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
        /// Updates the specified entity with new information.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> with update information and id </param>
        /// <returns>if entity has been updated successfully, return updated entity, otherwise return
        ///  null </returns>
        T Update(T entity);

        /// <summary>
        /// Get <see cref="Entity"/> list from data source
        /// </summary>
        IEnumerable<T> GetEntities();

        /// <summary>
        /// The processing result of the interface, contains the flag for result (fail or success) and message
        /// </summary>
        ProcessingResult ProcessResult { get; }
    }
}