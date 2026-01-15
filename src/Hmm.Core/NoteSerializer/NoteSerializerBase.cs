using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging;
using System;

namespace Hmm.Core.NoteSerializer
{
    public abstract class NoteSerializerBase<T> : INoteSerialize<T>
    {
        protected ILogger Logger { get; } = null;

        public NoteSerializerBase(ILogger logger)
        {
            Logger = logger;
        }

        public abstract ProcessingResult<T> GetEntity(HmmNote note);

        public abstract ProcessingResult<HmmNote> GetNote(in T entity);
    }
}