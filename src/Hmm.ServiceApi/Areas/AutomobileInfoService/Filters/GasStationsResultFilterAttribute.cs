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
/// Result filter that transforms a PageList of GasStation to PageList of ApiGasStation.
/// Apply using [TypeFilter(typeof(GasStationsResultFilter))].
/// </summary>
public class GasStationsResultFilter : ResultFilterBase
{
    public GasStationsResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<GasStation> stations && stations.Any())
        {
            var result = Mapper.Map<PageList<GasStation>, PageList<ApiGasStation>>(stations);
            foreach (var station in result)
            {
                station.CreateLinks(context, LinkGenerator);
            }
            resultFromAction.Value = result;
        }

        return next();
    }
}
