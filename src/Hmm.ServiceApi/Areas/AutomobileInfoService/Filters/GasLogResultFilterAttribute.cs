using AutoMapper;
using Hmm.Automobile.DomainEntity;
using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters
{
    public class GasLogResultFilterAttribute : ResultFilterAttribute
    {
        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var resultFromAction = context.Result as ObjectResult;
            if (resultFromAction?.Value == null ||
                resultFromAction.StatusCode is < 200 or >= 300)
            {
                await next();
                return;
            }

            // Get resource collection parameter
            var paraDesc = context.ActionDescriptor.Parameters.FirstOrDefault(t => t.Name.IsFieldsParameter());
            var fields = string.Empty;
            if (paraDesc != null && paraDesc.ParameterType == typeof(string) && context.Controller is Controller)
            {
                fields = context.HttpContext.Request.Query[paraDesc.Name].ToString();
            }

            var mapper = context.HttpContext.RequestServices.GetRequiredService<IMapper>();
            var linkGen = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();
            if (mapper != null)
            {
                var gasLog = resultFromAction.Value as GasLog;
                var newApiGasLog = mapper.Map<GasLog, ApiGasLog>(gasLog);
                newApiGasLog.CreateLinks(context, linkGen, gasLog.Car.Id);
                var links = newApiGasLog.Links;
                resultFromAction.Value = new { value = newApiGasLog.ShapeData(fields), links };
            }

            await next();
        }
    }
}
