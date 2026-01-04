using System;
using Hmm.Utility.Validation;

namespace Hmm.Utility.Specification
{
    public class Specification<TEntity> : ISpecification<TEntity>
    {
        private readonly Func<TEntity, bool> _spec;

        public Specification(Func<TEntity, bool> spec)
        {
            ArgumentNullException.ThrowIfNull(spec);
            _spec = spec;
        }

        public bool IsSatisfiedBy(TEntity candidate)
        {
            return _spec(candidate);
        }
    }
}