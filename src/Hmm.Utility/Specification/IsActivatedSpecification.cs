using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Utility.Specification
{
    /// <summary>
    /// Specification that filters entities by their IsActivated flag.
    /// Only entities where IsActivated is true will satisfy this specification.
    /// </summary>
    /// <typeparam name="T">An entity type that implements <see cref="IActivatable"/>.</typeparam>
    public class IsActivatedSpecification<T> : Specification<T> where T : IActivatable
    {
        public IsActivatedSpecification()
            : base(entity => entity.IsActivated)
        {
        }
    }
}
