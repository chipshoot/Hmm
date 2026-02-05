using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Specification;
using System.Threading.Tasks;

namespace Hmm.Utility.Dal.Repository
{
    /// <summary>
    /// Extension methods that bridge the Specification pattern with the repository layer.
    /// Allows passing <see cref="ISpecification{TEntity}"/> directly to repository queries
    /// instead of manually calling <see cref="ISpecification{TEntity}.ToExpression()"/>.
    /// </summary>
    public static class RepositorySpecificationExtensions
    {
        /// <summary>
        /// Gets entities matching a specification from the repository.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <typeparam name="TIdentity">The identity type.</typeparam>
        /// <param name="repository">The repository to query.</param>
        /// <param name="specification">The specification to filter entities.</param>
        /// <param name="resourceCollectionParameters">Pagination and sorting parameters.</param>
        /// <returns>ProcessingResult containing the matching entities.</returns>
        public static Task<ProcessingResult<PageList<T>>> GetEntitiesAsync<T, TIdentity>(
            this IGenericRepository<T, TIdentity> repository,
            ISpecification<T> specification,
            ResourceCollectionParameters resourceCollectionParameters = null)
            where T : AbstractEntity<TIdentity>
        {
            return repository.GetEntitiesAsync(specification.ToExpression(), resourceCollectionParameters);
        }
    }
}
