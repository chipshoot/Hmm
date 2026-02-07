using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Filters;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;

/// <summary>
/// Result filter that transforms a PageList of GasLog to PageList of ApiGasLog.
/// Apply using [TypeFilter(typeof(GasLogsResultFilter))].
/// </summary>
public class GasLogsResultFilter : ResultFilterBase
{
    public GasLogsResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<GasLog> logs && logs.Any())
        {
            var result = Mapper.Map<PageList<GasLog>, PageList<ApiGasLog>>(logs);
            var autoId = logs.First().AutomobileId;
            foreach (var log in result)
            {
                log.CreateLinks(context, LinkGenerator, autoId);
            }
            resultFromAction.Value = result;
        }

        return next();
    }
}
