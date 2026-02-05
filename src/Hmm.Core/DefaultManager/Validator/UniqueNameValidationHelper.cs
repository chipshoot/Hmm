using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
using Hmm.Utility.Specification;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hmm.Core.DefaultManager.Validator
{
    /// <summary>
    /// Provides reusable uniqueness validation logic for entities with unique name constraints.
    /// Eliminates duplicate validation code across AuthorValidator, TagValidator, and NoteCatalogValidator.
    /// </summary>
    public static class UniqueNameValidationHelper
    {
        /// <summary>
        /// Validates that a name is unique for an entity, handling both create and update scenarios.
        /// </summary>
        /// <typeparam name="TEntity">The domain entity type being validated</typeparam>
        /// <typeparam name="TDao">The DAO entity type used by the repository</typeparam>
        /// <param name="repository">The repository to query for existing entities</param>
        /// <param name="entityId">The ID of the entity being validated (0 for new entities)</param>
        /// <param name="name">The name to validate for uniqueness</param>
        /// <param name="nameSelector">Expression to select the name property from the DAO</param>
        /// <param name="additionalFilter">Optional additional filter (e.g., IsActivated check)</param>
        /// <returns>True if the name is unique, false otherwise</returns>
        public static async Task<bool> IsNameUniqueAsync<TEntity, TDao>(
            IGenericRepository<TDao, int> repository,
            int entityId,
            string name,
            Expression<Func<TDao, string>> nameSelector,
            Expression<Func<TDao, bool>> additionalFilter = null)
            where TDao : Entity
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var normalizedName = name.Trim().ToLower();

            // Check if this is an existing entity
            var existingEntityResult = await repository.GetEntityAsync(entityId);
            var isNewEntity = !existingEntityResult.Success || existingEntityResult.IsNotFound;

            // Build the specification for finding matching names
            var nameMatchSpec = new NameMatchSpecification<TDao>(nameSelector, normalizedName, additionalFilter);

            if (isNewEntity)
            {
                // Creating new entity - check if any entity has this name
                var existingResult = await repository.GetEntitiesAsync(nameMatchSpec);
                if (existingResult.Success && !existingResult.IsNotFound && existingResult.Value?.Any() == true)
                {
                    return false;
                }
            }
            else
            {
                // Updating existing entity - check for conflicts with OTHER entities
                var excludeIdSpec = new Specification<TDao>(e => e.Id != entityId);
                var conflictSpec = nameMatchSpec.And(excludeIdSpec);
                var conflictResult = await repository.GetEntitiesAsync(conflictSpec);
                if (conflictResult.Success && !conflictResult.IsNotFound && conflictResult.Value?.Any() == true)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Overload for repositories that implement ICompositeEntityRepository.
        /// </summary>
        public static async Task<bool> IsNameUniqueAsync<TDao, TRelated>(
            ICompositeEntityRepository<TDao, TRelated> repository,
            int entityId,
            string name,
            Expression<Func<TDao, string>> nameSelector,
            Expression<Func<TDao, bool>> additionalFilter = null)
            where TDao : Entity
            where TRelated : Entity
        {
            // Cast to IGenericRepository to reuse the core logic
            return await IsNameUniqueAsync<object, TDao>(
                repository,
                entityId,
                name,
                nameSelector,
                additionalFilter);
        }

    }
}
