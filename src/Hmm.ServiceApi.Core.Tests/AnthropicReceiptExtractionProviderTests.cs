using Hmm.Utility.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace Hmm.ServiceApi.Core.Tests
{
    public class AnthropicReceiptExtractionProviderTests
    {
        private static readonly byte[] Bytes = { 1, 2, 3 };

        private readonly Mock<ILogger<AnthropicReceiptExtractionProvider>> _logger = new();

        private static AiEngineDescriptor Engine() => new()
        {
            Name = "claude",
            Provider = AiProvider.Anthropic,
            ApiKey = "test-key",
            Model = "claude-haiku-4-5",
            BaseUrl = "https://api.anthropic.com",
            SupportsVision = true,
            MaxTokens = 2048
        };

        private AnthropicReceiptExtractionProvider CreateProvider(MockHttpMessageHandler handler)
            => new(new HttpClient(handler), _logger.Object);

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
            var provider = CreateProvider(new MockHttpMessageHandler(json));

            var result = await provider.ExtractAsync(Engine(), Bytes, "image/jpeg");

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
            var provider = CreateProvider(
                new MockHttpMessageHandler("error", HttpStatusCode.InternalServerError));

            var result = await provider.ExtractAsync(Engine(), Bytes, "image/jpeg");

            Assert.False(result.Success);
            Assert.Contains("unavailable", result.ErrorMessage);
        }

        [Fact]
        public async Task ExtractAsync_WithRefusal_ReturnsFail()
        {
            var provider = CreateProvider(
                new MockHttpMessageHandler("""{"stop_reason":"refusal","content":[]}"""));

            var result = await provider.ExtractAsync(Engine(), Bytes, "image/jpeg");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ExtractAsync_WithNoToolUseBlock_ReturnsFail()
        {
            var provider = CreateProvider(new MockHttpMessageHandler(
                """{"stop_reason":"end_turn","content":[{"type":"text","text":"sorry"}]}"""));

            var result = await provider.ExtractAsync(Engine(), Bytes, "image/jpeg");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ExtractAsync_WithoutApiKey_ReturnsFail()
        {
            var engine = Engine();
            engine.ApiKey = "";
            var provider = CreateProvider(new MockHttpMessageHandler("{}"));

            var result = await provider.ExtractAsync(engine, Bytes, "image/jpeg");

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ExtractAsync_WithEmptyBytes_ReturnsInvalid()
        {
            var provider = CreateProvider(new MockHttpMessageHandler("{}"));

            var result = await provider.ExtractAsync(Engine(), System.Array.Empty<byte>(), "image/jpeg");

            Assert.False(result.Success);
        }
    }
}
