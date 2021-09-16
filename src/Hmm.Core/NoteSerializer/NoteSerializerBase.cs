using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging;
using System;

namespace Hmm.Core.NoteSerializer
{
    public abstract class NoteSerializerBase<T> : INoteSerializer<T>
    {
        protected NoteSerializerBase(ILogger logger)
        {
            Guard.Against<ArgumentNullException>(logger == null, nameof(logger));
            ProcessResult = new ProcessingResult(logger);
        }

        public abstract T GetEntity(HmmNote note);

        public abstract HmmNote GetNote(in T entity);

        public ProcessingResult ProcessResult { get; }
    }
}