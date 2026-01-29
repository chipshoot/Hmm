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
/// Result filter that transforms a PageList of Tag to PageList of ApiTag.
/// Apply using [TypeFilter(typeof(TagsResultFilter))].
/// </summary>
public class TagsResultFilter : ResultFilterBase
{
    public TagsResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<Tag> tags && tags.Any())
        {
            var result = Mapper.Map<PageList<Tag>, PageList<ApiTag>>(tags);
            foreach (var tag in result)
            {
                tag.CreateLinks(context, LinkGenerator);
            }

            resultFromAction.Value = result;
        }

        return next();
    }
}
