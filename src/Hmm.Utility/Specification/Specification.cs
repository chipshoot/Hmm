using System;
using System.Linq.Expressions;

namespace Hmm.Utility.Specification
{
    /// <summary>
    /// Base implementation of the specification pattern using expression trees.
    /// Supports both in-memory evaluation and EF Core/LINQ translation.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity this specification applies to.</typeparam>
    public class Specification<TEntity> : ISpecification<TEntity>
    {
        private readonly Expression<Func<TEntity, bool>> _expression;
        private readonly Lazy<Func<TEntity, bool>> _compiledExpression;

        /// <summary>
        /// Creates a new specification from an expression tree.
        /// </summary>
        /// <param name="expression">The expression that defines this specification.</param>
        /// <exception cref="ArgumentNullException">Thrown when expression is null.</exception>
        public Specification(Expression<Func<TEntity, bool>> expression)
        {
            ArgumentNullException.ThrowIfNull(expression);
            _expression = expression;
            _compiledExpression = new Lazy<Func<TEntity, bool>>(() => _expression.Compile());
        }

        /// <inheritdoc />
        public bool IsSatisfiedBy(TEntity candidate)
        {
            return _compiledExpression.Value(candidate);
        }

        /// <inheritdoc />
        public Expression<Func<TEntity, bool>> ToExpression()
        {
            return _expression;
        }

        /// <summary>
        /// Implicit conversion from Expression to Specification for convenience.
        /// </summary>
        public static implicit operator Specification<TEntity>(Expression<Func<TEntity, bool>> expression)
        {
            return new Specification<TEntity>(expression);
        }

        /// <summary>
        /// Implicit conversion from Specification to Expression for LINQ integration.
        /// </summary>
        public static implicit operator Expression<Func<TEntity, bool>>(Specification<TEntity> specification)
        {
            return specification.ToExpression();
        }
    }
}
