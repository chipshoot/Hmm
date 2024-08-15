using System.Text.Json.Serialization;

namespace Hmm.ServiceApi.Models
{
    public class ApiResponse(int statusCode, string message = null)
    {
        public int StatusCode { get; } = statusCode;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Message { get; } = message ?? GetDefaultMessageForStatusCode(statusCode);

        private static string GetDefaultMessageForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad request data",
                404 => "Resource not found",
                500 => "a not handled error occurred",
                _ => null
            };
        }
    }
}