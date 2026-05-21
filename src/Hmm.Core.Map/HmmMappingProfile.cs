using System.Text.Json;
using AutoMapper;
using Hmm.Core.Map.DbEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.Vault;
using Hmm.Utility.Dal.Query;

namespace Hmm.Core.Map;

public class HmmMappingProfile : Profile
{
    public HmmMappingProfile()
    {
        // PageList mappings - required because PageList implements IReadOnlyList<T> not List<T>
        CreateMap<PageList<AuthorDao>, PageList<Author>>()
            .ConvertUsing(new PageListConverter<AuthorDao, Author>());
        CreateMap<PageList<ContactDao>, PageList<Contact>>()
            .ConvertUsing(new PageListConverter<ContactDao, Contact>());
        CreateMap<PageList<NoteCatalogDao>, PageList<NoteCatalog>>()
            .ConvertUsing(new PageListConverter<NoteCatalogDao, NoteCatalog>());
        CreateMap<PageList<TagDao>, PageList<Tag>>()
            .ConvertUsing(new PageListConverter<TagDao, Tag>());
        CreateMap<PageList<HmmNoteDao>, PageList<HmmNote>>()
            .ConvertUsing(new PageListConverter<HmmNoteDao, HmmNote>());

        CreateMap<ContactDao, Contact>()
            .ConvertUsing((src, dest) =>
            {
                if (src == null)
                {
                    return null;
                }

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
                        Description = src.Description,
                        IsActivated = src.IsActivated
                    }
                    : new Contact
                    {
                        Id = src.Id,
                        FirstName = contactInfo.FirstName,
                        LastName = contactInfo.LastName,
                        Emails = contactInfo.Emails,
                        Phones = contactInfo.Phones,
                        Addresses = contactInfo.Addresses,
                        Description = src.Description,
                        IsActivated = src.IsActivated
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

        CreateMap<MigrationLogDao, MigrationLog>();
        CreateMap<MigrationLog, MigrationLogDao>();
        CreateMap<PageList<MigrationLogDao>, PageList<MigrationLog>>()
            .ConvertUsing(new PageListConverter<MigrationLogDao, MigrationLog>());

        CreateMap<HmmNoteDao, HmmNote>()
            .ForMember(dest => dest.Tags,
                opt => opt.MapFrom(src => src.Tags.Select(ntr => ntr.Tag)))
            // Decode the per-note JSON column into typed refs.
            // NoteAttachmentsCodec throws FormatException on a
            // malformed / non-vault-kind payload — that's a hard
            // failure path (the manager validates on write so this
            // is "shouldn't happen at read time," but if it does
            // there's no graceful recovery).
            .ForMember(dest => dest.PrimaryImage,
                opt => opt.MapFrom(src =>
                    NoteAttachmentsCodec.Decode(src.Attachments).PrimaryImage))
            .ForMember(dest => dest.Images,
                opt => opt.MapFrom(src =>
                    NoteAttachmentsCodec.Decode(src.Attachments).Images.ToList()));
        CreateMap<HmmNote, HmmNoteDao>()
            .ForMember(dest => dest.Tags,
                opt => opt.MapFrom(src =>
                    src.Tags.Select(tag => new NoteTagRefDao { TagId = tag.Id, NoteId = src.Id })))
            // Encode the two domain fields back into the JSON
            // column. An empty payload encodes to null so the
            // column stores SQL NULL rather than "{}".
            .ForMember(dest => dest.Attachments,
                opt => opt.MapFrom(src =>
                    NoteAttachmentsCodec.Encode(
                        new NoteAttachments(
                            src.PrimaryImage,
                            src.Images))));
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