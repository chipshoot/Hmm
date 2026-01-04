using System;
using Hmm.Utility.Validation;

namespace Hmm.Utility.Specification
{
    internal class NotSpecification<TEntity> : ISpecification<TEntity>
    {
        protected ISpecification<TEntity> Wrapped { get; }

        internal NotSpecification(ISpecification<TEntity> spec)
        {
            ArgumentNullException.ThrowIfNull(spec);

            Wrapped = spec;
        }

        public bool IsSatisfiedBy(TEntity candidate)
        {
            return !Wrapped.IsSatisfiedBy(candidate);
        }
    }
}