using Hmm.Utility.Dal.DataEntity;

namespace Hmm.Utility.Dal.Repository
{
    public interface IVersionRepository<T> : IGenericRepository<T, int> where T : VersionedEntity
    {
    }
}