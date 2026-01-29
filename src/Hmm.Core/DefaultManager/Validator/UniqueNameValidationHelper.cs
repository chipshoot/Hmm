using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Repository;
using Hmm.Utility.Misc;
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

            // Build the query for finding matching names
            var matchingNamesQuery = BuildNameMatchQuery(nameSelector, normalizedName, additionalFilter);

            if (isNewEntity)
            {
                // Creating new entity - check if any entity has this name
                var existingResult = await repository.GetEntitiesAsync(matchingNamesQuery);
                if (existingResult.Success && !existingResult.IsNotFound && existingResult.Value?.Any() == true)
                {
                    return false;
                }
            }
            else
            {
                // Updating existing entity - check for conflicts with OTHER entities
                var conflictQuery = CombineWithIdExclusion(matchingNamesQuery, entityId);
                var conflictResult = await repository.GetEntitiesAsync(conflictQuery);
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

        /// <summary>
        /// Builds an expression that checks if the name matches (case-insensitive).
        /// </summary>
        private static Expression<Func<TDao, bool>> BuildNameMatchQuery<TDao>(
            Expression<Func<TDao, string>> nameSelector,
            string normalizedName,
            Expression<Func<TDao, bool>> additionalFilter)
            where TDao : Entity
        {
            // Build: entity => entity.Name.ToLower() == normalizedName
            var parameter = nameSelector.Parameters[0];
            var nameAccess = nameSelector.Body;
            var toLowerCall = Expression.Call(nameAccess, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var comparison = Expression.Equal(toLowerCall, Expression.Constant(normalizedName));

            Expression finalBody = comparison;

            // Add additional filter if provided (e.g., IsActivated)
            if (additionalFilter != null)
            {
                var additionalBody = ReplaceParameter(additionalFilter.Body, additionalFilter.Parameters[0], parameter);
                finalBody = Expression.AndAlso(comparison, additionalBody);
            }

            return Expression.Lambda<Func<TDao, bool>>(finalBody, parameter);
        }

        /// <summary>
        /// Combines the name match query with an ID exclusion for update scenarios.
        /// </summary>
        private static Expression<Func<TDao, bool>> CombineWithIdExclusion<TDao>(
            Expression<Func<TDao, bool>> baseQuery,
            int entityIdToExclude)
            where TDao : Entity
        {
            // Build: entity => baseQuery(entity) && entity.Id != entityIdToExclude
            var parameter = baseQuery.Parameters[0];
            var idProperty = Expression.Property(parameter, nameof(Entity.Id));
            var idComparison = Expression.NotEqual(idProperty, Expression.Constant(entityIdToExclude));
            var combinedBody = Expression.AndAlso(baseQuery.Body, idComparison);

            return Expression.Lambda<Func<TDao, bool>>(combinedBody, parameter);
        }

        /// <summary>
        /// Replaces parameter references in an expression.
        /// </summary>
        private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParam, ParameterExpression newParam)
        {
            return new ParameterReplacer(oldParam, newParam).Visit(expression);
        }

        private class ParameterReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam;
            private readonly ParameterExpression _newParam;

            public ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam)
            {
                _oldParam = oldParam;
                _newParam = newParam;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParam ? _newParam : base.VisitParameter(node);
            }
        }
    }
}
