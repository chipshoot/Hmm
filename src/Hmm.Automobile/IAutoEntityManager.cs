using Hmm.Automobile.DomainEntity;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using System.Threading.Tasks;

namespace Hmm.Automobile
{
    public interface IAutoEntityManager<T> where T : AutomobileBase
    {
        INoteSerializer<T> NoteSerializer { get; }

        IHmmValidator<T> Validator { get; }

        Author DefaultAuthor { get; }

        Task<ProcessingResult<T>> GetEntityByIdAsync(int id);

        Task<ProcessingResult<PageList<T>>> GetEntitiesAsync(ResourceCollectionParameters resourceCollectionParameter = null);

        Task<ProcessingResult<T>> CreateAsync(T entity);

        Task<ProcessingResult<T>> UpdateAsync(T entity);

        Task<bool> IsEntityOwnerAsync(int id);
    }
}
