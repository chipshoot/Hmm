# AI Engine Switch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the backend's AI engine swappable — config default + per-request override — behind a provider abstraction, with Anthropic as the first provider and a clean drop-in point for a future self-hosted engine.

**Architecture:** `IReceiptExtractionProvider` (one impl per provider, stateless, reads a per-engine `AiEngineDescriptor`); `AiEngineOptions` config (named engines + `Default` + purpose `Routes`); `AiEngineSelector` (explicit > route > default); a provider `Registry` keyed by `AiProvider`; and a `ReceiptExtractionService` facade the controller calls. The existing Claude service is refactored into the Anthropic provider; `AnthropicSettings` is removed.

**Tech Stack:** .NET 10, `Hmm.ServiceApi`, `HttpClient`, `System.Text.Json`, xUnit + Moq. `ProcessingResult<T>` in `Hmm.Utility.Misc`.

---

## Reference reading
- `docs/superpowers/specs/2026-07-04-ai-engine-switch-design.md` — the design.
- Existing (all become inputs to this refactor): `src/Hmm.Utility.Services/{ClaudeReceiptExtractionService,IReceiptExtractionService,ReceiptExtractionResult,AnthropicSettings}.cs`, `src/Hmm.ServiceApi/Areas/UtilityService/Controllers/ReceiptExtractionController.cs`, `.../Infrastructure/UtilityServiceStartup.cs`, `src/Hmm.ServiceApi.Core.Tests/{ClaudeReceiptExtractionServiceTests,ReceiptExtractionControllerTests}.cs`, `src/Hmm.ServiceApi/appsettings.json`.

## Conventions
- Nullable reference types are **off** in these projects — use plain reference types (no `?` annotations on strings).
- Run tests filtered + build the test project (compiles the chain): `dotnet test src/Hmm.ServiceApi.Core.Tests/Hmm.ServiceApi.Core.Tests.csproj --filter "FullyQualifiedName~<Name>" > /tmp/t.txt 2>&1; echo EXIT $?`. Then `grep -iE "Passed!|Failed!|error CS" /tmp/t.txt`.
- Commit after each task with the footer `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

## File Structure
**Create (`src/Hmm.Utility.Services/`):** `AiProvider.cs`, `AiEngineDescriptor.cs`, `AiEngineOptions.cs`, `IReceiptExtractionProvider.cs`, `AnthropicReceiptExtractionProvider.cs`, `IAiEngineSelector.cs`, `AiEngineSelector.cs`, `IReceiptExtractionProviderRegistry.cs`, `ReceiptExtractionProviderRegistry.cs`, `ReceiptExtractionService.cs`.
**Modify:** `IReceiptExtractionService.cs` (optional params), `ReceiptExtractionController.cs`, `UtilityServiceStartup.cs`, `appsettings.json`, both `CLAUDE.md`s.
**Delete:** `ClaudeReceiptExtractionService.cs`, `AnthropicSettings.cs` (Task 5/7).
**Create tests:** `AiEngineSelectorTests.cs`, `ReceiptExtractionProviderRegistryTests.cs`, `ReceiptExtractionServiceTests.cs`. **Rename/retarget:** `ClaudeReceiptExtractionServiceTests.cs` → `AnthropicReceiptExtractionProviderTests.cs`.

---

### Task 1: Config/model types + provider interface

**Files:** Create `AiProvider.cs`, `AiEngineDescriptor.cs`, `AiEngineOptions.cs`, `IReceiptExtractionProvider.cs` in `src/Hmm.Utility.Services/`.

- [ ] **Step 1: Write the types** (additive — no test yet; they're plain data/contract).

`AiProvider.cs`:
```csharp
namespace Hmm.Utility.Services
{
    public enum AiProvider
    {
        Anthropic,
        SelfHosted
    }
}
```

`AiEngineDescriptor.cs`:
```csharp
namespace Hmm.Utility.Services
{
    /// <summary>One configured AI engine (a config entry under AiEngines:Engines).</summary>
    public class AiEngineDescriptor
    {
        public string Name { get; set; }
        public AiProvider Provider { get; set; }
        public string Model { get; set; }
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public bool SupportsVision { get; set; } = true;
        public int MaxTokens { get; set; } = 2048;
    }
}
```

`AiEngineOptions.cs`:
```csharp
using System.Collections.Generic;

