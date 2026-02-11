using Hmm.ServiceApi.Configuration;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;

namespace Hmm.ServiceApi.Core.Tests.Filters;

public class ApiSunsetHeaderFilterTests
{
    [Fact]
    public void OnActionExecuting_VersionNotInSunsetSchedule_NoHeadersAdded()
    {
        // Arrange
        var options = CreateOptions(new ApiDeprecationOptions
        {
            SunsetSchedule = new Dictionary<string, DateTimeOffset>
            {
                ["2.0"] = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero)
            }
        });
        var filter = new ApiSunsetHeaderFilter(options);
        var context = CreateActionExecutingContext(version: "1.0");

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.False(context.HttpContext.Response.Headers.ContainsKey("Sunset"));
        Assert.False(context.HttpContext.Response.Headers.ContainsKey("Link"));
    }

    [Fact]
    public void OnActionExecuting_VersionInSunsetSchedule_AddsSunsetHeader()
    {
        // Arrange
        var sunsetDate = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero);
        var options = CreateOptions(new ApiDeprecationOptions
        {
            SunsetSchedule = new Dictionary<string, DateTimeOffset>
            {
                ["1.0"] = sunsetDate
            }
        });
        var filter = new ApiSunsetHeaderFilter(options);
        var context = CreateActionExecutingContext(version: "1.0");

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Equal(sunsetDate.ToString("R"), context.HttpContext.Response.Headers["Sunset"].ToString());
    }

    [Fact]
    public void OnActionExecuting_WithDeprecationDocUrl_AddsLinkHeader()
    {
        // Arrange
        var options = CreateOptions(new ApiDeprecationOptions
        {
            SunsetSchedule = new Dictionary<string, DateTimeOffset>
            {
                ["1.0"] = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero)
            },
            DeprecationDocUrl = "https://docs.example.com/migration"
        });
        var filter = new ApiSunsetHeaderFilter(options);
        var context = CreateActionExecutingContext(version: "1.0");

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.Equal(
            "<https://docs.example.com/migration>; rel=\"sunset\"",
            context.HttpContext.Response.Headers["Link"].ToString());
    }

    [Fact]
    public void OnActionExecuting_WithoutDeprecationDocUrl_NoLinkHeader()
    {
        // Arrange
        var options = CreateOptions(new ApiDeprecationOptions
        {
            SunsetSchedule = new Dictionary<string, DateTimeOffset>
            {
                ["1.0"] = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero)
            }
        });
        var filter = new ApiSunsetHeaderFilter(options);
        var context = CreateActionExecutingContext(version: "1.0");

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.True(context.HttpContext.Response.Headers.ContainsKey("Sunset"));
        Assert.False(context.HttpContext.Response.Headers.ContainsKey("Link"));
    }

    [Fact]
    public void OnActionExecuting_NoVersionInRouteData_NoHeadersAdded()
    {
        // Arrange
        var options = CreateOptions(new ApiDeprecationOptions
        {
            SunsetSchedule = new Dictionary<string, DateTimeOffset>
            {
                ["1.0"] = new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero)
            }
        });
        var filter = new ApiSunsetHeaderFilter(options);
        var context = CreateActionExecutingContext(version: null);

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.False(context.HttpContext.Response.Headers.ContainsKey("Sunset"));
        Assert.False(context.HttpContext.Response.Headers.ContainsKey("Link"));
    }

    [Fact]
    public void OnActionExecuting_EmptySunsetSchedule_NoHeadersAdded()
    {
        // Arrange
        var options = CreateOptions(new ApiDeprecationOptions());
        var filter = new ApiSunsetHeaderFilter(options);
        var context = CreateActionExecutingContext(version: "1.0");

        // Act
        filter.OnActionExecuting(context);

        // Assert
        Assert.False(context.HttpContext.Response.Headers.ContainsKey("Sunset"));
        Assert.False(context.HttpContext.Response.Headers.ContainsKey("Link"));
    }

    private static IOptions<ApiDeprecationOptions> CreateOptions(ApiDeprecationOptions value)
    {
        var mock = new Mock<IOptions<ApiDeprecationOptions>>();
        mock.Setup(o => o.Value).Returns(value);
        return mock.Object;
    }

    private static ActionExecutingContext CreateActionExecutingContext(string version)
    {
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        if (version != null)
        {
            routeData.Values["version"] = version;
        }

        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object>(),
            new object());
    }
}
