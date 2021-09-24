using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System.Collections.Generic;

namespace Hmm.Automobile
{
    public interface IAutoEntityManager<T> where T : AutomobileBase
    {
        INoteSerializer<T> NoteSerializer { get; }

        IHmmValidator<T> Validator { get; }

        Author DefaultAuthor { get; }

        T GetEntityById(int id);

        IEnumerable<T> GetEntities();

        T Create(T entity);

        T Update(T entity);

        bool IsEntityOwner(int id);

        ProcessingResult ProcessResult { get; }
    }
}