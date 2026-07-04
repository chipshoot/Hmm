# AI Engine Switch — Design

Owner: .NET `Hmm.ServiceApi` (backend)
Status: **Draft / pre-implementation**
Date: 2026-07-04
Related: `docs/superpowers/specs/2026-07-01-receipt-scan-autofill-design.md`
(hmm_console) — the receipt-scan feature whose extraction call this
abstracts.

## Why

The backend has exactly one AI consumer today —
`ClaudeReceiptExtractionService`, hardwired to Anthropic/Claude. We want
to **switch the AI engine** — by configuration and dynamically per
request — so we can, for example, route personal-information requests to
a **self-hosted** model in the future while everything else uses the
cloud default. This introduces a small, provider-agnostic abstraction
with config-driven selection; adding a self-hosted (or any other)
provider later is a drop-in class plus a config entry.

## Scope

**In scope:**
- A provider-agnostic **engine abstraction** for receipt extraction, with
  the **Anthropic provider** as the first (only) implementation —
  refactored from the existing Claude service.
- **Config-driven selection**: named engines, a default, optional
  purpose→engine routes, and an optional **per-request override**.
- The selection infrastructure (options + selector + registry) is
  feature-agnostic, so future AI features reuse it.

**Out of scope (deliberately):**
- Implementing a second provider now (self-hosted / OpenAI-compatible).
  The design leaves a **drop-in extension point**; the provider lands
  when a model is available. (Note: receipt extraction needs *vision* — a
  self-hosted engine must be vision-capable to serve receipts; the
  `SupportsVision` flag gates this.)
- A universal LLM/chat abstraction (`IChatEngine.Complete(...)`). YAGNI
  with one consumer; Approach 1 can grow into it later.
- Any client (`hmm_console`) change. The endpoint stays backward
  compatible; the client may start sending `?purpose=…` later.

## Architecture (Approach 1: operation-specific engine + config selection)

Separate *how to talk to a provider* (stateless impl) from *which engine
is configured* (a descriptor).

### Provider interface + descriptor

```csharp
enum AiProvider { Anthropic, SelfHosted }   // grows as providers are added

// One configured engine (a config entry).
class AiEngineDescriptor {
  string Name;                    // "claude", "local", …
  AiProvider Provider;
  string Model;
  string BaseUrl;
  string ApiKey;                  // from env; empty for a keyless local endpoint
  bool   SupportsVision = true;   // gates receipt (image/PDF) use
  int    MaxTokens = 2048;
}

// One implementation per provider — stateless; per-call config from the descriptor.
interface IReceiptExtractionProvider {
  AiProvider Provider { get; }
  Task<ProcessingResult<ReceiptExtractionResult>> ExtractAsync(
      AiEngineDescriptor engine, byte[] bytes, string contentType);
}
```

Today: `AnthropicReceiptExtractionProvider` (`Provider => Anthropic`).
Later: a `SelfHostedReceiptExtractionProvider` (`Provider => SelfHosted`)
— no change to the interface, selector, or facade.

### Config (`AiEngines` section)

```csharp
class AiEngineOptions {
  const string SectionName = "AiEngines";
  string Default;                              // engine name
  Dictionary<string,string> Routes;            // purpose -> engine name
  List<AiEngineDescriptor> Engines;
}
```
```json
"AiEngines": {
  "Default": "claude",
  "Routes": { "personal": "local" },
  "Engines": [
    { "Name": "claude", "Provider": "Anthropic", "Model": "claude-haiku-4-5",
      "BaseUrl": "https://api.anthropic.com", "SupportsVision": true, "MaxTokens": 2048 }
  ]
}
```
API keys stay in env (`AiEngines__Engines__0__ApiKey=…`), never committed.
Adding self-hosting later = one new `Engines` entry (`"local"`,
`Provider: SelfHosted`, a `BaseUrl`) — no code change to switch to it.

### Selector — precedence

```csharp
interface IAiEngineSelector {
  ProcessingResult<AiEngineDescriptor> Resolve(string requestedEngine, string purpose);
}
```
Highest first: (1) explicit `requestedEngine` name → (2) `Routes[purpose]`
→ (3) `Default`. Resolves to a descriptor, or `Invalid` (unknown engine
name) / `Fail` (missing/misconfigured default).

