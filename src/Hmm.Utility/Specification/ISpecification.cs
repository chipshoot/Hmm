namespace Hmm.Utility.Specification
{
    /// <summary>
    /// The composite interface regarding the specification design pattern
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public interface ISpecification<in TEntity>
    {
        bool IsSatisfiedBy(TEntity candidate);
    }
}