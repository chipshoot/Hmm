using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.DomainEntity;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public interface IAutoEntityManager<T> where T : AutomobileBase
    {
        INoteSerializer<T> NoteSerializer { get; }

        IHmmValidator<T> Validator { get; }

        Author DefaultAuthor { get; }

        T GetEntityById(int id);

        Task<T> GetEntityByIdAsync(int id);

        IEnumerable<T> GetEntities();

        Task<IEnumerable<T>> GetEntitiesAsync();

        T Create(T entity);

        Task<T> CreateAsync(T entity);

        T Update(T entity);

        Task<T> UpdateAsync(T entity);

        bool IsEntityOwner(int id);

        ProcessingResult ProcessResult { get; }
    }
}