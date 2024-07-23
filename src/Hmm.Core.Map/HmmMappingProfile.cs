using System.Text.Json;
using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;

namespace Hmm.Core.Map;

public class HmmMappingProfile: Profile
{
    public HmmMappingProfile()
    {
        CreateMap<ContactDao, Contact>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => ContactDaoConvert(src.Contact).FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => ContactDaoConvert(src.Contact).LastName))
            .ForMember(dest => dest.Emails, opt => opt.MapFrom(src => ContactDaoConvert(src.Contact).Emails))
            .ForMember(dest => dest.Phones, opt => opt.MapFrom(src => ContactDaoConvert(src.Contact).Phones))
            .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => ContactDaoConvert(src.Contact).Addresses));

        CreateMap<Contact, ContactDao>()
            .ForMember(dest => dest.Contact, opt => opt.MapFrom(src => ContactConvert(src)));

        CreateMap<AuthorDao, Author>();
        CreateMap<Author, AuthorDao>();

        CreateMap<NoteCatalogDao, NoteCatalog>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.FormatType));
        CreateMap<NoteCatalog, NoteCatalogDao>()
            .ForMember(dest => dest.FormatType, opt => opt.MapFrom(src => src.Type));

        CreateMap<TagDao, Tag>()
            .ForMember(dest=>dest.Notes, opt=>opt.Ignore());
        CreateMap<Tag, TagDao>()
            .ForMember(dest => dest.Notes, opt => opt.Ignore());

    }
    
    private static ContactInfo? ContactDaoConvert(string contactString)
    {
        try
        {
            var contact = JsonSerializer.Deserialize<ContactInfo>(contactString);
            return contact;
        }
        catch (JsonException ex)
        {
            return null;
        }
    }

    private static string ContactConvert(Contact contact)
    {
        var info = new ContactInfo
        {
            FirstName = contact.FirstName,
            LastName = contact.LastName,
            Emails = contact.Emails.ToList(),
            Phones = contact.Phones.ToList(),
            Addresses = contact.Addresses.ToList()
        };

        var result = JsonSerializer.Serialize<ContactInfo>(info);
        return result;
    }

    internal class ContactInfo
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public List<Email> Emails { get; set; }

        public List<Phone> Phones { get; set; }

        public List<AddressInfo> Addresses { get; set; }
    }
}