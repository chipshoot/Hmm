using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;

public class AutoScheduledServiceResultFilter : ResultFilterBase
{
    public AutoScheduledServiceResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is AutoScheduledService schedule)
        {
            var api = Mapper.Map<AutoScheduledService, ApiAutoScheduledService>(schedule);
            api.CreateLinks(context, LinkGenerator, schedule.AutomobileId);
            resultFromAction.Value = api;
        }

        return next();
    }
}
