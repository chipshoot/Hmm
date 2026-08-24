using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Hmm.ServiceApi.Filters;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Filters;

/// <summary>
/// Transforms a PageList of CheatsheetCard into a PageList of ApiCheatsheet and
/// writes the X-Pagination header.
///
/// This deliberately does NOT reuse the shared CollectionResultFilter: that
/// filter runs ShapeData, which reflects every public property into an
/// ExpandoObject. Cheatsheet DTOs carry preserved data through
/// [JsonExtensionData] and a row JsonConverter, both of which reflection
/// flattens - the response would nest extras under "ExtraFields" and lose the
/// verbatim row shape. Keeping the typed objects keeps the wire format honest.
/// </summary>
public class CheatsheetsResultFilter : ResultFilterBase
{
    public CheatsheetsResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<CheatsheetCard> cards)
        {
            var apiCards = Mapper.Map<PageList<CheatsheetCard>, PageList<ApiCheatsheet>>(cards);
            foreach (var apiCard in apiCards)
            {
                apiCard.CreateLinks(context, LinkGenerator);
            }

            WritePaginationHeader(context, cards);
            resultFromAction.Value = apiCards;
        }

        return next();
    }

    private static void WritePaginationHeader(
        ResultExecutingContext context,
        PageList<CheatsheetCard> cards)
    {
        var metadata = new
        {
            totalCount = cards.TotalCount,
            pageSize = cards.PageSize,
            currentPage = cards.CurrentPage,
            totalPages = cards.TotalPages,
            maxPageSize = ResourceCollectionParameters.MaxPageSize
        };

        context.HttpContext?.Response.Headers.Append(
            "X-Pagination",
            JsonSerializer.Serialize(metadata));
    }
}