namespace Hmm.Utility.Services
{
    public class AiEngineOptions
    {
        public const string SectionName = "AiEngines";

        /// <summary>Name of the engine used when no override/route applies.</summary>
        public string Default { get; set; }

        /// <summary>Optional purpose -> engine-name routes (e.g. "personal" -> "local").</summary>
        public Dictionary<string, string> Routes { get; set; } = new();

        public List<AiEngineDescriptor> Engines { get; set; } = new();
    }
}
```

`IReceiptExtractionProvider.cs`:
```csharp
using Hmm.Utility.Misc;
using System.Threading.Tasks;

namespace Hmm.Utility.Services
{
    /// <summary>One implementation per AI provider. Stateless — all per-engine
    /// config (model, endpoint, key) comes from the descriptor.</summary>
    public interface IReceiptExtractionProvider
    {
        AiProvider Provider { get; }

        Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(
            AiEngineDescriptor engine, byte[] bytes, string contentType);
    }
}
```

- [ ] **Step 2: Build** the test project (compiles the chain). Expected: 0 errors.

- [ ] **Step 3: Commit**
```
feat(ai-engine): engine descriptor, options, provider enum + interface
```

---

### Task 2: `AnthropicReceiptExtractionProvider` (refactor the Claude logic)

Copy the whole body of `ClaudeReceiptExtractionService` into a new provider that implements `IReceiptExtractionProvider` and reads config from the descriptor instead of `AnthropicSettings`. Leave the old `ClaudeReceiptExtractionService` in place for now (removed in Task 5).

**Files:** Create `src/Hmm.Utility.Services/AnthropicReceiptExtractionProvider.cs`. Rename test file `ClaudeReceiptExtractionServiceTests.cs` → `AnthropicReceiptExtractionProviderTests.cs`.

- [ ] **Step 1: Write the provider.** Start from the current `ClaudeReceiptExtractionService.cs` and apply exactly these changes:
  - Class/ctor rename to `AnthropicReceiptExtractionProvider`; drop the `IOptions<AnthropicSettings>` ctor param and the `_settings` field. Keep `HttpClient` + `ILogger<AnthropicReceiptExtractionProvider>`.
  - Add `public AiProvider Provider => AiProvider.Anthropic;`
  - Change the method signature to `public async Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(AiEngineDescriptor engine, byte[] bytes, string contentType)`.
  - Replace every `_settings.X` with `engine.X` (`engine.ApiKey`, `engine.BaseUrl`, `engine.Model`, `engine.MaxTokens`).
  - `BuildRequestJson` becomes an instance/static method taking `engine` (for `engine.Model` + `engine.MaxTokens`) — thread `engine` through.
  - Everything else (headers, tool schema with PascalCase `Labour/Part/Fee` enum, `ParseResponse`, `MapInput`, the `Get*` helpers) is unchanged.

- [ ] **Step 2: Retarget the tests.** In the renamed `AnthropicReceiptExtractionProviderTests.cs`: replace the `AnthropicSettings` field + `Options.Create(...)` with an `AiEngineDescriptor`:
```csharp
private static AiEngineDescriptor Engine() => new()
{
    Name = "claude", Provider = AiProvider.Anthropic, ApiKey = "test-key",
    Model = "claude-haiku-4-5", BaseUrl = "https://api.anthropic.com",
    SupportsVision = true, MaxTokens = 2048
};

private AnthropicReceiptExtractionProvider CreateProvider(MockHttpMessageHandler handler) =>
    new(new HttpClient(handler), _logger.Object);
