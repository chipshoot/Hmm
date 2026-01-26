using System;
using System.Linq.Expressions;

namespace Hmm.Utility.Specification
{
    /// <summary>
    /// Combines two specifications with a logical OR operation.
    /// Either specification being satisfied will satisfy the combined specification.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity this specification applies to.</typeparam>
    internal class OrSpecification<TEntity> : ISpecification<TEntity>
    {
        private readonly ISpecification<TEntity> _spec1;
        private readonly ISpecification<TEntity> _spec2;
        private readonly Lazy<Expression<Func<TEntity, bool>>> _combinedExpression;
        private readonly Lazy<Func<TEntity, bool>> _compiledExpression;

        internal OrSpecification(ISpecification<TEntity> spec1, ISpecification<TEntity> spec2)
        {
            ArgumentNullException.ThrowIfNull(spec1);
            ArgumentNullException.ThrowIfNull(spec2);

            _spec1 = spec1;
            _spec2 = spec2;
            _combinedExpression = new Lazy<Expression<Func<TEntity, bool>>>(CreateCombinedExpression);
            _compiledExpression = new Lazy<Func<TEntity, bool>>(() => _combinedExpression.Value.Compile());
        }

        public bool IsSatisfiedBy(TEntity candidate)
        {
            return _compiledExpression.Value(candidate);
        }

        public Expression<Func<TEntity, bool>> ToExpression()
        {
            return _combinedExpression.Value;
        }

        private Expression<Func<TEntity, bool>> CreateCombinedExpression()
        {
            var expr1 = _spec1.ToExpression();
            var expr2 = _spec2.ToExpression();

            // Use the parameter from the first expression
            var parameter = expr1.Parameters[0];

            // Replace the parameter in the second expression with the first one
            var body2 = new ParameterReplacer(expr2.Parameters[0], parameter).Visit(expr2.Body);

            // Combine with OrElse (||)
            var combinedBody = Expression.OrElse(expr1.Body, body2);

            return Expression.Lambda<Func<TEntity, bool>>(combinedBody, parameter);
        }
    }
}
