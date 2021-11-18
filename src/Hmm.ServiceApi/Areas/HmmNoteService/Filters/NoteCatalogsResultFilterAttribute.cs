using AutoMapper;
using Hmm.Core.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Filters
{
    public class NoteCatalogsResultFilterAttribute : ResultFilterAttribute
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

            if (resultFromAction.Value is List<NoteCatalog> catalogs && catalogs.Any())
            {
                var mapper = context.HttpContext.RequestServices.GetRequiredService<IMapper>();
                var linkGen = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();
                if (mapper != null)
                {
                    var result = mapper.Map<List<NoteCatalog>, List<ApiNoteCatalog>>(catalogs).ToList();
                    foreach (var catalog in result)
                    {
                        catalog.CreateLinks(context, linkGen);
                    }

                    resultFromAction.Value = result;
                }
            }
            await next();
        }
    }
}