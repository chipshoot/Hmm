using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Filters;

/// <summary>
/// Base class for result filters that provides constructor injection for common dependencies.
/// Eliminates the service locator anti-pattern (GetRequiredService) used in derived filters.
/// Apply derived filters using [TypeFilter(typeof(DerivedFilter))] on controller actions.
/// </summary>
public abstract class ResultFilterBase : IAsyncResultFilter
{
    protected IMapper Mapper { get; }
    protected LinkGenerator LinkGenerator { get; }

    protected ResultFilterBase(IMapper mapper, LinkGenerator linkGenerator)
    {
        Mapper = mapper;
        LinkGenerator = linkGenerator;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var resultFromAction = context.Result as ObjectResult;
        if (resultFromAction?.Value == null ||
            resultFromAction.StatusCode is < 200 or >= 300)
        {
            await next();
            return;
        }

        await TransformResultAsync(context, resultFromAction, next);
    }

    /// <summary>
    /// Override this method to transform the result value.
    /// Called only when the result is a successful ObjectResult with a non-null value.
    /// </summary>
    /// <param name="context">The result executing context</param>
    /// <param name="resultFromAction">The ObjectResult from the action</param>
    /// <param name="next">The delegate to call to continue the pipeline</param>
    protected abstract Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next);
}
