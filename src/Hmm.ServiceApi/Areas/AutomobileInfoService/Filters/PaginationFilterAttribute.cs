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
    public class PaginationFilterAttribute : ResultFilterAttribute
    {
        private const string CollectionParameterName = "resourceParameters";

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
                        await GeneratePaginationValue(context, autos, "GetAutomobiles");
                        break;
                    }
                case PageList<ApiDiscount> discounts:
                    {
                        await GeneratePaginationValue(context, discounts, "GetGasDiscounts");
                        break;
                    }
                case PageList<ApiGasLog> gasLogs:
                    {
                        await GeneratePaginationValue(context, gasLogs, "GetGasLogs");
                        break;
                    }
                case PageList<ApiAuthor> authors:
                    {
                        await GeneratePaginationValue(context, authors, "GetAuthors");
                        break;
                    }
                case PageList<ApiNoteRender> renders:
                    {
                        await GeneratePaginationValue(context, renders, "GetNoteRenders");
                        break;
                    }
                case PageList<ApiNoteCatalog> catalogs:
                    {
                        await GeneratePaginationValue(context, catalogs, "GetNoteCatalogs");
                        break;
                    }
                case PageList<ApiSubsystem> systems:
                    {
                        await GeneratePaginationValue(context, systems, "GetSubsystems");
                        break;
                    }
                case PageList<ApiNote> notes:
                    {
                        await GeneratePaginationValue(context, notes, "GetNotes");
                        break;
                    }
            }

            await next();
        }

        private static async Task GeneratePaginationValue<T>(ResultExecutingContext context, PageList<T> records, string routName)
        {
            if (!records.Any() || context.Result is not ObjectResult resultFromAction)
            {
                return;
            }

            GeneratePaginationHeader(context, records.TotalCount, records.PageSize, records.CurrentPage, records.TotalPages);

            var linkGen = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();

            // Get resource collection parameter
            var paraDesc =
                context.ActionDescriptor.Parameters.FirstOrDefault(
                    t => t.Name.Equals(CollectionParameterName));
            object parameter = null;
            if (paraDesc != null && context.Controller is Controller controller)
            {
                parameter = Activator.CreateInstance(paraDesc.ParameterType);
                if (parameter != null)
                {
                    await controller.TryUpdateModelAsync(parameter, paraDesc.ParameterType, "");
                }
            }

            var links = records.CreatePaginationLinks(routName, context, linkGen, (ResourceCollectionParameters)parameter);
            resultFromAction.Value = new { value = records, links };
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