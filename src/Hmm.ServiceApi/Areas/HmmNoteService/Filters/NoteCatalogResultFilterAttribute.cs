using AutoMapper;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Filters;

/// <summary>
/// Result filter that transforms a single NoteCatalog to ApiNoteCatalog.
/// Apply using [TypeFilter(typeof(NoteCatalogResultFilter))].
/// </summary>
public class NoteCatalogResultFilter : ResultFilterBase
{
    public NoteCatalogResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is NoteCatalog catalog)
        {
            var apiCatalog = Mapper.Map<NoteCatalog, ApiNoteCatalog>(catalog);
            apiCatalog.CreateLinks(context, LinkGenerator);
            resultFromAction.Value = apiCatalog;
        }

        return next();
    }
}
