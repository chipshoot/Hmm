// Ignore Spelling: Dao

using Hmm.Core.Map.DomainEntity;

namespace Hmm.Core.Map;

public interface IEntityMapper<TDomainEntity, TDaoEntity> where TDomainEntity : EntityBase where TDaoEntity : class
{
    TDomainEntity MapToDomainEntity(TDaoEntity daoEntity);

    TDaoEntity MapToDaoEntity(TDomainEntity domainEntity);
}