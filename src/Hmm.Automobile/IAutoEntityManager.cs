using Hmm.Utility.Misc;
using System;
using System.Collections.Generic;
using Hmm.Core;
using Hmm.Automobile.DomainEntity;
using Hmm.Core.DomainEntity;

namespace Hmm.Automobile
{
    public interface IAutoEntityManager<T> where T : AutomobileBase
    {
        INoteSerializer<T> NoteSerializer { get; }

        Author DefaultAuthor { get; }

        T GetEntityById(int id);

        IEnumerable<T> GetEntities();

        T Create(T entity);

        T Update(T entity);

        bool IsEntityOwner(int id);

        ProcessingResult ProcessResult { get; }
    }
}