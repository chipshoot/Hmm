using System;
using System.Linq.Expressions;

namespace Hmm.Utility.Specification
{
    /// <summary>
    /// The composite interface regarding the specification design pattern.
    /// Supports both in-memory evaluation via IsSatisfiedBy and EF Core/LINQ translation via ToExpression.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface ISpecification<TEntity>
    {
        /// <summary>
        /// Evaluates the specification against a candidate entity in-memory.
        /// </summary>
        /// <param name="candidate">The entity to evaluate.</param>
        /// <returns>True if the entity satisfies the specification; otherwise, false.</returns>
        bool IsSatisfiedBy(TEntity candidate);

        /// <summary>
        /// Gets the expression tree representation of this specification.
        /// This can be used with EF Core and other LINQ providers for SQL translation.
        /// </summary>
        /// <returns>An expression that can be used in LINQ queries.</returns>
        Expression<Func<TEntity, bool>> ToExpression();
    }
}
