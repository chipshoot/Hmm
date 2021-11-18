using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters
{
    public class AutomobilesResultFilterAttribute : ResultFilterAttribute
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

            if (resultFromAction.Value is List<AutomobileInfo> autos && autos.Any())
            {
                var mapper = context.HttpContext.RequestServices.GetRequiredService<IMapper>();
                var linkGen = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();

                if (mapper != null)
                {
                    var result = mapper.Map<List<AutomobileInfo>, List<ApiAutomobile>>(autos).ToList();
                    foreach (var auto in result)
                    {
                        auto.CreateLinks(context, linkGen);
                    }
                    resultFromAction.Value = result;
                }
            }

            await next();
        }
    }
}