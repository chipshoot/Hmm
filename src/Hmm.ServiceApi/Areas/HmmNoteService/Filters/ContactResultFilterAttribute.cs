using AutoMapper;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Filters
{
    public class ContactResultFilterAttribute : ResultFilterAttribute
    {
        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var resultFromAction = context.Result as ObjectResult;
            if (resultFromAction?.Value == null ||
                resultFromAction.StatusCode is < 200 or >= 300)
            {
                await next();
                return;
            }

            var mapper = context.HttpContext.RequestServices.GetRequiredService<IMapper>();
            var linkGen = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();
            if (mapper != null)
            {
                var newApiContact = mapper.Map<Contact, ApiContact>(resultFromAction.Value as Contact);
                newApiContact.CreateLinks(context, linkGen);
                resultFromAction.Value = newApiContact;
            }

            await next();
        }
    }
}