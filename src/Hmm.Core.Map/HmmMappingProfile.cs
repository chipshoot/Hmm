using System.Text.Json;
using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;

namespace Hmm.Core.Map;

public class HmmMappingProfile : Profile
{
    public HmmMappingProfile()
    {
        CreateMap<ContactDao, Contact>()
            .ConvertUsing((src, dest) =>
            {
                var contactInfo = ContactDaoConvert(src.Contact);
                dest = contactInfo == null
                    ? new Contact
                    {
                        Id = src.Id,
                        FirstName = string.Empty,
                        LastName = string.Empty,
                        Emails = new List<Email>(),
                        Phones = new List<Phone>(),
                        Addresses = new List<AddressInfo>(),
                        Description = src.Description
                    }
                    : new Contact
                    {
                        Id = src.Id,
                        FirstName = contactInfo.FirstName,
                        LastName = contactInfo.LastName,
                        Emails = contactInfo.Emails,
                        Phones = contactInfo.Phones,
                        Addresses = contactInfo.Addresses,
                        Description = src.Description
                    };

                return dest;
            });
        CreateMap<Contact, ContactDao>()
            .ForMember(dest => dest.Contact, opt => opt.MapFrom(src => ContactConvert(src)));

        CreateMap<AuthorDao, Author>();
        CreateMap<Author, AuthorDao>();

        CreateMap<NoteCatalogDao, NoteCatalog>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.FormatType));
        CreateMap<NoteCatalog, NoteCatalogDao>()
            .ForMember(dest => dest.FormatType, opt => opt.MapFrom(src => src.Type));

        CreateMap<TagDao, Tag>()
            .ForMember(dest => dest.Notes, opt => opt.Ignore());
        CreateMap<Tag, TagDao>()
            .ForMember(dest => dest.Notes, opt => opt.Ignore());

        CreateMap<HmmNoteDao, HmmNote>();
        CreateMap<HmmNote, HmmNoteDao>()
            .ForMember(dest => dest.Tags, opt => opt.Ignore());
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