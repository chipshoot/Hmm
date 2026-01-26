using System.Linq.Expressions;

namespace Hmm.Utility.Specification
{
    /// <summary>
    /// Expression visitor that replaces one parameter with another.
    /// Used when combining specification expressions to ensure they use the same parameter.
    /// </summary>
    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        /// <summary>
        /// Creates a new parameter replacer.
        /// </summary>
        /// <param name="oldParameter">The parameter to replace.</param>
        /// <param name="newParameter">The parameter to use instead.</param>
        public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        /// <summary>
        /// Visits a parameter expression and replaces it if it matches the old parameter.
        /// </summary>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}
