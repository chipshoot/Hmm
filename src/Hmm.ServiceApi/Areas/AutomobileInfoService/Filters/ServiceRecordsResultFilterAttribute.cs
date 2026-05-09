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

public class ServiceRecordsResultFilter : ResultFilterBase
{
    public ServiceRecordsResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<ServiceRecord> records && records.Any())
        {
            var result = Mapper.Map<PageList<ServiceRecord>, PageList<ApiServiceRecord>>(records);
            var autoId = records.First().AutomobileId;
            foreach (var r in result)
            {
                r.CreateLinks(context, LinkGenerator, autoId);
            }
            resultFromAction.Value = result;
        }

        return next();
    }
}
