using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;

/// <summary>
/// Result filter that transforms a single GasStation to ApiGasStation.
/// Apply using [TypeFilter(typeof(GasStationResultFilter))].
/// </summary>
public class GasStationResultFilter : ResultFilterBase
{
    public GasStationResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is GasStation station)
        {
            var apiStation = Mapper.Map<GasStation, ApiGasStation>(station);
            apiStation.CreateLinks(context, LinkGenerator);
            resultFromAction.Value = apiStation;
        }

        return next();
    }
}
