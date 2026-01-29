using AutoMapper;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Filters;

/// <summary>
/// Result filter that transforms a single Contact to ApiContact.
/// Apply using [TypeFilter(typeof(ContactResultFilter))].
/// </summary>
public class ContactResultFilter : ResultFilterBase
{
    public ContactResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is Contact contact)
        {
            var apiContact = Mapper.Map<Contact, ApiContact>(contact);
            apiContact.CreateLinks(context, LinkGenerator);
            resultFromAction.Value = apiContact;
        }

        return next();
    }
}
