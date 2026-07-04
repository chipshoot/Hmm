using Hmm.Utility.Services;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class ReceiptExtractionProviderRegistryTests
    {
        private static IReceiptExtractionProvider Provider(AiProvider p)
        {
            var mock = new Mock<IReceiptExtractionProvider>();
            mock.SetupGet(x => x.Provider).Returns(p);
            return mock.Object;
        }

        [Fact]
        public void Get_ReturnsMatchingProvider()
        {
            var registry = new ReceiptExtractionProviderRegistry(
                new[] { Provider(AiProvider.Anthropic) });

            var r = registry.Get(AiProvider.Anthropic);

            Assert.True(r.Success);
            Assert.Equal(AiProvider.Anthropic, r.Value.Provider);
        }

        [Fact]
        public void Get_UnknownProvider_ReturnsFail()
        {
            var registry = new ReceiptExtractionProviderRegistry(
                new[] { Provider(AiProvider.Anthropic) });

            var r = registry.Get(AiProvider.SelfHosted);

            Assert.False(r.Success);
        }
    }
}
