using Hmm.ServiceApi.Configuration;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Hmm.ServiceApi.Filters;

public class ApiSunsetHeaderFilter : IActionFilter
{
    private readonly ApiDeprecationOptions _options;

    public ApiSunsetHeaderFilter(IOptions<ApiDeprecationOptions> options)
    {
        _options = options.Value;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (_options.SunsetSchedule.Count == 0)
        {
            return;
        }

        if (!context.RouteData.Values.TryGetValue("version", out var versionObj) ||
            versionObj is not string version)
        {
            return;
        }

        if (!_options.SunsetSchedule.TryGetValue(version, out var sunsetDate))
        {
            return;
        }

        context.HttpContext.Response.Headers["Sunset"] = sunsetDate.ToString("R");

        if (!string.IsNullOrEmpty(_options.DeprecationDocUrl))
        {
            context.HttpContext.Response.Headers["Link"] = $"<{_options.DeprecationDocUrl}>; rel=\"sunset\"";
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
