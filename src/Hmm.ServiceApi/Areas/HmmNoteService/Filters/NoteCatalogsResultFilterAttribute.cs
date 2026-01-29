using AutoMapper;
using Hmm.Core.Map.DomainEntity;
using Hmm.ServiceApi.DtoEntity.HmmNote;
using Hmm.ServiceApi.Filters;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Areas.HmmNoteService.Filters;

/// <summary>
/// Result filter that transforms a PageList of NoteCatalog to PageList of ApiNoteCatalog.
/// Apply using [TypeFilter(typeof(NoteCatalogsResultFilter))].
/// </summary>
public class NoteCatalogsResultFilter : ResultFilterBase
{
    public NoteCatalogsResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<NoteCatalog> catalogs && catalogs.Any())
        {
            var result = Mapper.Map<PageList<NoteCatalog>, PageList<ApiNoteCatalog>>(catalogs);
            foreach (var catalog in result)
            {
                catalog.CreateLinks(context, LinkGenerator);
            }

            resultFromAction.Value = result;
        }

        return next();
    }
}
