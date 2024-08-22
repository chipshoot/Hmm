// Ignore Spelling: Dto

using AutoMapper;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.Utility.Dal.Query;
using System;

namespace Hmm.ServiceApi.DtoEntity.Profiles
{
    public class ApiMappingProfile : Profile
    {
        public ApiMappingProfile()
        {
            // Author
            CreateMap<ApiAuthor, Author>()
                .ForMember(d => d.Role, opt => opt.MapFrom(src => Enum.Parse(typeof(AuthorRoleType), src.Role, true)));
            CreateMap<Author, ApiAuthor>()
                .ForMember(d => d.Role, opt => opt.MapFrom(src => src.Role.ToString().ToLower()));
            CreateMap<ApiAuthorForCreate, Author>();
            CreateMap<ApiAuthorForUpdate, Author>();
            CreateMap<Author, ApiAuthorForCreate>();
            CreateMap<Author, ApiAuthorForUpdate>();
            CreateMap<PageList<Author>, PageList<ApiAuthor>>()
                .ConvertUsing(new PageListConverter<Author, ApiAuthor>());

            // Contact
            CreateMap<ApiContact, Contact>();
            CreateMap<Contact, ApiContact>();
            CreateMap<ApiContactForCreate, Contact>();
            CreateMap<ApiContactForUpdate, Contact>();
            CreateMap<Contact, ApiContactForCreate>();
            CreateMap<Contact, ApiContactForUpdate>();
            CreateMap<ApiEmail, Email>();
            CreateMap<Email, ApiEmail>();
            CreateMap<Phone, ApiPhone>();
            CreateMap<ApiPhone, Phone>();
            CreateMap<ApiAddressInfo, AddressInfo>();
            CreateMap<AddressInfo, ApiAddressInfo>();

            // Catalog
            CreateMap<ApiNoteCatalog, NoteCatalog>();
            CreateMap<NoteCatalog, ApiNoteCatalog>();

            CreateMap<ApiNoteCatalogForCreate, NoteCatalog>();
            CreateMap<ApiNoteCatalogForUpdate, NoteCatalog>();
            CreateMap<NoteCatalog, ApiNoteCatalogForUpdate>();
            CreateMap<PageList<NoteCatalog>, PageList<ApiNoteCatalog>>()
                .ConvertUsing(new PageListConverter<NoteCatalog, ApiNoteCatalog>());

            // Tag
            CreateMap<ApiTag, Tag>();
            CreateMap<Tag, ApiTag>();
            CreateMap<ApiTagForApply, Tag>()
                .ForMember(d => d.Description, opt => opt.Ignore());

            CreateMap<ApiTagForCreate, Tag>();
            CreateMap<ApiTagForUpdate, Tag>();
            CreateMap<Tag, ApiTagForUpdate>();
            CreateMap<PageList<Tag>, PageList<ApiTag>>()
                .ConvertUsing(new PageListConverter<Tag, ApiTag>());

            // Note
            CreateMap<Core.Map.DomainEntity.HmmNote, ApiNote>()
                .ForMember(n => n.AuthorId, opt => opt.MapFrom(s => s.Author.Id))
                .ForMember(n => n.CatalogId, opt => opt.MapFrom(s => s.Catalog.Id));

            CreateMap<ApiNoteForCreate, Core.Map.DomainEntity.HmmNote>()
                .ForMember(n => n.Author, opt => opt.MapFrom(s => new Author { Id = s.AuthorId }))
                .ForMember(n => n.Catalog, opt => opt.MapFrom(s => new NoteCatalog() { Id = s.NoteCatalogId }));
            CreateMap<ApiNoteForUpdate, Core.Map.DomainEntity.HmmNote>();
            CreateMap<Core.Map.DomainEntity.HmmNote, ApiNoteForCreate>();
            CreateMap<Core.Map.DomainEntity.HmmNote, ApiNoteForUpdate>();
            CreateMap<PageList<Core.Map.DomainEntity.HmmNote>, PageList<ApiNote>>()
                .ConvertUsing(new PageListConverter<Core.Map.DomainEntity.HmmNote, ApiNote>());
        }
    }
}