using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;

public class ServiceRecordResultFilter : ResultFilterBase
{
    public ServiceRecordResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is ServiceRecord record)
        {
            var api = Mapper.Map<ServiceRecord, ApiServiceRecord>(record);
            api.CreateLinks(context, LinkGenerator, record.AutomobileId);
            resultFromAction.Value = api;
        }

        return next();
    }
}
