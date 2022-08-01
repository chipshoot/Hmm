using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters
{
    public class GasLogsResultFilterAttribute : ResultFilterAttribute
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

            if (resultFromAction.Value is PageList<GasLog> logs && logs.Any())
            {
                var mapper = context.HttpContext.RequestServices.GetRequiredService<IMapper>();
                var linkGen = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();

                if (mapper != null)
                {
                    var result = mapper.Map<PageList<GasLog>, PageList<ApiGasLog>>(logs);
                    foreach (var log in result)
                    {
                        log.CreateLinks(context, linkGen);
                    }
                    resultFromAction.Value = result;
                }
            }

            await next();
        }
    }
}