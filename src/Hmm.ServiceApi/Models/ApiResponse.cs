using System.Text.Json.Serialization;

namespace Hmm.ServiceApi.Models
{
    public class ApiResponse
    {
        public int StatusCode { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Message { get; }

        public ApiResponse(int statusCode, string message = null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        }

        private static string GetDefaultMessageForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad request date",
                404 => "Resource not found",
                500 => "a not handled error occurred",
                _ => null
            };
        }
    }
}