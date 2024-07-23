// Ignore Spelling: Dao

using System.Text.Json;
using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Misc;

namespace Hmm.Core.Map;

public class ContactMapper : IEntityMapper<Contact, ContactDao>
{
    private readonly IMapper _mapper;

    public ContactMapper(IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));
        _mapper = mapper;
    }

    public Contact MapToDomainEntity(ContactDao daoEntity)
    {
        ArgumentNullException.ThrowIfNull(daoEntity);
        var contact = _mapper.Map<Contact>(daoEntity);
        return contact;
    }

    public ContactDao MapToDaoEntity(Contact domainEntity)
    {
        throw new NotImplementedException();
    }
}
