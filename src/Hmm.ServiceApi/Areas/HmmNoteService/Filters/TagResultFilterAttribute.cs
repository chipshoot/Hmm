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
/// Result filter that transforms a single Tag to ApiTag.
/// Apply using [TypeFilter(typeof(TagResultFilter))].
/// </summary>
public class TagResultFilter : ResultFilterBase
{
    public TagResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is Tag tag)
        {
            var apiTag = Mapper.Map<Tag, ApiTag>(tag);
            apiTag.CreateLinks(context, LinkGenerator);
            resultFromAction.Value = apiTag;
        }

        return next();
    }
}
