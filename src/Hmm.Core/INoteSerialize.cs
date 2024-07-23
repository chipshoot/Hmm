using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;

namespace Hmm.Core
{
    public interface INoteSerialize<T>
    {
        T GetEntity(HmmNote note);

        HmmNote GetNote(in T entity);

        ProcessingResult ProcessResult { get; }
    }
}