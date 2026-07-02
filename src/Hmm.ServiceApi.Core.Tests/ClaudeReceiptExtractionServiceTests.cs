using Hmm.Utility.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace Hmm.ServiceApi.Core.Tests
{
    public class ClaudeReceiptExtractionServiceTests
    {
        private static readonly byte[] Bytes = { 1, 2, 3 };

        private readonly Mock<ILogger<ClaudeReceiptExtractionService>> _logger = new();

        private static AnthropicSettings Settings() => new()
        {
            ApiKey = "test-key",
            Model = "claude-haiku-4-5",
            BaseUrl = "https://api.anthropic.com",
            MaxTokens = 2048
        };

        private ClaudeReceiptExtractionService CreateService(
            MockHttpMessageHandler handler, AnthropicSettings settings = null)
        {
            var client = new HttpClient(handler);
            return new ClaudeReceiptExtractionService(
                client, Options.Create(settings ?? Settings()), _logger.Object);
        }

        [Fact]
        public async Task ExtractAsync_MapsToolUseInputToResult()
        {
            var json = """
            {
              "stop_reason": "tool_use",
              "content": [
                {"type":"text","text":"Here you go"},
                {"type":"tool_use","name":"record_service_receipt","input":{
                  "shopName":"Bob Auto","date":"2026-03-02","odometer":45000,
                  "tax":3.5,"total":53.5,"currency":"CAD",
                  "lineItems":[
                    {"type":"Labour","name":"Oil change","quantity":1,"unitCost":40},
                    {"type":"Part","name":"Filter","quantity":2,"unitCost":10}
                  ]}}
              ]
            }
            """;
            var service = CreateService(new MockHttpMessageHandler(json));

            var result = await service.ExtractAsync(Bytes, "image/jpeg");

            Assert.True(result.Success);
            Assert.Equal("Bob Auto", result.Value.ShopName);
            Assert.Equal(45000, result.Value.Odometer.Value);
            Assert.Equal(3.5, result.Value.Tax.Value);
            Assert.Equal("CAD", result.Value.Currency);
            Assert.Equal(2, result.Value.LineItems.Count);
            Assert.Equal("Labour", result.Value.LineItems[0].Type);
            Assert.Equal("Filter", result.Value.LineItems[1].Name);
            Assert.Equal(2, result.Value.LineItems[1].Quantity);
        }

        [Fact]
        public async Task ExtractAsync_WithHttpError_ReturnsFail()
        {
            var service = CreateService(
                new MockHttpMessageHandler("error", HttpStatusCode.InternalServerError));

            var result = await service.ExtractAsync(Bytes, "image/jpeg");

            Assert.False(result.Success);
            Assert.Contains("unavailable", result.ErrorMessage);
        }

        [Fact]
        public async Task ExtractAsync_WithRefusal_ReturnsFail()
        {
            var service = CreateService(
                new MockHttpMessageHandler("""{"stop_reason":"refusal","content":[]}"""));

            var result = await service.ExtractAsync(Bytes, "image/jpeg");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ExtractAsync_WithNoToolUseBlock_ReturnsFail()
        {
            var service = CreateService(new MockHttpMessageHandler(
                """{"stop_reason":"end_turn","content":[{"type":"text","text":"sorry"}]}"""));

            var result = await service.ExtractAsync(Bytes, "image/jpeg");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ExtractAsync_WithoutApiKey_ReturnsFail()
        {
            var settings = Settings();
            settings.ApiKey = "";
            var service = CreateService(new MockHttpMessageHandler("{}"), settings);

            var result = await service.ExtractAsync(Bytes, "image/jpeg");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ExtractAsync_WithEmptyBytes_ReturnsInvalid()
        {
            var service = CreateService(new MockHttpMessageHandler("{}"));

            var result = await service.ExtractAsync(System.Array.Empty<byte>(), "image/jpeg");

            Assert.False(result.Success);
        }
    }
}
