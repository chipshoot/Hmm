using AutoMapper;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Filters;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Filters;

/// <summary>
/// Result filter that transforms a PageList of Contact to PageList of ApiContact.
/// Apply using [TypeFilter(typeof(ContactsResultFilter))].
/// </summary>
public class ContactsResultFilter : ResultFilterBase
{
    public ContactsResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<Contact> contacts && contacts.Any())
        {
            var result = Mapper.Map<PageList<Contact>, PageList<ApiContact>>(contacts);
            foreach (var contact in result)
            {
                contact.CreateLinks(context, LinkGenerator);
            }

            resultFromAction.Value = result;
        }

        return next();
    }
}
