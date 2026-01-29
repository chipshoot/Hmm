using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Filters;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using AutoMapper;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Filters;

/// <summary>
/// Result filter that handles collection results with pagination support.
/// Apply using [TypeFilter(typeof(CollectionResultFilter))].
/// </summary>
public class CollectionResultFilter : ResultFilterBase
{
    public CollectionResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override async Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        switch (resultFromAction.Value)
        {
            case PageList<ApiAuthor> authors:
                await GenerateCollectionResultValue(context, authors, "GetAuthors");
                break;
            case PageList<ApiNoteCatalog> catalogs:
                await GenerateCollectionResultValue(context, catalogs, "GetNoteCatalogs");
                break;
            case PageList<ApiNote> notes:
                await GenerateCollectionResultValue(context, notes, "GetNotes");
                break;
        }

        await next();
    }

    private async Task GenerateCollectionResultValue<T>(ResultExecutingContext context, PageList<T> records, string routName)
    {
        if (!records.Any() || context.Result is not ObjectResult resultFromAction)
        {
            return;
        }

        GeneratePaginationHeader(context, records.TotalCount, records.PageSize, records.CurrentPage, records.TotalPages);

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
            var links = records.CreatePaginationLinks(routName, context, LinkGenerator, collectionResourcePara);
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

        context.HttpContext?.Response.Headers?.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata));
    }
}
