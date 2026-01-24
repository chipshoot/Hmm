using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Repository;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Hmm.Core.Dal.EF
{
    /// <summary>
    /// Data context interface that abstracts EF Core implementation details.
    /// Uses generic Set&lt;T&gt;() method instead of exposing DbSet properties directly,
    /// following the Repository pattern best practices.
    /// Implements IUnitOfWork to support proper transaction boundaries.
    /// </summary>
    public interface IHmmDataContext : IUnitOfWork
    {
        /// <summary>
        /// Gets the DbSet for the specified entity type.
        /// This is the single point of access for entity collections, replacing individual DbSet properties.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <returns>DbSet for the entity type</returns>
        DbSet<T> Set<T>() where T : class;

        /// <summary>
        /// Gets the default entity for any type that extends HasDefaultEntity.
        /// This enables Open/Closed principle compliance by avoiding type-specific checks in repositories.
        /// </summary>
        /// <typeparam name="T">Entity type that extends HasDefaultEntity</typeparam>
        /// <returns>The default entity or null if not found</returns>
        Task<T> GetDefaultEntityAsync<T>() where T : HasDefaultEntity;
    }
}