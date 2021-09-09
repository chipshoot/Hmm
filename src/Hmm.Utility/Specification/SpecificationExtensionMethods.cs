namespace Hmm.Utility.Specification
{
    /// <summary>
    /// The help classes for using AndSpecification, OrSpecification and NotSpecification
    /// <remarks>
    /// example of specification pattern logic chaining
    ///     OverDueSpecification OverDue = new OverDueSpecification();
    ///     var NoticeSent = new NoticeSentSpecification();
    ///     var InCollection = new InCollectionSpecification();
    ///     ISpecification{Invoice} SendToCollection = OverDue.And(NoticeSent).And(InCollection.Not());
    ///     InvoiceCollection = Service.GetInvoices();
    ///     foreach (Invoice currentInvoice in InvoiceCollection)
    ///     {
    ///         if (SendToCollection.IsSatisfiedBy(currentInvoice))
    ///         {
    ///             currentInvoice.SendToCollection();
    ///         }
    ///     }
    /// </remarks>
    /// </summary>
    public static class SpecificationExtensionMethods
    {
        public static ISpecification<TEntity> And<TEntity>(this ISpecification<TEntity> spec1,
            ISpecification<TEntity> spec2)
        {
            return new AndSpecification<TEntity>(spec1, spec2);
        }

        public static ISpecification<TEntity> Or<TEntity>(this ISpecification<TEntity> spec1,
            ISpecification<TEntity> spec2)
        {
            return new OrSpecification<TEntity>(spec1, spec2);
        }

        public static ISpecification<TEntity> Not<TEntity>(this ISpecification<TEntity> spec)
        {
            return new NotSpecification<TEntity>(spec);
        }
    }
}