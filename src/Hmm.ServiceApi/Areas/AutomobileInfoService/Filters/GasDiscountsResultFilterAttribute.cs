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
/// Result filter that transforms a PageList of GasDiscount to PageList of ApiDiscount.
/// Apply using [TypeFilter(typeof(GasDiscountsResultFilter))].
/// </summary>
public class GasDiscountsResultFilter : ResultFilterBase
{
    public GasDiscountsResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<GasDiscount> discounts && discounts.Any())
        {
            var result = Mapper.Map<PageList<GasDiscount>, PageList<ApiDiscount>>(discounts);
            foreach (var discount in result)
            {
                discount.CreateLinks(context, LinkGenerator);
            }
            resultFromAction.Value = result;
        }

        return next();
    }
}