```
Update each call to `service.ExtractAsync(Bytes, "image/jpeg")` → `provider.ExtractAsync(Engine(), Bytes, "image/jpeg")`. For the "without API key" test, build a descriptor with `ApiKey = ""`. The assertions (mapped result, http error → Fail, refusal → Fail, no-tool-use → Fail, empty bytes → Invalid) are unchanged.

- [ ] **Step 3: Run** `--filter "FullyQualifiedName~AnthropicReceiptExtractionProvider"` → all pass.

- [ ] **Step 4: Commit**
```
feat(ai-engine): AnthropicReceiptExtractionProvider (descriptor-driven refactor of the Claude service)
```

---

### Task 3: `AiEngineSelector`

**Files:** Create `IAiEngineSelector.cs`, `AiEngineSelector.cs`. Test: `AiEngineSelectorTests.cs`.

- [ ] **Step 1: Write the failing test**
```csharp
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
```

- [ ] **Step 2: Run — expect FAIL** (types missing).

- [ ] **Step 3: Implement**

`IAiEngineSelector.cs`:
```csharp
using Hmm.Utility.Misc;

namespace Hmm.Utility.Services
{
    public interface IAiEngineSelector
    {
        /// <summary>Resolve which engine to use. Precedence: explicit name >
        /// purpose route > default.</summary>
        ProcessingResult<AiEngineDescriptor> Resolve(string requestedEngine, string purpose);
    }
}
```

`AiEngineSelector.cs`:
```csharp
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
                // A configured route pointing at a missing engine is a config error.
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
```

- [ ] **Step 4: Run — expect PASS.** Commit:
```
feat(ai-engine): AiEngineSelector (explicit > purpose route > default)
```

---

### Task 4: `ReceiptExtractionProviderRegistry`

**Files:** Create `IReceiptExtractionProviderRegistry.cs`, `ReceiptExtractionProviderRegistry.cs`. Test: `ReceiptExtractionProviderRegistryTests.cs`.

- [ ] **Step 1: Write the failing test**
```csharp
using Hmm.Utility.Misc;
using Hmm.Utility.Services;
using Moq;
using System.Threading.Tasks;

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
```

- [ ] **Step 2: Run — expect FAIL.**

- [ ] **Step 3: Implement**

`IReceiptExtractionProviderRegistry.cs`:
```csharp
using Hmm.Utility.Misc;

namespace Hmm.Utility.Services
{
    public interface IReceiptExtractionProviderRegistry
    {
        ProcessingResult<IReceiptExtractionProvider> Get(AiProvider provider);
    }
}
```

`ReceiptExtractionProviderRegistry.cs`:
```csharp
using Hmm.Utility.Misc;
using System.Collections.Generic;
using System.Linq;

namespace Hmm.Utility.Services
{
    public class ReceiptExtractionProviderRegistry : IReceiptExtractionProviderRegistry
    {
        private readonly Dictionary<AiProvider, IReceiptExtractionProvider> _byProvider;

        public ReceiptExtractionProviderRegistry(IEnumerable<IReceiptExtractionProvider> providers)
        {
            _byProvider = providers.ToDictionary(p => p.Provider);
        }

        public ProcessingResult<IReceiptExtractionProvider> Get(AiProvider provider) =>
            _byProvider.TryGetValue(provider, out var impl)
                ? ProcessingResult<IReceiptExtractionProvider>.Ok(impl)
                : ProcessingResult<IReceiptExtractionProvider>.Fail(
                    $"No AI provider registered for '{provider}'.");
    }
}
```

- [ ] **Step 4: Run — expect PASS.** Commit:
```
feat(ai-engine): provider registry keyed by AiProvider
```

---

### Task 5: `ReceiptExtractionService` facade + remove the old service/settings

**Files:** Modify `IReceiptExtractionService.cs`; create `ReceiptExtractionService.cs`; **delete** `ClaudeReceiptExtractionService.cs` and `AnthropicSettings.cs`. Test: `ReceiptExtractionServiceTests.cs`.

- [ ] **Step 1: Extend the interface** (`IReceiptExtractionService.cs`):
```csharp
Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(
    byte[] bytes, string contentType, string engine = null, string purpose = null);
