using Hmm.ServiceApi.Areas.HmmNoteService.Filters;
using Hmm.ServiceApi.DtoEntity.GasLogNotes;
using Hmm.ServiceApi.DtoEntity.HmmNote;
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
            switch (resultFromAction.Value)
            {
                case PageList<ApiAutomobile> autos:
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
                                maxPageSize = ResourceCollectionParameters.MaxPageSize,
                                prevPageLink,
                                nextPageLink
                            };

                            context.HttpContext.Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
                        }

                        break;
                    }
                case PageList<ApiDiscount> discounts:
                    {
                        if (discounts.Any())
                        {
                            var (prevPageLink, nextPageLink) = discounts.CreatePaginationLinks(context, linkGen);
                            var paginationMetadata = new
                            {
                                totalCount = discounts.TotalCount,
                                pageSize = discounts.PageSize,
                                currentPage = discounts.CurrentPage,
                                totalPages = discounts.TotalPages,
                                maxPageSize = ResourceCollectionParameters.MaxPageSize,
                                prevPageLink,
                                nextPageLink
                            };

                            context.HttpContext.Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
                        }

                        break;
                    }
                case PageList<ApiGasLog> gasLogs:
                    {
                        if (gasLogs.Any())
                        {
                            var (prevPageLink, nextPageLink) = gasLogs.CreatePaginationLinks(context, linkGen);
                            var paginationMetadata = new
                            {
                                totalCount = gasLogs.TotalCount,
                                pageSize = gasLogs.PageSize,
                                currentPage = gasLogs.CurrentPage,
                                totalPages = gasLogs.TotalPages,
                                maxPageSize = ResourceCollectionParameters.MaxPageSize,
                                prevPageLink,
                                nextPageLink
                            };

                            context.HttpContext.Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
                        }

                        break;
                    }
                case PageList<ApiAuthor> authors:
                    {
                        if (authors.Any())
                        {
                            var (prevPageLink, nextPageLink) = authors.CreatePaginationLinks(context, linkGen);
                            var paginationMetadata = new
                            {
                                totalCount = authors.TotalCount,
                                pageSize = authors.PageSize,
                                currentPage = authors.CurrentPage,
                                totalPages = authors.TotalPages,
                                maxPageSize = ResourceCollectionParameters.MaxPageSize,
                                prevPageLink,
                                nextPageLink
                            };

                            context.HttpContext.Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
                        }

                        break;
                    }
                case PageList<ApiNoteRender> renders:
                    {
                        if (renders.Any())
                        {
                            var (prevPageLink, nextPageLink) = renders.CreatePaginationLinks(context, linkGen);
                            var paginationMetadata = new
                            {
                                totalCount = renders.TotalCount,
                                pageSize = renders.PageSize,
                                currentPage = renders.CurrentPage,
                                totalPages = renders.TotalPages,
                                maxPageSize = ResourceCollectionParameters.MaxPageSize,
                                prevPageLink,
                                nextPageLink
                            };

                            context.HttpContext.Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
                        }

                        break;
                    }
                case PageList<ApiNoteCatalog> catalogs:
                    {
                        if (catalogs.Any())
                        {
                            var (prevPageLink, nextPageLink) = catalogs.CreatePaginationLinks(context, linkGen);
                            var paginationMetadata = new
                            {
                                totalCount = catalogs.TotalCount,
                                pageSize = catalogs.PageSize,
                                currentPage = catalogs.CurrentPage,
                                totalPages = catalogs.TotalPages,
                                maxPageSize = ResourceCollectionParameters.MaxPageSize,
                                prevPageLink,
                                nextPageLink
                            };

                            context.HttpContext.Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
                        }

                        break;
                    }
                case PageList<ApiSubsystem> systems:
                    {
                        if (systems.Any())
                        {
                            var (prevPageLink, nextPageLink) = systems.CreatePaginationLinks(context, linkGen);
                            var paginationMetadata = new
                            {
                                totalCount = systems.TotalCount,
                                pageSize = systems.PageSize,
                                currentPage = systems.CurrentPage,
                                totalPages = systems.TotalPages,
                                maxPageSize = ResourceCollectionParameters.MaxPageSize,
                                prevPageLink,
                                nextPageLink
                            };

                            context.HttpContext.Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
                        }

                        break;
                    }
                case PageList<ApiNote> notes:
                    {
                        if (notes.Any())
                        {
                            var (prevPageLink, nextPageLink) = notes.CreatePaginationLinks(context, linkGen);
                            var paginationMetadata = new
                            {
                                totalCount = notes.TotalCount,
                                pageSize = notes.PageSize,
                                currentPage = notes.CurrentPage,
                                totalPages = notes.TotalPages,
                                maxPageSize = ResourceCollectionParameters.MaxPageSize,
                                prevPageLink,
                                nextPageLink
                            };

                            context.HttpContext.Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
                        }

                        break;
                    }
            }

            await next();
        }
    }
}