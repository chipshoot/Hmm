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
            switch (statusCode)
            {
                case 404:
                    return "Resource not found";

                case 500:
                    return "a not handled error occurred";

                default:
                    return null;
            }
        }
    }
}