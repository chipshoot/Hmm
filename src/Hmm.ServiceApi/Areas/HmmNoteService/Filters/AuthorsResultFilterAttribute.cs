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
/// Result filter that transforms a PageList of Author to PageList of ApiAuthor.
/// Apply using [TypeFilter(typeof(AuthorsResultFilter))].
/// </summary>
public class AuthorsResultFilter : ResultFilterBase
{
    public AuthorsResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<Author> authors && authors.Any())
        {
            var result = Mapper.Map<PageList<Author>, PageList<ApiAuthor>>(authors);
            foreach (var author in result)
            {
                author.CreateLinks(context, LinkGenerator);
            }

            resultFromAction.Value = result;
        }

        return next();
    }
}
