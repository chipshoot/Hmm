using System;
using Hmm.Utility.Validation;

namespace Hmm.Utility.Specification
{
    internal class AndSpecification<TEntity> : ISpecification<TEntity>
    {
        internal AndSpecification(ISpecification<TEntity> spec1, ISpecification<TEntity> spec2)
        {
            Guard.Against<ArgumentNullException>(spec1 == null, "spec1");
            Guard.Against<ArgumentNullException>(spec2 == null, "spec2");

            Spec1 = spec1;
            Spec2 = spec2;
        }

        protected ISpecification<TEntity> Spec1 { get; }

        protected ISpecification<TEntity> Spec2 { get; }

        public bool IsSatisfiedBy(TEntity candidate)
        {
            return Spec1.IsSatisfiedBy(candidate) && Spec2.IsSatisfiedBy(candidate);
        }
    }
}