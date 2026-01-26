using System;
using System.Linq.Expressions;

namespace Hmm.Utility.Specification
{
    /// <summary>
    /// Negates a specification with a logical NOT operation.
    /// The wrapped specification must NOT be satisfied for this specification to be satisfied.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity this specification applies to.</typeparam>
    internal class NotSpecification<TEntity> : ISpecification<TEntity>
    {
        private readonly ISpecification<TEntity> _wrapped;
        private readonly Lazy<Expression<Func<TEntity, bool>>> _negatedExpression;
        private readonly Lazy<Func<TEntity, bool>> _compiledExpression;

        internal NotSpecification(ISpecification<TEntity> spec)
        {
            ArgumentNullException.ThrowIfNull(spec);

            _wrapped = spec;
            _negatedExpression = new Lazy<Expression<Func<TEntity, bool>>>(CreateNegatedExpression);
            _compiledExpression = new Lazy<Func<TEntity, bool>>(() => _negatedExpression.Value.Compile());
        }

        public bool IsSatisfiedBy(TEntity candidate)
        {
            return _compiledExpression.Value(candidate);
        }

        public Expression<Func<TEntity, bool>> ToExpression()
        {
            return _negatedExpression.Value;
        }

        private Expression<Func<TEntity, bool>> CreateNegatedExpression()
        {
            var expr = _wrapped.ToExpression();
            var parameter = expr.Parameters[0];

            // Negate with Not (!)
            var negatedBody = Expression.Not(expr.Body);

            return Expression.Lambda<Func<TEntity, bool>>(negatedBody, parameter);
        }
    }
}
