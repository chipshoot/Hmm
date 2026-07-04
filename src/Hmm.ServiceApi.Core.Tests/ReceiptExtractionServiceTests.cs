using Hmm.Utility.Misc;
using Hmm.Utility.Services;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class ReceiptExtractionServiceTests
    {
        private static readonly byte[] Bytes = { 1, 2, 3 };

        private static AiEngineDescriptor Engine(bool vision = true) => new()
        {
            Name = "claude",
            Provider = AiProvider.Anthropic,
            SupportsVision = vision
        };

        private static (ReceiptExtractionService, Mock<IReceiptExtractionProvider>) Build(
            ProcessingResult<AiEngineDescriptor> resolved)
        {
            var selector = new Mock<IAiEngineSelector>();
            selector.Setup(s => s.Resolve(It.IsAny<string>(), It.IsAny<string>())).Returns(resolved);

            var provider = new Mock<IReceiptExtractionProvider>();
            provider.SetupGet(p => p.Provider).Returns(AiProvider.Anthropic);
            provider
                .Setup(p => p.ExtractAsync(It.IsAny<AiEngineDescriptor>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(ProcessingResult<ReceiptExtractionResult>.Ok(
                    new ReceiptExtractionResult { ShopName = "Bob" }));

            var registry = new Mock<IReceiptExtractionProviderRegistry>();
            registry.Setup(r => r.Get(AiProvider.Anthropic))
                .Returns(ProcessingResult<IReceiptExtractionProvider>.Ok(provider.Object));

            return (new ReceiptExtractionService(selector.Object, registry.Object), provider);
        }

        [Fact]
        public async Task Delegates_ToResolvedProvider()
        {
            var (service, provider) = Build(ProcessingResult<AiEngineDescriptor>.Ok(Engine()));

            var r = await service.ExtractAsync(Bytes, "image/jpeg");

            Assert.True(r.Success);
            Assert.Equal("Bob", r.Value.ShopName);
            provider.Verify(
                p => p.ExtractAsync(It.IsAny<AiEngineDescriptor>(), Bytes, "image/jpeg"), Times.Once);
        }

        [Fact]
        public async Task WhenSelectionFails_ReturnsFailure()
        {
            var (service, _) = Build(
                ProcessingResult<AiEngineDescriptor>.Invalid("Unknown AI engine 'x'."));

            var r = await service.ExtractAsync(Bytes, "image/jpeg", engine: "x");

            Assert.False(r.Success);
        }

        [Fact]
        public async Task WhenEngineLacksVision_ReturnsFail()
        {
            var (service, provider) = Build(
                ProcessingResult<AiEngineDescriptor>.Ok(Engine(vision: false)));

            var r = await service.ExtractAsync(Bytes, "image/jpeg");

            Assert.False(r.Success);
            provider.Verify(
                p => p.ExtractAsync(It.IsAny<AiEngineDescriptor>(), It.IsAny<byte[]>(), It.IsAny<string>()),
                Times.Never);
        }
    }
}
