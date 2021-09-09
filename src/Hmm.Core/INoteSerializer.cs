using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;

namespace Hmm.Core
{
    public interface INoteSerializer<T>
    {
        T GetEntity(HmmNote note);

        HmmNote GetNote(in T entity);

        ProcessingResult ProcessResult { get; }
    }
}