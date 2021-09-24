using Hmm.Utility.Misc;

namespace Hmm.Utility.Validation
{
    public interface IHmmValidator<in T>
    {
        bool IsValidEntity(T entity, ProcessingResult processResult);
    }
}