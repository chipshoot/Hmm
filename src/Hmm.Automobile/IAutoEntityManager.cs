using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public interface IAutoEntityManager<T> where T : AutomobileBase
    {
        INoteSerializer<T> NoteSerializer { get; }

        IHmmValidator<T> Validator { get; }

        /// <summary>
        /// Gets the author provider for resolving the author used in automobile operations.
        /// This can be either the default author provider or the current user provider.
        /// </summary>
        IAuthorProvider AuthorProvider { get; }

        Task<ProcessingResult<T>> GetEntityByIdAsync(int id);

        Task<ProcessingResult<PageList<T>>> GetEntitiesAsync(ResourceCollectionParameters resourceCollectionParameter = null);

        /// <summary>
        /// Creates a new entity in the data source.
        /// </summary>
        /// <param name="entity">The entity to create.</param>
        /// <param name="commitChanges">
        /// If true (default), changes are committed immediately.
        /// If false, changes are tracked but not committed - caller must use IUnitOfWork.CommitAsync() to persist.
        /// </param>
        Task<ProcessingResult<T>> CreateAsync(T entity, bool commitChanges = true);

        /// <summary>
        /// Updates an existing entity in the data source.
        /// </summary>
        /// <param name="entity">The entity with updated values.</param>
        /// <param name="commitChanges">
        /// If true (default), changes are committed immediately.
        /// If false, changes are tracked but not committed - caller must use IUnitOfWork.CommitAsync() to persist.
        /// </param>
        Task<ProcessingResult<T>> UpdateAsync(T entity, bool commitChanges = true);

        Task<bool> IsEntityOwnerAsync(int id);
    }
}
