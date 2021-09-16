using System;
using System.Linq;
using System.Linq.Expressions;
using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Misc;

namespace Hmm.Utility.Dal.Query
{
    /// <summary>
    /// The interface is for quick check entity by its identity
    /// </summary>
    public interface IEntityLookup
    {
        /// <summary>
        /// Gets the a int id entity by its id.
        /// </summary>
        /// <returns>The entity that get id</returns>
        T GetEntity<T>(int id) where T : Entity;

        /// <summary>
        /// Gets the a GUID id entity by its id
        /// </summary>
        /// <returns>The entity that get id</returns>
        T GetEntity<T>(Guid id) where T : GuidEntity;

        /// <summary>
        /// Gets the list of entities that match criteria.
        /// </summary>
        /// <param name="query">The query to search the data source.</param>
        /// <returns>
        /// The list of entity that match the criteria
        /// </returns>
        IQueryable<T> GetEntities<T>(Expression<Func<T, bool>> query = null);

        /// <summary>
        /// Hole query processing result
        /// </summary>
        ProcessingResult ProcessResult { get; }
    }
}