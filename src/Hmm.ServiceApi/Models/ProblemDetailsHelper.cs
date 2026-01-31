using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hmm.ServiceApi.Models
{
    /// <summary>
    /// Helper class for creating consistent RFC 7807 ProblemDetails responses.
    /// </summary>
    public static class ProblemDetailsHelper
    {
        private const string BadRequestType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
        private const string NotFoundType = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
        private const string ConflictType = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
        private const string InternalServerErrorType = "https://tools.ietf.org/html/rfc7231#section-6.6.1";

        /// <summary>
        /// Creates a 400 Bad Request ProblemDetails response.
        /// </summary>
        public static ProblemDetails BadRequest(string detail, HttpContext httpContext)
        {
            return CreateProblemDetails(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                BadRequestType,
                detail,
                httpContext);
        }

        /// <summary>
        /// Creates a 404 Not Found ProblemDetails response.
        /// </summary>
        public static ProblemDetails NotFound(string detail, HttpContext httpContext)
        {
            return CreateProblemDetails(
                StatusCodes.Status404NotFound,
                "Not Found",
                NotFoundType,
                detail,
                httpContext);
        }

        /// <summary>
        /// Creates a 409 Conflict ProblemDetails response.
        /// </summary>
        public static ProblemDetails Conflict(string detail, HttpContext httpContext)
        {
            return CreateProblemDetails(
                StatusCodes.Status409Conflict,
                "Conflict",
                ConflictType,
                detail,
                httpContext);
        }

        /// <summary>
        /// Creates a 500 Internal Server Error ProblemDetails response.
        /// </summary>
        public static ProblemDetails InternalServerError(string detail, HttpContext httpContext)
        {
            return CreateProblemDetails(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                InternalServerErrorType,
                detail,
                httpContext);
        }

        private static ProblemDetails CreateProblemDetails(
            int statusCode,
            string title,
            string type,
            string detail,
            HttpContext httpContext)
        {
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = httpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

            return problemDetails;
        }
    }
}
