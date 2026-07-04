using Hmm.Utility.Misc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Hmm.Utility.Services
{
    public class AiEngineSelector : IAiEngineSelector
    {
        private readonly AiEngineOptions _options;

        public AiEngineSelector(IOptions<AiEngineOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            _options = options.Value;
        }

        public ProcessingResult<AiEngineDescriptor> Resolve(string requestedEngine, string purpose)
        {
            // 1. Explicit override by name.
            if (!string.IsNullOrWhiteSpace(requestedEngine))
            {
                var explicitEngine = Find(requestedEngine);
                return explicitEngine != null
                    ? ProcessingResult<AiEngineDescriptor>.Ok(explicitEngine)
                    : ProcessingResult<AiEngineDescriptor>.Invalid($"Unknown AI engine '{requestedEngine}'.");
            }

            // 2. Purpose route.
            if (!string.IsNullOrWhiteSpace(purpose) &&
                _options.Routes != null &&
                _options.Routes.TryGetValue(purpose, out var routed))
            {
                var routedEngine = Find(routed);
                if (routedEngine != null)
                {
                    return ProcessingResult<AiEngineDescriptor>.Ok(routedEngine);
                }
                // A route pointing at a missing engine is a config error.
                return ProcessingResult<AiEngineDescriptor>.Fail(
                    $"AI engine route '{purpose}' -> '{routed}' has no matching engine.");
            }

            // 3. Default.
            var defaultEngine = string.IsNullOrWhiteSpace(_options.Default) ? null : Find(_options.Default);
            return defaultEngine != null
                ? ProcessingResult<AiEngineDescriptor>.Ok(defaultEngine)
                : ProcessingResult<AiEngineDescriptor>.Fail("No default AI engine is configured.");
        }

        private AiEngineDescriptor Find(string name) =>
            _options.Engines?.FirstOrDefault(e =>
                string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
