using AutoMapper;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Hmm.ServiceApi.Core.Tests.Filters;

public class ResultFilterBaseTests
{
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<LinkGenerator> _mockLinkGenerator;

    public ResultFilterBaseTests()
    {
        _mockMapper = new Mock<IMapper>();
        _mockLinkGenerator = new Mock<LinkGenerator>();
    }

    [Fact]
    public async Task OnResultExecutionAsync_NullResult_CallsNextAndReturns()
    {
        // Arrange
        var filter = new TestResultFilter(_mockMapper.Object, _mockLinkGenerator.Object);
        var emptyResult = new EmptyResult();
        var context = CreateResultExecutingContext(emptyResult);
        // Set Result to null to simulate the null check scenario
        context.Result = null!;
        var nextCalled = false;
        Task<ResultExecutedContext> Next()
        {
            nextCalled = true;
            return Task.FromResult(CreateResultExecutedContext(context, emptyResult));
        }

        // Act
        await filter.OnResultExecutionAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
        Assert.False(filter.TransformResultCalled);
    }

    [Fact]
    public async Task OnResultExecutionAsync_NonObjectResult_CallsNextAndReturns()
    {
        // Arrange
        var filter = new TestResultFilter(_mockMapper.Object, _mockLinkGenerator.Object);
        var context = CreateResultExecutingContext(new ViewResult());
        var nextCalled = false;
        Task<ResultExecutedContext> Next() { nextCalled = true; return Task.FromResult(CreateResultExecutedContext(context)); }

        // Act
        await filter.OnResultExecutionAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
        Assert.False(filter.TransformResultCalled);
    }

    [Fact]
    public async Task OnResultExecutionAsync_ObjectResultWithNullValue_CallsNextAndReturns()
    {
        // Arrange
        var filter = new TestResultFilter(_mockMapper.Object, _mockLinkGenerator.Object);
        var context = CreateResultExecutingContext(new ObjectResult(null) { StatusCode = 200 });
        var nextCalled = false;
        Task<ResultExecutedContext> Next() { nextCalled = true; return Task.FromResult(CreateResultExecutedContext(context)); }

        // Act
        await filter.OnResultExecutionAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
        Assert.False(filter.TransformResultCalled);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(199)]
    [InlineData(300)]
    [InlineData(400)]
    [InlineData(500)]
    public async Task OnResultExecutionAsync_NonSuccessStatusCode_CallsNextAndReturns(int statusCode)
    {
        // Arrange
        var filter = new TestResultFilter(_mockMapper.Object, _mockLinkGenerator.Object);
        var context = CreateResultExecutingContext(new ObjectResult("test") { StatusCode = statusCode });
        var nextCalled = false;
        Task<ResultExecutedContext> Next() { nextCalled = true; return Task.FromResult(CreateResultExecutedContext(context)); }

        // Act
        await filter.OnResultExecutionAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
        Assert.False(filter.TransformResultCalled);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(299)]
    public async Task OnResultExecutionAsync_SuccessStatusCode_CallsTransformResult(int statusCode)
    {
        // Arrange
        var filter = new TestResultFilter(_mockMapper.Object, _mockLinkGenerator.Object);
        var context = CreateResultExecutingContext(new ObjectResult("test") { StatusCode = statusCode });
        var nextCalled = false;
        Task<ResultExecutedContext> Next() { nextCalled = true; return Task.FromResult(CreateResultExecutedContext(context)); }

        // Act
        await filter.OnResultExecutionAsync(context, Next);

        // Assert
        Assert.True(nextCalled);
        Assert.True(filter.TransformResultCalled);
    }

    [Fact]
    public async Task OnResultExecutionAsync_SuccessResult_ProvidesMapperAndLinkGenerator()
    {
        // Arrange
        var filter = new TestResultFilter(_mockMapper.Object, _mockLinkGenerator.Object);
        var context = CreateResultExecutingContext(new ObjectResult("test") { StatusCode = 200 });
        Task<ResultExecutedContext> Next() => Task.FromResult(CreateResultExecutedContext(context));

        // Act
        await filter.OnResultExecutionAsync(context, Next);

        // Assert
        Assert.Same(_mockMapper.Object, filter.ReceivedMapper);
        Assert.Same(_mockLinkGenerator.Object, filter.ReceivedLinkGenerator);
    }

    private static ResultExecutingContext CreateResultExecutingContext(IActionResult result)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            result,
            new object());
    }

    private static ResultExecutedContext CreateResultExecutedContext(ResultExecutingContext context, IActionResult? overrideResult = null)
    {
        return new ResultExecutedContext(
            context,
            context.Filters,
            overrideResult ?? context.Result,
            context.Controller);
    }

    /// <summary>
    /// Test implementation of ResultFilterBase for testing purposes.
    /// </summary>
    private class TestResultFilter : ResultFilterBase
    {
        public bool TransformResultCalled { get; private set; }
        public IMapper ReceivedMapper { get; private set; }
        public LinkGenerator ReceivedLinkGenerator { get; private set; }

        public TestResultFilter(IMapper mapper, LinkGenerator linkGenerator)
            : base(mapper, linkGenerator)
        {
        }

        protected override Task TransformResultAsync(
            ResultExecutingContext context,
            ObjectResult resultFromAction,
            ResultExecutionDelegate next)
        {
            TransformResultCalled = true;
            ReceivedMapper = Mapper;
            ReceivedLinkGenerator = LinkGenerator;
            return next();
        }
    }
}
