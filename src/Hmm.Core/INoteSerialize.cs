using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;

namespace Hmm.Core
{
    public interface INoteSerialize<T>
    {
        Task<ProcessingResult<T>> GetEntity(HmmNote note);

        Task<ProcessingResult<HmmNote>> GetNote(in T entity);
    }
}