using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;

namespace Hmm.Core
{
    public interface INoteSerialize<T>
    {
        ProcessingResult<T> GetEntity(HmmNote note);

        ProcessingResult<HmmNote> GetNote(in T entity);
    }
}