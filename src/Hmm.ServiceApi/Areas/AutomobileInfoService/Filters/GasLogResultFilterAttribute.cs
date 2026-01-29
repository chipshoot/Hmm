using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters;

/// <summary>
/// Result filter that transforms a single GasLog to ApiGasLog with data shaping.
/// Apply using [TypeFilter(typeof(GasLogResultFilter))].
/// </summary>
public class GasLogResultFilter : ResultFilterBase
{
    public GasLogResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is GasLog gasLog)
        {
            // Get resource collection parameter
            var paraDesc = context.ActionDescriptor.Parameters.FirstOrDefault(t => t.Name.IsFieldsParameter());
            var fields = string.Empty;
            if (paraDesc != null && paraDesc.ParameterType == typeof(string) && context.Controller is Controller)
            {
                fields = context.HttpContext.Request.Query[paraDesc.Name].ToString();
            }

            var apiGasLog = Mapper.Map<GasLog, ApiGasLog>(gasLog);
            apiGasLog.CreateLinks(context, LinkGenerator, gasLog.Car.Id);
            var links = apiGasLog.Links;
            resultFromAction.Value = new { value = apiGasLog.ShapeData(fields), links };
        }

        return next();
    }
}