```

- [ ] **Step 2: Write the failing facade test** (`ReceiptExtractionServiceTests.cs`):
```csharp
using Hmm.Utility.Misc;
using Hmm.Utility.Services;
using Moq;
using System.Threading.Tasks;

namespace Hmm.ServiceApi.Core.Tests
{
    public class ReceiptExtractionServiceTests
    {
        private static readonly byte[] Bytes = { 1, 2, 3 };

        private static AiEngineDescriptor Engine(bool vision = true) => new()
        {
            Name = "claude", Provider = AiProvider.Anthropic, SupportsVision = vision
        };

        private static (ReceiptExtractionService, Mock<IReceiptExtractionProvider>) Build(
            ProcessingResult<AiEngineDescriptor> resolved)
        {
            var selector = new Mock<IAiEngineSelector>();
            selector.Setup(s => s.Resolve(It.IsAny<string>(), It.IsAny<string>())).Returns(resolved);

            var provider = new Mock<IReceiptExtractionProvider>();
            provider.SetupGet(p => p.Provider).Returns(AiProvider.Anthropic);
            provider.Setup(p => p.ExtractAsync(It.IsAny<AiEngineDescriptor>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .ReturnsAsync(ProcessingResult<ReceiptExtractionResult>.Ok(new ReceiptExtractionResult { ShopName = "Bob" }));

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
            provider.Verify(p => p.ExtractAsync(It.IsAny<AiEngineDescriptor>(), Bytes, "image/jpeg"), Times.Once);
        }

        [Fact]
        public async Task WhenSelectionFails_ReturnsFailure()
        {
            var (service, _) = Build(ProcessingResult<AiEngineDescriptor>.Invalid("Unknown AI engine 'x'."));
            var r = await service.ExtractAsync(Bytes, "image/jpeg", engine: "x");
            Assert.False(r.Success);
        }

        [Fact]
        public async Task WhenEngineLacksVision_ReturnsFail()
        {
            var (service, provider) = Build(ProcessingResult<AiEngineDescriptor>.Ok(Engine(vision: false)));
            var r = await service.ExtractAsync(Bytes, "image/jpeg");
            Assert.False(r.Success);
            provider.Verify(p => p.ExtractAsync(It.IsAny<AiEngineDescriptor>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
        }
    }
}
```

- [ ] **Step 3: Run — expect FAIL.**

- [ ] **Step 4: Implement the facade** (`ReceiptExtractionService.cs`):
```csharp
using Hmm.Utility.Misc;
using System;
using System.Threading.Tasks;

namespace Hmm.Utility.Services
{
    public class ReceiptExtractionService : IReceiptExtractionService
    {
        private readonly IAiEngineSelector _selector;
        private readonly IReceiptExtractionProviderRegistry _registry;

        public ReceiptExtractionService(
            IAiEngineSelector selector,
            IReceiptExtractionProviderRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(selector);
            ArgumentNullException.ThrowIfNull(registry);
            _selector = selector;
            _registry = registry;
        }

        public async Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(
            byte[] bytes, string contentType, string engine = null, string purpose = null)
        {
            var resolved = _selector.Resolve(engine, purpose);
            if (!resolved.Success)
            {
                return ProcessingResult<ReceiptExtractionResult>.Fail(resolved.ErrorMessage);
            }

            var descriptor = resolved.Value;
            if (!descriptor.SupportsVision)
            {
                return ProcessingResult<ReceiptExtractionResult>.Fail(
                    $"AI engine '{descriptor.Name}' can't read receipts (no vision support).");
            }

            var providerResult = _registry.Get(descriptor.Provider);
            if (!providerResult.Success)
            {
                return ProcessingResult<ReceiptExtractionResult>.Fail(providerResult.ErrorMessage);
            }

            return await providerResult.Value.ExtractAsync(descriptor, bytes, contentType);
        }
    }
}
```

- [ ] **Step 5: Delete** `src/Hmm.Utility.Services/ClaudeReceiptExtractionService.cs` and `src/Hmm.Utility.Services/AnthropicSettings.cs` (`git rm`). The provider logic now lives in `AnthropicReceiptExtractionProvider`; the facade implements `IReceiptExtractionService`.

- [ ] **Step 6: Run** the receipt + selector + registry + facade tests. Expect all pass. (Build will fail until Task 7 updates DI — that's expected here; the test project references the API project. If the build breaks on DI, do Task 7 before running the full build, or temporarily run only the `Hmm.Utility.Services` + unit tests. Prefer doing Tasks 5→7 back-to-back.)

- [ ] **Step 7: Commit** (with Task 7 if the build requires DI first):
```
feat(ai-engine): ReceiptExtractionService facade; remove ClaudeReceiptExtractionService + AnthropicSettings
```

---

### Task 6: Controller — `engine` / `purpose` override

**Files:** Modify `ReceiptExtractionController.cs`. Test: extend `ReceiptExtractionControllerTests.cs`.

- [ ] **Step 1: Write the failing test** — assert the controller passes the query params through:
```csharp
[Fact]
public async Task Extract_PassesEngineAndPurposeToService()
{
    string capturedEngine = null, capturedPurpose = null;
    _service.Setup(s => s.ExtractAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        .Callback<byte[], string, string, string>((b, c, e, p) => { capturedEngine = e; capturedPurpose = p; })
        .ReturnsAsync(ProcessingResult<ReceiptExtractionResult>.Ok(new ReceiptExtractionResult()));

    await _controller.Extract(FakeFile("image/jpeg", 3, new byte[] { 1, 2, 3 }), "local", "personal");

    Assert.Equal("local", capturedEngine);
    Assert.Equal("personal", capturedPurpose);
}
```
(Update the other controller tests' `_service.Setup(...ExtractAsync(...))` and `_controller.Extract(file)` calls to the new 4-arg / 3-arg shapes: `ExtractAsync(bytes, contentType, engine, purpose)` and `Extract(file, engine, purpose)` with nulls.)

- [ ] **Step 2: Run — expect FAIL.**

- [ ] **Step 3: Implement** — change the action signature and the delegate call:
```csharp
public async Task<IActionResult> Extract(
    IFormFile file,
    [FromQuery] string engine = null,
    [FromQuery] string purpose = null)
{
    // ... unchanged validation ...
    var result = await _service.ExtractAsync(bytes, file.ContentType, engine, purpose);
    // ... unchanged result mapping ...
}
```

- [ ] **Step 4: Run — expect PASS.** Commit:
```
feat(ai-engine): receipts endpoint accepts ?engine / ?purpose override
```

---

### Task 7: DI wiring + appsettings

**Files:** Modify `UtilityServiceStartup.cs`, `appsettings.json`.

- [ ] **Step 1: DI** — replace the Anthropic registration block in `ConfigureServices()`:
```csharp
_services.Configure<AiEngineOptions>(
    _configuration.GetSection(AiEngineOptions.SectionName));

// Providers: one typed HttpClient each, also surfaced as IReceiptExtractionProvider.
_services.AddHttpClient<AnthropicReceiptExtractionProvider>();
_services.AddScoped<IReceiptExtractionProvider>(
    sp => sp.GetRequiredService<AnthropicReceiptExtractionProvider>());

_services.AddScoped<IReceiptExtractionProviderRegistry, ReceiptExtractionProviderRegistry>();
_services.AddScoped<IAiEngineSelector, AiEngineSelector>();
_services.AddScoped<IReceiptExtractionService, ReceiptExtractionService>();
```
Remove the old `Configure<AnthropicSettings>` + `AddHttpClient<IReceiptExtractionService, ClaudeReceiptExtractionService>()` lines.

> Note: `AddHttpClient<AnthropicReceiptExtractionProvider>()` registers it transient with an injected `HttpClient`; the `AddScoped<IReceiptExtractionProvider>(sp => sp.GetRequiredService<...>())` line surfaces the same concrete type under the interface so the registry's `IEnumerable<IReceiptExtractionProvider>` sees it. When a second provider is added later, register it the same way (add its `AddHttpClient<T>()` + an `AddScoped<IReceiptExtractionProvider>(sp => …)` line).

- [ ] **Step 2: appsettings.json** — replace the `AnthropicSettings` block with:
```json
"AiEngines": {
  "Default": "claude",
  "Routes": {},
  "Engines": [
    {
      "Name": "claude",
      "Provider": "Anthropic",
      "Model": "claude-haiku-4-5",
      "BaseUrl": "https://api.anthropic.com",
      "SupportsVision": true,
      "MaxTokens": 2048
    }
  ]
},
```
(`ApiKey` omitted — set via env `AiEngines__Engines__0__ApiKey` on the VPS.)

- [ ] **Step 3: Full solution build + all receipt/engine tests.**
```bash
dotnet build Hmm.sln > /tmp/b.txt 2>&1; echo EXIT $?; grep -iE "error|Build succeeded" /tmp/b.txt | head
dotnet test src/Hmm.ServiceApi.Core.Tests/Hmm.ServiceApi.Core.Tests.csproj --filter "FullyQualifiedName~Receipt|FullyQualifiedName~AiEngine|FullyQualifiedName~Anthropic" > /tmp/t.txt 2>&1; echo EXIT $?; grep -iE "Passed!|Failed!" /tmp/t.txt
```
Expected: build 0 errors; all tests pass.

- [ ] **Step 4: Commit** (may be combined with Task 5):
```
feat(ai-engine): DI wiring + AiEngines config (replaces AnthropicSettings)
```

---

### Task 8: Docs

**Files:** `~/projects/hmm/CLAUDE.md`, `~/projects/hmm_console/CLAUDE.md`.

- [ ] **Step 1** Update the backend `CLAUDE.md` `/v1/receipts/extract` note: it now selects an **AI engine** (config `AiEngines`: named engines + `Default` + purpose `Routes`; per-request `?engine=`/`?purpose=`), Anthropic is the only provider today, a self-hosted provider is a drop-in (`IReceiptExtractionProvider`), and the **key env var is now `AiEngines__Engines__0__ApiKey`** (was `AnthropicSettings__ApiKey`).

- [ ] **Step 2** Update the client `CLAUDE.md` receipt_scan row cloud-AI note to mention the backend engine is now swappable (config + `?purpose`/`?engine`), for future self-hosting.

- [ ] **Step 3: Commit**
```
docs: document the swappable AI engine (AiEngines) + key env-var rename
```

---

## Final verification
- [ ] `dotnet build Hmm.sln` → 0 errors.
- [ ] `dotnet test src/Hmm.ServiceApi.Core.Tests` → green (selector, registry, provider, facade, controller).
- [ ] Grep confirms `AnthropicSettings` and `ClaudeReceiptExtractionService` are fully removed: `grep -rn "AnthropicSettings\|ClaudeReceiptExtractionService" src | grep -v obj` → no hits.
- [ ] **Deploy note (manual):** on next `deploy-api.sh --deploy`, the key env var is `AiEngines__Engines__0__ApiKey` — update the systemd drop-in accordingly.

## Self-review notes
- **Spec coverage:** descriptor/options/interface (T1), Anthropic provider refactor (T2), selector (T3), registry (T4), facade + removals (T5), controller override (T6), DI + config (T7), docs + migration (T8). All spec sections map to a task.
- **Backward compatible:** the endpoint works unchanged when `engine`/`purpose` are omitted; the client needs no change.
- **Extension point:** adding a self-hosted provider = new `IReceiptExtractionProvider` impl + one `AddHttpClient`/`AddScoped` pair + a config `Engines` entry — no change to selector, registry, facade, or controller.
- **Type consistency:** `ExtractAsync(bytes, contentType, engine, purpose)` matches across interface, facade, and controller call; provider `ExtractAsync(descriptor, bytes, contentType)` matches interface + registry usage.
