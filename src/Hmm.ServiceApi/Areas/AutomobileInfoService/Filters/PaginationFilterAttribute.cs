using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.AutomobileInfoService.Filters
{
    public class PaginationFilterAttribute : ResultFilterAttribute
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

            var linkGen = context.HttpContext.RequestServices.GetRequiredService<LinkGenerator>();
            if (resultFromAction.Value is PageList<ApiAutomobile> autos)
            {
                if (autos.Any())
                {
                    var (prevPageLink, nextPageLink) = autos.CreatePaginationLinks(context, linkGen);
                    var paginationMetadata = new
                    {
                        totalCount = autos.TotalCount,
                        pageSize = autos.PageSize,
                        currentPage = autos.CurrentPage,
                        totalPages = autos.TotalPages,
                        prevPageLink,
                        nextPageLink
                    };

                    context.HttpContext.Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
                }
            }

            await next();
        }
    }
}