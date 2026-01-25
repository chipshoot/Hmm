using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Hmm.Core.Map;

/// <summary>
/// Represents a class that maps expressions from the source type to the target type.
/// Supports property name mapping configuration and handles navigation properties.
/// </summary>
/// <remarks>
/// This mapper translates LINQ expressions between domain and DAO entity types,
/// enabling queries written against domain models to be executed against database entities.
/// </remarks>
public class ExpressionMapper<TSource, TTarget> : ExpressionVisitor
{
    private readonly ParameterExpression _parameter;
    private readonly IReadOnlyDictionary<string, string> _propertyMappings;

    // Cache for member lookups to avoid repeated reflection
    private static readonly ConcurrentDictionary<string, MemberInfo?> MemberCache = new();

    /// <summary>
    /// Initializes a new instance of the ExpressionMapper with a target parameter.
    /// </summary>
    /// <param name="parameter">The parameter expression for the target type.</param>
    /// <param name="propertyMappings">Optional dictionary mapping source property names to target property names.</param>
    private ExpressionMapper(ParameterExpression parameter, IReadOnlyDictionary<string, string>? propertyMappings = null)
    {
        _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        _propertyMappings = propertyMappings ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Maps an expression from the source type to the target type.
    /// </summary>
    /// <param name="expression">The source expression to map.</param>
    /// <returns>The mapped expression for the target type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when expression is null.</exception>
    public static Expression<Func<TTarget, bool>> MapExpression(Expression<Func<TSource, bool>> expression)
    {
        return MapExpression(expression, null);
    }

    /// <summary>
    /// Maps an expression from the source type to the target type with custom property mappings.
    /// </summary>
    /// <param name="expression">The source expression to map.</param>
    /// <param name="propertyMappings">Dictionary mapping source property names to target property names.</param>
    /// <returns>The mapped expression for the target type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when expression is null.</exception>
    public static Expression<Func<TTarget, bool>> MapExpression(
        Expression<Func<TSource, bool>> expression,
        IReadOnlyDictionary<string, string>? propertyMappings)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var parameter = Expression.Parameter(typeof(TTarget), "target");
        var mapper = new ExpressionMapper<TSource, TTarget>(parameter, propertyMappings);
        var body = mapper.Visit(expression.Body);
        return Expression.Lambda<Func<TTarget, bool>>(body, parameter);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        // Replace source parameter with target parameter
        if (node.Type == typeof(TSource))
        {
            return _parameter;
        }
        return base.VisitParameter(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // Handle nested member access (navigation properties)
        if (node.Expression is MemberExpression parentMember)
        {
            var visitedParent = Visit(parentMember);
            if (visitedParent != null)
            {
                return HandleMemberAccess(node, visitedParent);
            }
        }

        // Handle direct member access on the source type
        if (node.Member.DeclaringType == typeof(TSource) ||
            (node.Member.DeclaringType != null && node.Member.DeclaringType.IsAssignableFrom(typeof(TSource))))
        {
            var visitedExpression = Visit(node.Expression);
            if (visitedExpression != null)
            {
                return HandleMemberAccess(node, visitedExpression);
            }
        }

        // Handle closure variables and constants (e.g., captured variables in lambdas)
        if (node.Expression is ConstantExpression)
        {
            return base.VisitMember(node);
        }

        return base.VisitMember(node);
    }

    private Expression HandleMemberAccess(MemberExpression node, Expression visitedExpression)
    {
        var sourceMemberName = node.Member.Name;

        // Check for custom property mapping
        var targetMemberName = _propertyMappings.TryGetValue(sourceMemberName, out var mappedName)
            ? mappedName
            : sourceMemberName;

        // Get the target type from the visited expression
        var targetType = visitedExpression.Type;

        // Try to find the member on the target type
        var targetMember = GetCachedMember(targetType, targetMemberName);

        if (targetMember == null)
        {
            // Try case-insensitive match as fallback
            targetMember = GetCachedMember(targetType, targetMemberName, ignoreCase: true);
        }

        if (targetMember == null)
        {
            throw new InvalidOperationException(
                $"Property '{sourceMemberName}' from type '{typeof(TSource).Name}' " +
                $"could not be mapped to type '{targetType.Name}'. " +
                $"No property named '{targetMemberName}' was found on the target type. " +
                $"Consider adding a custom property mapping.");
        }

        return Expression.MakeMemberAccess(visitedExpression, targetMember);
    }

    private static MemberInfo? GetCachedMember(Type type, string memberName, bool ignoreCase = false)
    {
        var cacheKey = $"{type.FullName}.{memberName}.{ignoreCase}";

        return MemberCache.GetOrAdd(cacheKey, _ =>
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            if (ignoreCase)
            {
                bindingFlags |= BindingFlags.IgnoreCase;
            }

            // Try property first
            var property = type.GetProperty(memberName, bindingFlags);
            if (property != null)
            {
                return property;
            }

            // Fall back to field
            var field = type.GetField(memberName, bindingFlags);
            return field;
        });
    }
}