### Facade + registry

```csharp
interface IReceiptExtractionProviderRegistry {
  ProcessingResult<IReceiptExtractionProvider> Get(AiProvider provider);
}

// ReceiptExtractionService : IReceiptExtractionService  (the facade the controller calls)
ExtractAsync(bytes, contentType, engine = null, purpose = null) {
  var d = _selector.Resolve(engine, purpose);
  if (!d.Success) return d.Fail;
  if (!d.Value.SupportsVision) return Fail("Engine '{name}' can't read receipts (no vision).");
  var p = _registry.Get(d.Value.Provider);
  if (!p.Success) return p.Fail;
  return await p.Value.ExtractAsync(d.Value, bytes, contentType);
}
```
`IReceiptExtractionService` gains two **optional** params (`engine`,
`purpose`) — backward compatible.

### Controller

`POST /v1/receipts/extract` gains optional `[FromQuery] string engine`
and `[FromQuery] string purpose`, passed to the facade. Omitting both =
today's default behavior. The `415/413/400` validation is unchanged.

### DI (`UtilityServiceStartup`)

`Configure<AiEngineOptions>`; each provider registered as a typed
`HttpClient` **and** surfaced as `IReceiptExtractionProvider` (registry
gets them all via `IEnumerable<IReceiptExtractionProvider>`, keyed by
`Provider`); scoped `IAiEngineSelector`, registry, and facade. One
`HttpClient` per provider type suffices — `BaseUrl` comes from the
descriptor per call (absolute URL), as the Anthropic code already does.

## Refactor & migration

- `ClaudeReceiptExtractionService` → `AnthropicReceiptExtractionProvider :
  IReceiptExtractionProvider`; reads `descriptor.Model/BaseUrl/ApiKey/
  MaxTokens` instead of `AnthropicSettings`. Tool-use + HttpClient + JSON
  logic unchanged.
- `AnthropicSettings` removed; its values become the `claude` engine in
  `AiEngines`.
- `ReceiptExtractionService` becomes the new facade; the controller keeps
  calling `IReceiptExtractionService`.

**Migration note (real, but free right now):** the API-key env var changes
`AnthropicSettings__ApiKey` → `AiEngines__Engines__0__ApiKey`. The key was
**never set on the VPS**, so there is nothing to migrate — set the new
name during the key step. `appsettings.json` swaps the `AnthropicSettings`
block for the `AiEngines` block; both CLAUDE.md notes are updated.

## Error handling

- Unknown requested engine → `Invalid` → controller `400` with the name.
- Missing/misconfigured default → `Fail` → `400` ("AI engine not
  configured").
- Routed/selected engine lacks vision → `Fail` with a clear message
  (don't send a receipt to a text-only engine).
- No provider registered for an engine's `Provider` → `Fail`.
- Provider/API failures propagate as today (`Fail`, refusal → `Fail`).

## Testing

- **`AiEngineSelector`**: explicit > route > default; unknown engine →
  `Invalid`; unknown purpose → default; missing default → `Fail`.
- **Registry**: right provider by enum; unknown → `Fail`.
- **`AnthropicReceiptExtractionProvider`**: existing fake-
  `HttpMessageHandler` tests, now driven by a descriptor.
- **Facade `ReceiptExtractionService`**: mocked selector + registry →
  picks engine, guards vision (non-vision engine → `Fail`), delegates,
  propagates provider result, unknown provider → `Fail`.
- **Controller**: passes `engine`/`purpose` through (mocked service
  verifies); validation unchanged.

## Implementation order

1. Config/model types + provider interface.
2. `AnthropicReceiptExtractionProvider` (refactor the Claude logic,
   descriptor-driven) + its tests.
3. `AiEngineSelector` + tests.
4. `ReceiptExtractionProviderRegistry` + tests.
5. `ReceiptExtractionService` facade + `IReceiptExtractionService`
   optional params; remove the old `ClaudeReceiptExtractionService` +
   `AnthropicSettings` + facade tests.
6. Controller `engine`/`purpose` params + test.
7. DI wiring + `appsettings` `AiEngines` block.
8. Docs (both CLAUDE.md notes + the systemd key-env instruction).
