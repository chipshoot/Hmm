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
/// Result filter that transforms a PageList of HmmNote to PageList of ApiNote.
/// Apply using [TypeFilter(typeof(NotesResultFilter))].
/// </summary>
public class NotesResultFilter : ResultFilterBase
{
    public NotesResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<HmmNote> notes && notes.Any())
        {
            var result = Mapper.Map<PageList<HmmNote>, PageList<ApiNote>>(notes);
            foreach (var note in result)
            {
                note.CreateLinks(context, LinkGenerator);
            }

            resultFromAction.Value = result;
        }

        return next();
    }
}
