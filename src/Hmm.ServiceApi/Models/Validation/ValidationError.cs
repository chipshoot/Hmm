using System.Text.Json.Serialization;

namespace Hmm.ServiceApi.Models.Validation
{
    public class ValidationError
    {
        public ValidationError(string field, string message)
        {
            Field = string.IsNullOrEmpty(field) ? null : field;
            Message = message;
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Field { get; }

        public string Message { get; }
    }
}