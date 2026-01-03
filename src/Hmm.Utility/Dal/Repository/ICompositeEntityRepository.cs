using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Utility.Dal.Repository;

public interface ICompositeEntityRepository<T, TChild> : IRepository<T> where T : Entity where TChild : Entity
{
    Task<ProcessingResult<PageList<TChild>>> GetNoteByTagAsync(int tagId, ResourceCollectionParameters resourceCollectionParameters = null);
}