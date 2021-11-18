using AutoMapper;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Filters
{
    public class NoteRenderResultFilterAttribute : ResultFilterAttribute
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
                var newApiNoteRender = mapper.Map<NoteRender, ApiNoteRender>(resultFromAction.Value as NoteRender);
                newApiNoteRender.CreateLinks(context, linkGen);
                resultFromAction.Value = newApiNoteRender;
            }

            await next();
        }
    }
}