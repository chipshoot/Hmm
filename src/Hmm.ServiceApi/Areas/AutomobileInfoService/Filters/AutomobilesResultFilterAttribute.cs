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
/// Result filter that transforms a PageList of AutomobileInfo to PageList of ApiAutomobile.
/// Apply using [TypeFilter(typeof(AutomobilesResultFilter))].
/// </summary>
public class AutomobilesResultFilter : ResultFilterBase
{
    public AutomobilesResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<AutomobileInfo> autos && autos.Any())
        {
            var result = Mapper.Map<PageList<AutomobileInfo>, PageList<ApiAutomobile>>(autos);
            foreach (var auto in result)
            {
                auto.CreateLinks(context, LinkGenerator);
            }
            resultFromAction.Value = result;
        }

        return next();
    }
}
