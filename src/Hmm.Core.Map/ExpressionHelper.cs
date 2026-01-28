using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Hmm.Core.Map;

/// <summary>
/// Provides helper methods for expression manipulation with caching support.
/// Used to combine query expressions with common filters like IsActivated.
/// </summary>
/// <remarks>
/// This class addresses performance issue #36: runtime expression building without caching.
/// By caching the IsActivated expressions and parameter creation, we avoid repeated
/// Expression.Parameter/AndAlso/Invoke calls on every query.
/// </remarks>
public static class ExpressionHelper
{
    /// <summary>
    /// Cache for IsActivated expressions per type to avoid rebuilding on every query.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, LambdaExpression> IsActivatedExpressionCache = new();

    /// <summary>
    /// Cache for parameter expressions per type.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, ParameterExpression> ParameterCache = new();

    /// <summary>
    /// Gets a cached parameter expression for the specified type.
    /// </summary>
    /// <typeparam name="T">The type for which to get the parameter.</typeparam>
    /// <param name="name">Optional parameter name.</param>
    /// <returns>A cached parameter expression.</returns>
    public static ParameterExpression GetCachedParameter<T>(string name = "x")
    {
        return ParameterCache.GetOrAdd(typeof(T), _ => Expression.Parameter(typeof(T), name));
    }

    /// <summary>
    /// Gets a cached IsActivated expression for entities that have an IsActivated property.
    /// </summary>
    /// <typeparam name="T">The entity type with an IsActivated property.</typeparam>
    /// <returns>An expression representing t => t.IsActivated.</returns>
    public static Expression<Func<T, bool>> GetIsActivatedExpression<T>()
    {
        var cachedExpression = IsActivatedExpressionCache.GetOrAdd(typeof(T), type =>
        {
            var parameter = Expression.Parameter(type, "t");
            var property = type.GetProperty("IsActivated");

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Type '{type.Name}' does not have an 'IsActivated' property.");
            }

            var body = Expression.Property(parameter, property);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        });

        return (Expression<Func<T, bool>>)cachedExpression;
    }

    /// <summary>
    /// Combines a mapped query expression with the IsActivated filter.
    /// This method caches intermediate expressions for better performance.
    /// </summary>
    /// <typeparam name="TSource">The source domain entity type.</typeparam>
    /// <typeparam name="TDest">The destination DAO entity type.</typeparam>
    /// <param name="query">The optional query expression in source entity terms.</param>
    /// <returns>A combined expression with IsActivated filter, or just IsActivated if query is null.</returns>
    public static Expression<Func<TDest, bool>> CombineWithIsActivated<TSource, TDest>(
        Expression<Func<TSource, bool>>? query)
    {
        var isActivatedExpr = GetIsActivatedExpression<TDest>();

        if (query == null)
        {
            return isActivatedExpr;
        }

        // Map the source query to destination type
        var mappedQuery = ExpressionMapper<TSource, TDest>.MapExpression(query);

        // Combine the mapped query with IsActivated using AndAlso
        return CombineExpressions(mappedQuery, isActivatedExpr);
    }

    /// <summary>
    /// Combines two boolean expressions using AndAlso (logical AND).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="first">The first expression.</param>
    /// <param name="second">The second expression.</param>
    /// <returns>A combined expression representing (first AND second).</returns>
    public static Expression<Func<T, bool>> CombineExpressions<T>(
        Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        // Use a parameter replacer to unify the parameter references
        var parameter = Expression.Parameter(typeof(T), "x");

        var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var secondBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);

        var combinedBody = Expression.AndAlso(firstBody, secondBody);

        return Expression.Lambda<Func<T, bool>>(combinedBody, parameter);
    }

    /// <summary>
    /// Replaces a parameter in an expression with a new parameter.
    /// </summary>
    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParam, ParameterExpression newParam)
    {
        return new ParameterReplacer(oldParam, newParam).Visit(expression);
    }

    /// <summary>
    /// Expression visitor that replaces parameter references.
    /// </summary>
    private sealed class ParameterReplacer : ExpressionVisitor
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
