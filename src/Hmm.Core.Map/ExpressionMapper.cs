using System.Linq.Expressions;

namespace Hmm.Core.Map;

/// <summary>
/// Represents a class that maps expressions from the source type to the target type.
/// </summary>
public class ExpressionMapper<TSource, TTarget>(ParameterExpression parameter) : ExpressionVisitor
{
    public static Expression<Func<TTarget, bool>> MapExpression(Expression<Func<TSource, bool>> expression)
    {
        var parameter = Expression.Parameter(typeof(TTarget), "tt");
        var body = new ExpressionMapper<TSource, TTarget>(parameter).Visit(expression.Body);
        return Expression.Lambda<Func<TTarget, bool>>(body, parameter);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return parameter;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member.DeclaringType == typeof(TSource))
        {
            var newMember = typeof(TTarget).GetMember(node.Member.Name).FirstOrDefault();
            return Expression.MakeMemberAccess(Visit(node.Expression), newMember);
        }

        return base.VisitMember(node);
    }
}