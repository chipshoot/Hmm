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
/// Result filter that transforms a single GasDiscount to ApiDiscount.
/// Apply using [TypeFilter(typeof(GasDiscountResultFilter))].
/// </summary>
public class GasDiscountResultFilter : ResultFilterBase
{
    public GasDiscountResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is GasDiscount discount)
        {
            var apiDiscount = Mapper.Map<GasDiscount, ApiDiscount>(discount);
            apiDiscount.CreateLinks(context, LinkGenerator);
            resultFromAction.Value = apiDiscount;
        }

        return next();
    }
}
