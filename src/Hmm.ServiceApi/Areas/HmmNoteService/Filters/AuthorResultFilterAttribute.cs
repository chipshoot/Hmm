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
/// Result filter that transforms a single Author to ApiAuthor.
/// Apply using [TypeFilter(typeof(AuthorResultFilter))].
/// </summary>
public class AuthorResultFilter : ResultFilterBase
{
    public AuthorResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is Author author)
        {
            var apiAuthor = Mapper.Map<Author, ApiAuthor>(author);
            apiAuthor.CreateLinks(context, LinkGenerator);
            resultFromAction.Value = apiAuthor;
        }

        return next();
    }
}
