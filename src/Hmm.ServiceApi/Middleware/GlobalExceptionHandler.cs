using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An unhandled exception has occurred.");

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var response = _env.IsDevelopment()
                ? new
                {
                    StatusCode = httpContext.Response.StatusCode,
                    Message = "Internal Server Error. See exception details.",
                    Details = exception.ToString()
                }
                : new
                {
                    StatusCode = httpContext.Response.StatusCode,
                    Message = "An internal server error has occurred."
                };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, options);

            await httpContext.Response.WriteAsync(json, cancellationToken);

            return true;
        }

        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}