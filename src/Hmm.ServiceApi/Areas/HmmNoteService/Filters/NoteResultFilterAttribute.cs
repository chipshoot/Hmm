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
/// Result filter that transforms a single HmmNote to ApiNote.
/// Apply using [TypeFilter(typeof(NoteResultFilter))].
/// </summary>
public class NoteResultFilter : ResultFilterBase
{
    public NoteResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is HmmNote note)
        {
            var apiNote = Mapper.Map<HmmNote, ApiNote>(note);
            apiNote.CreateLinks(context, LinkGenerator);
            resultFromAction.Value = apiNote;
        }

        return next();
    }
}
