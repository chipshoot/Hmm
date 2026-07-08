using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hmm.Utility.Services
{
    /// <summary>
    /// Receipt extraction via the Anthropic Messages API (Claude vision).
    /// Sends the receipt as an image/PDF content block and forces structured
    /// output via a single tool the model must call; the tool's input is the
    /// extracted <see cref="ReceiptExtractionResult"/>.
    ///
    /// Stateless — all per-engine config (model, endpoint, key, max tokens)
    /// comes from the <see cref="AiEngineDescriptor"/>, so one instance serves
    /// every Anthropic engine. Uses <see cref="HttpClient"/> against the
    /// documented REST API (no SDK), keeping it unit-testable with a fake
    /// handler.
    /// </summary>
    public class AnthropicReceiptExtractionProvider : IReceiptExtractionProvider
    {
        private const string ToolName = "record_service_receipt";
        private const string AnthropicVersion = "2023-06-01";

        private readonly HttpClient _httpClient;
        private readonly ILogger<AnthropicReceiptExtractionProvider> _logger;

        public AnthropicReceiptExtractionProvider(
            HttpClient httpClient,
            ILogger<AnthropicReceiptExtractionProvider> logger)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(logger);

            _httpClient = httpClient;
            _logger = logger;
        }

        public AiProvider Provider => AiProvider.Anthropic;

        public async Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(
            AiEngineDescriptor engine, byte[] bytes, string contentType)
        {
            ArgumentNullException.ThrowIfNull(engine);

            if (bytes == null || bytes.Length == 0)
            {
                return ProcessingResult<ReceiptExtractionResult>.Invalid("A receipt file is required.");
            }
            if (string.IsNullOrWhiteSpace(engine.ApiKey))
            {
                return ProcessingResult<ReceiptExtractionResult>.Fail("Receipt extraction is not configured.");
            }

            try
            {
                var requestJson = BuildRequestJson(engine, bytes, contentType);

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{engine.BaseUrl}/v1/messages");
                request.Headers.Add("x-api-key", engine.ApiKey);
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

                var parsed = ParseResponse(body);
                if (parsed.Success && _logger.IsEnabled(LogLevel.Debug))
                {
                    // Diagnostic (numbers only, no receipt content): confirms
                    // whether varied line-item quantities survive extraction.
                    // Debug-level — silent in prod, flip the level to inspect.
                    var qtys = new StringBuilder();
                    foreach (var li in parsed.Value.LineItems)
                    {
                        if (qtys.Length > 0) qtys.Append(',');
                        qtys.Append(li.Quantity);
                    }
                    _logger.LogDebug(
                        "Receipt extraction OK: {Count} line items, quantities [{Quantities}]",
                        parsed.Value.LineItems.Count, qtys.ToString());
                }
                return parsed;
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

        private static string BuildRequestJson(AiEngineDescriptor engine, byte[] bytes, string contentType)
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
                model = engine.Model,
                max_tokens = engine.MaxTokens,
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
                                    + "yyyy-MM-dd for the date. For every line item, read its quantity from "
                                    + "the receipt's quantity column. That column is not always headed "
                                    + "\"Qty\" — it may be labelled Shp, Ship, or Shipped (or shown as "
                                    + "\"x2\"); use whichever column holds the per-item count. Set unitCost "
                                    + "to the per-unit price, not the line total. Cross-check with the line "
                                    + "amount: quantity multiplied by unitCost should equal the line's "
                                    + "total amount, so when an amount is a whole multiple of the unit "
                                    + "price, use that multiple as the quantity. Use 1 only when no "
                                    + "quantity is shown and the amount equals the unit price. Also set "
                                    + "amount to the line's printed total for that item."
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
                                quantity = new { type = "integer", description = "Per-item count from the quantity column (may be headed Qty, Shp, Ship or Shipped); equals line amount / unit price. 1 if none shown" },
                                unitCost = new { type = nullableNumber, description = "Per-unit price (not the line total)" },
                                amount = new { type = nullableNumber, description = "Line total for this item (unit price x quantity)" }
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
                        UnitCost = GetDouble(item, "unitCost"),
                        Amount = GetDouble(item, "amount")
                    });
                }
            }

            return result;
        }

        private static string GetString(JsonElement o, string name) =>
            o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static int? GetInt(JsonElement o, string name)
        {
            if (!o.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Number)
                return null;
            // Some models emit integer-typed fields as floats ("2.0"), which
            // TryGetInt32 rejects — fall back to rounding the double so a
            // quantity of 2 doesn't silently degrade to the default of 1.
            if (v.TryGetInt32(out var i)) return i;
            return v.TryGetDouble(out var d) ? (int)System.Math.Round(d) : (int?)null;
        }

        private static double? GetDouble(JsonElement o, string name) =>
            o.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)
                ? d
                : (double?)null;
    }
}
