using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;

public class AutoInsurancePolicyResultFilter : ResultFilterBase
{
    public AutoInsurancePolicyResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is AutoInsurancePolicy policy)
        {
            var api = Mapper.Map<AutoInsurancePolicy, ApiAutoInsurancePolicy>(policy);
            api.CreateLinks(context, LinkGenerator, policy.AutomobileId);
            resultFromAction.Value = api;
        }

        return next();
    }
}
