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

public class AutoScheduledServicesResultFilter : ResultFilterBase
{
    public AutoScheduledServicesResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<AutoScheduledService> schedules && schedules.Any())
        {
            var result = Mapper.Map<PageList<AutoScheduledService>, PageList<ApiAutoScheduledService>>(schedules);
            var autoId = schedules.First().AutomobileId;
            foreach (var s in result)
            {
                s.CreateLinks(context, LinkGenerator, autoId);
            }
            resultFromAction.Value = result;
        }

        return next();
    }
}
