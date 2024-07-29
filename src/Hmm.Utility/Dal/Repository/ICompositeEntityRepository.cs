using Hmm.Utility.Dal.DataEntity;
using Hmm.Utility.Dal.Query;
using System.Threading.Tasks;

namespace Hmm.Utility.Dal.Repository;

public interface ICompositeEntityRepository<T, TChild> : IRepository<T> where T : Entity where TChild : Entity
{
    Task<PageList<TChild>> GetNoteByTagAsync(int tagId, ResourceCollectionParameters resourceCollectionParameters = null);
}