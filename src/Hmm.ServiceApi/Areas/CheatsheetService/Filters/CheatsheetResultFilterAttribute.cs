using System.Threading.Tasks;
using AutoMapper;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Filters;

/// <summary>
/// Transforms a single CheatsheetCard into ApiCheatsheet.
/// Apply using [TypeFilter(typeof(CheatsheetResultFilter))].
/// </summary>
public class CheatsheetResultFilter : ResultFilterBase
{
    public CheatsheetResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is CheatsheetCard card)
        {
            var apiCard = Mapper.Map<CheatsheetCard, ApiCheatsheet>(card);
            apiCard.CreateLinks(context, LinkGenerator);
            resultFromAction.Value = apiCard;
        }

        return next();
    }
}
