using System.Linq;

namespace Hmm.Utility.Specification
{
    /// <summary>
    /// Extension methods for working with specifications.
    /// Provides fluent API for combining specifications with And, Or, and Not operations.
    /// </summary>
    /// <remarks>
    /// Example of specification pattern logic chaining:
    /// <code>
    /// var overDue = new Specification&lt;Invoice&gt;(i => i.DueDate &lt; DateTime.Now);
    /// var noticeSent = new Specification&lt;Invoice&gt;(i => i.NoticeSent);
    /// var inCollection = new Specification&lt;Invoice&gt;(i => i.InCollection);
    ///
    /// // Combine specifications
    /// ISpecification&lt;Invoice&gt; sendToCollection = overDue.And(noticeSent).And(inCollection.Not());
    ///
    /// // Use with EF Core (translates to SQL)
    /// var invoices = dbContext.Invoices.Where(sendToCollection.ToExpression()).ToList();
    ///
    /// // Or use in-memory
    /// foreach (var invoice in invoiceCollection)
    /// {
    ///     if (sendToCollection.IsSatisfiedBy(invoice))
    ///     {
    ///         invoice.SendToCollection();
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public static class SpecificationExtensionMethods
    {
        /// <summary>
        /// Combines two specifications with a logical AND operation.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="spec1">The first specification.</param>
        /// <param name="spec2">The second specification.</param>
        /// <returns>A new specification that is satisfied only when both specifications are satisfied.</returns>
        public static ISpecification<TEntity> And<TEntity>(this ISpecification<TEntity> spec1,
            ISpecification<TEntity> spec2)
        {
            return new AndSpecification<TEntity>(spec1, spec2);
        }

        /// <summary>
        /// Combines two specifications with a logical OR operation.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="spec1">The first specification.</param>
        /// <param name="spec2">The second specification.</param>
        /// <returns>A new specification that is satisfied when either specification is satisfied.</returns>
        public static ISpecification<TEntity> Or<TEntity>(this ISpecification<TEntity> spec1,
            ISpecification<TEntity> spec2)
        {
            return new OrSpecification<TEntity>(spec1, spec2);
        }

        /// <summary>
        /// Negates a specification with a logical NOT operation.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="spec">The specification to negate.</param>
        /// <returns>A new specification that is satisfied only when the original specification is NOT satisfied.</returns>
        public static ISpecification<TEntity> Not<TEntity>(this ISpecification<TEntity> spec)
        {
            return new NotSpecification<TEntity>(spec);
        }

        /// <summary>
        /// Filters an IQueryable using a specification.
        /// The specification's expression is translated to SQL by the underlying provider.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity.</typeparam>
        /// <param name="query">The queryable to filter.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>A filtered queryable.</returns>
        public static IQueryable<TEntity> Satisfying<TEntity>(this IQueryable<TEntity> query,
            ISpecification<TEntity> specification)
        {
            return query.Where(specification.ToExpression());
        }
    }
}
