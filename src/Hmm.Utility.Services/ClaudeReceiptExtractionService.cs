using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Utility.Services
{
    /// <summary>
    /// Receipt extraction backed by the Anthropic Messages API (Claude vision).
    /// Sends the receipt as an image/PDF content block and forces structured
    /// output via a single tool the model must call; the tool's input is the
    /// extracted <see cref="ReceiptExtractionResult"/>.
    ///
    /// Uses <see cref="HttpClient"/> against the documented REST API (mirroring
    /// <c>NominatimGeocodingService</c>) rather than an SDK — the wire format is
    /// stable and this keeps the service unit-testable with a fake handler.
    /// </summary>
    public class ClaudeReceiptExtractionService : IReceiptExtractionService
    {
        private const string ToolName = "record_service_receipt";
        private const string AnthropicVersion = "2023-06-01";

        private readonly HttpClient _httpClient;
        private readonly AnthropicSettings _settings;
        private readonly ILogger<ClaudeReceiptExtractionService> _logger;

        public ClaudeReceiptExtractionService(
            HttpClient httpClient,
            IOptions<AnthropicSettings> settings,
            ILogger<ClaudeReceiptExtractionService> logger)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(logger);

            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(byte[] bytes, string contentType)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return ProcessingResult<ReceiptExtractionResult>.Invalid("A receipt file is required.");
            }
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                return ProcessingResult<ReceiptExtractionResult>.Fail("Receipt extraction is not configured.");
            }

            try
            {
                var requestJson = BuildRequestJson(bytes, contentType);

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/messages");
                request.Headers.Add("x-api-key", _settings.ApiKey);
                request.Headers.Add("anthropic-version", AnthropicVersion);
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Receipt extraction API returned {Status}: {Body}",
                        (int)response.StatusCode, body);
                    return ProcessingResult<ReceiptExtractionResult>.Fail(
                        "Receipt extraction service is unavailable.");
                }

                return ParseResponse(body);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Receipt extraction request failed");
                return ProcessingResult<ReceiptExtractionResult>.Fail(
                    $"Receipt extraction service unavailable: {ex.Message}");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse receipt extraction response");
                return ProcessingResult<ReceiptExtractionResult>.Fail(
                    "Failed to parse the receipt extraction response.");
            }
        }

        private string BuildRequestJson(byte[] bytes, string contentType)
        {
            var base64 = Convert.ToBase64String(bytes);
            var isPdf = string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);

            object mediaBlock = isPdf
                ? new
                {
                    type = "document",
                    source = new { type = "base64", media_type = "application/pdf", data = base64 }
                }
                : new
                {
                    type = "image",
                    source = new { type = "base64", media_type = contentType, data = base64 }
                };

            var payload = new
            {
                model = _settings.Model,
                max_tokens = _settings.MaxTokens,
                tools = new object[]
                {
                    new
                    {
                        name = ToolName,
                        description = "Record the structured data extracted from a vehicle service receipt.",
                        input_schema = BuildSchema()
                    }
                },
                tool_choice = new { type = "tool", name = ToolName },
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            mediaBlock,
                            new
                            {
                                type = "text",
                                text = "Extract this vehicle service receipt using the tool. Fill only "
                                    + "fields present on the receipt; leave anything absent null. Classify "
                                    + "each line item's type as \"Labour\", \"Part\", or \"Fee\". Use ISO "
                                    + "yyyy-MM-dd for the date."
                            }
                        }
                    }
                }
            };

            return JsonSerializer.Serialize(payload);
        }

        private static object BuildSchema()
        {
            string[] nullableString = { "string", "null" };
            string[] nullableNumber = { "number", "null" };
            string[] nullableInteger = { "integer", "null" };

            return new
            {
                type = "object",
                properties = new
                {
                    shopName = new { type = nullableString },
                    date = new { type = nullableString, description = "ISO date yyyy-MM-dd" },
                    odometer = new { type = nullableInteger, description = "Odometer / mileage reading" },
                    tax = new { type = nullableNumber },
                    total = new { type = nullableNumber },
                    currency = new { type = nullableString, description = "ISO 4217 code, e.g. CAD" },
                    lineItems = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                type = new { type = "string", @enum = new[] { "Labour", "Part", "Fee" } },
                                name = new { type = "string" },
                                quantity = new { type = "integer" },
                                unitCost = new { type = nullableNumber }
                            },
                            required = new[] { "type", "name", "quantity" }
                        }
                    }
                },
                required = new[] { "lineItems" }
            };
        }

        private static ProcessingResult<ReceiptExtractionResult> ParseResponse(string body)
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("stop_reason", out var stop) &&
                stop.ValueKind == JsonValueKind.String &&
                stop.GetString() == "refusal")
            {
                return ProcessingResult<ReceiptExtractionResult>.Fail("The receipt could not be processed.");
            }

            if (root.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in content.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var t) &&
                        t.GetString() == "tool_use" &&
                        block.TryGetProperty("input", out var input))
                    {
                        return ProcessingResult<ReceiptExtractionResult>.Ok(MapInput(input));
                    }
                }
            }

            return ProcessingResult<ReceiptExtractionResult>.Fail("No structured data returned from the receipt.");
        }

        private static ReceiptExtractionResult MapInput(JsonElement input)
        {
            var result = new ReceiptExtractionResult
            {
                ShopName = GetString(input, "shopName"),
                Date = GetString(input, "date"),
                Odometer = GetInt(input, "odometer"),
                Tax = GetDouble(input, "tax"),
                Total = GetDouble(input, "total"),
                Currency = GetString(input, "currency")
            };

            if (input.TryGetProperty("lineItems", out var items) &&
                items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    result.LineItems.Add(new ReceiptExtractionLineItem
                    {
                        Type = GetString(item, "type") ?? "Part",
                        Name = GetString(item, "name") ?? string.Empty,
                        Quantity = GetInt(item, "quantity") ?? 1,
                        UnitCost = GetDouble(item, "unitCost")
                    });
                }
            }

            return result;
        }

        private static string GetString(JsonElement o, string name) =>
            o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static int? GetInt(JsonElement o, string name) =>
            o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i)
                ? i
                : (int?)null;

        private static double? GetDouble(JsonElement o, string name) =>
            o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)
                ? d
                : (double?)null;
    }
}
