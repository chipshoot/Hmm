using Hmm.Utility.Services;
using Microsoft.Extensions.Options;

namespace Hmm.ServiceApi.Core.Tests
{
    public class AiEngineSelectorTests
    {
        private static AiEngineSelector Selector(string @default = "claude")
        {
            var opts = new AiEngineOptions
            {
                Default = @default,
                Routes = new() { ["personal"] = "local" },
                Engines =
                {
                    new AiEngineDescriptor { Name = "claude", Provider = AiProvider.Anthropic },
                    new AiEngineDescriptor { Name = "local", Provider = AiProvider.SelfHosted },
                }
            };
            return new AiEngineSelector(Options.Create(opts));
        }

        [Fact]
        public void ExplicitEngine_WinsOverEverything()
        {
            var r = Selector().Resolve("local", "personal");
            Assert.True(r.Success);
            Assert.Equal("local", r.Value.Name);
        }

        [Fact]
        public void Purpose_RoutesWhenNoExplicitEngine()
        {
            var r = Selector().Resolve(null, "personal");
            Assert.True(r.Success);
            Assert.Equal("local", r.Value.Name);
        }

        [Fact]
        public void FallsBackToDefault()
        {
            var r = Selector().Resolve(null, null);
            Assert.True(r.Success);
            Assert.Equal("claude", r.Value.Name);
        }

        [Fact]
        public void UnknownPurpose_FallsBackToDefault()
        {
            var r = Selector().Resolve(null, "nope");
            Assert.True(r.Success);
            Assert.Equal("claude", r.Value.Name);
        }

        [Fact]
        public void UnknownExplicitEngine_ReturnsInvalid()
        {
            var r = Selector().Resolve("bogus", null);
            Assert.False(r.Success);
        }

        [Fact]
        public void MissingDefault_ReturnsFail()
        {
            var r = Selector(@default: "").Resolve(null, null);
            Assert.False(r.Success);
        }
    }
}
