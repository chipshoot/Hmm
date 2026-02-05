using System;
using System.Linq.Expressions;
using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Utility.Specification
{
    /// <summary>
    /// Specification that matches entities by a case-insensitive name comparison.
    /// Optionally composes with an additional filter (e.g., IsActivated).
    /// </summary>
    /// <typeparam name="TDao">The DAO entity type with an integer identity.</typeparam>
    public class NameMatchSpecification<TDao> : Specification<TDao> where TDao : Entity
    {
        /// <summary>
        /// Creates a specification that matches entities by name (case-insensitive).
        /// </summary>
        /// <param name="nameSelector">Expression selecting the name property from the entity.</param>
        /// <param name="normalizedName">The name to match (should already be trimmed and lowered).</param>
        /// <param name="additionalFilter">Optional additional filter to compose with AND.</param>
        public NameMatchSpecification(
            Expression<Func<TDao, string>> nameSelector,
            string normalizedName,
            Expression<Func<TDao, bool>> additionalFilter = null)
            : base(BuildExpression(nameSelector, normalizedName, additionalFilter))
        {
        }

        private static Expression<Func<TDao, bool>> BuildExpression(
            Expression<Func<TDao, string>> nameSelector,
            string normalizedName,
            Expression<Func<TDao, bool>> additionalFilter)
        {
            var parameter = nameSelector.Parameters[0];
            var nameAccess = nameSelector.Body;
            var toLowerCall = Expression.Call(nameAccess, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var comparison = Expression.Equal(toLowerCall, Expression.Constant(normalizedName));

            Expression finalBody = comparison;

            if (additionalFilter != null)
            {
                var replacedBody = new ParameterReplacer(additionalFilter.Parameters[0], parameter)
                    .Visit(additionalFilter.Body);
                finalBody = Expression.AndAlso(comparison, replacedBody);
            }

            return Expression.Lambda<Func<TDao, bool>>(finalBody, parameter);
        }
    }
}
