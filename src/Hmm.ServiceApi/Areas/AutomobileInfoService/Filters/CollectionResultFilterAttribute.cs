using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters
{
    public class CollectionResultFilterAttribute : ResultFilterAttribute
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

            switch (resultFromAction.Value)
            {
                case PageList<ApiAutomobile> autos:
                    {
                        await GenerateCollectionResultValue(context, autos, "GetAutomobiles");
                        break;
                    }
                case PageList<ApiDiscount> discounts:
                    {
                        await GenerateCollectionResultValue(context, discounts, "GetGasDiscounts");
                        break;
                    }
                case PageList<ApiGasLog> gasLogs:
                    {
                        await GenerateCollectionResultValue(context, gasLogs, "GetGasLogs");
                        break;
                    }
                case PageList<ApiAuthor> authors:
                    {
                        await GenerateCollectionResultValue(context, authors, "GetAuthors");
                        break;
                    }
                case PageList<ApiNoteRender> renders:
                    {
                        await GenerateCollectionResultValue(context, renders, "GetNoteRenders");
                        break;
                    }
                case PageList<ApiNoteCatalog> catalogs:
                    {
                        await GenerateCollectionResultValue(context, catalogs, "GetNoteCatalogs");
                        break;
                    }
                case PageList<ApiSubsystem> systems:
                    {
                        await GenerateCollectionResultValue(context, systems, "GetSubsystems");
                        break;
                    }
                case PageList<ApiNote> notes:
                    {
                        await GenerateCollectionResultValue(context, notes, "GetNotes");
                        break;
                    }
            }

            await next();
        }

        private static async Task GenerateCollectionResultValue<T>(ResultExecutingContext context, PageList<T> records, string routName)
        {
            if (!records.Any() || context.Result is not ObjectResult resultFromAction)
            {
                return;
            }

            GeneratePaginationHeader(context, records.TotalCount, records.PageSize, records.CurrentPage, records.TotalPages);

            var linkGen = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();

            // Get resource collection parameter
            var paraDesc = context.ActionDescriptor.Parameters.FirstOrDefault(t => t.Name.IsCollectionParameter());
            object parameter = null;
            if (paraDesc != null && context.Controller is Controller controller)
            {
                parameter = Activator.CreateInstance(paraDesc.ParameterType);
                if (parameter != null)
                {
                    await controller.TryUpdateModelAsync(parameter, paraDesc.ParameterType, "");
                }
            }

            if (parameter is ResourceCollectionParameters collectionResourcePara)
            {
                var links = records.CreatePaginationLinks(routName, context, linkGen, collectionResourcePara);
                resultFromAction.Value = new { value = records.ShapeData(collectionResourcePara.Fields), links };
            }
        }

        private static void GeneratePaginationHeader(ActionContext context, int totalCount, int pageSize,
            int currentPage, int totalPages)
        {
            if (context == null)
            {
                return;
            }

            var paginationMetadata = new
            {
                totalCount,
                pageSize,
                currentPage,
                totalPages,
                maxPageSize = ResourceCollectionParameters.MaxPageSize,
            };

            context.HttpContext.Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
        }
    }
}