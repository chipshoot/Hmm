using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;

namespace Hmm.Core.NoteSerializer
{
    public abstract class NoteSerializerBase<T> : INoteSerializer<T>
    {
        protected ILogger Logger { get; } = null;

        protected NoteSerializerBase(ILogger logger)
        {
            Logger = logger;
        }

        public abstract Task<ProcessingResult<T>> GetEntity(HmmNote note);

        public abstract Task<ProcessingResult<HmmNote>> GetNote(in T entity);
    }
}