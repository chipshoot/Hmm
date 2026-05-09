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

public class AutoInsurancePoliciesResultFilter : ResultFilterBase
{
    public AutoInsurancePoliciesResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<AutoInsurancePolicy> policies && policies.Any())
        {
            var result = Mapper.Map<PageList<AutoInsurancePolicy>, PageList<ApiAutoInsurancePolicy>>(policies);
            var autoId = policies.First().AutomobileId;
            foreach (var p in result)
            {
                p.CreateLinks(context, LinkGenerator, autoId);
            }
            resultFromAction.Value = result;
        }

        return next();
    }
}
