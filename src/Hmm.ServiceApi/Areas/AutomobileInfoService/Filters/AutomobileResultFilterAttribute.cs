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
/// Result filter that transforms a single AutomobileInfo to ApiAutomobile.
/// Apply using [TypeFilter(typeof(AutomobileResultFilter))].
/// </summary>
public class AutomobileResultFilter : ResultFilterBase
{
    public AutomobileResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is AutomobileInfo auto)
        {
            var apiAuto = Mapper.Map<AutomobileInfo, ApiAutomobile>(auto);
            apiAuto.CreateLinks(context, LinkGenerator);
            resultFromAction.Value = apiAuto;
        }

        return next();
    }
}
