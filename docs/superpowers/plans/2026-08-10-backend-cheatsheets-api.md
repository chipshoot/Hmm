# Backend Cheatsheets API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `/v1/cheatsheets` REST API to `Hmm.ServiceApi`, backed by a new typed `Hmm.Cheatsheet` domain module, that stores each cheatsheet card as one `HmmNote` in exactly the JSON shape the Flutter client already persists — losslessly, including rows and fields the backend does not understand.

**Architecture:** A new `Hmm.Cheatsheet` class library mirrors the shape of `Hmm.Automobile` (domain entity + validator + JSON note serializer + manager), but stands alone rather than deriving from `EntityManagerBase<T>` — that base is constrained to `where T : AutomobileBase` and exists to de-duplicate seven automobile entities; a single cheatsheet entity does not need it (YAGNI). Cards live in the existing `Notes` table under a `Hmm.CheatsheetMan.Cheatsheet` note catalog, addressed by note subject `Cheatsheet:{cardId}`. The API layer adds a `CheatsheetService` area with DTOs, an AutoMapper profile, result filters, and a controller.

**Tech Stack:** .NET 10.0, ASP.NET Core, xUnit + Moq, FluentValidation (via `Hmm.Utility.Validation.ValidatorBase<T>`), `System.Text.Json` (note content), `Newtonsoft.Json` (API wire format — the API registers `.AddNewtonsoftJson()`), AutoMapper 15.1.3.

## Global Constraints

- **Tracker row #33.** This work is tracker row #33, previously deferred/on-hold. Mark it 🚀 in-progress when Task 1 starts and ✅ finished when Task 11 passes.
- **No EF migration is required.** Cheatsheets ride the existing `Notes` and `NoteCatalogs` tables via the note-content pattern. No new `DbSet`, no new DAO entity, no change to `HmmDataContext`, therefore no migration and no `HmmDataContextModelSnapshot.cs` change. The only database-side effect is a new **row** in `NoteCatalogs` (data, not schema), created at boot by `CheatsheetAppStartupFilter` (Task 11) — the same mechanism `AutomobileAppStartupFilter.EnsureNoteCatalogsExist` uses. Task 11 includes a verification command proving the model is still clean.
- **Persisted note content shape (must match the client byte-for-byte in structure):**
  `{"note":{"content":{"Cheatsheet":{ "schemaVersion":1, "id":…, "title":…, "walletGroup":…, "tags":[…], "templateId":…, "protected":…, "rows":[…] }}}}`
  Card JSON keys are **camelCase**. Row keys: `label`, `valueAction`, `openSource`, `source`. Source keys: `noteUuid`, `kind`, `locator`.
- **Note subject is identity:** `Cheatsheet:{cardId}`. Never the title. Cards are found by subject, never by decoding content — a card whose JSON is unreadable must still be findable, updatable, and deletable.
- **Note catalog name:** `Hmm.CheatsheetMan.Cheatsheet` (three segments — the client's `CatalogPalette.domainKeyFor` groups on this).
- **`protected` is stored verbatim.** No server-side gating, no encryption, no rejection. Client-side UI concern only.
- **THE CRITICAL INTEROP RULE — losslessness.** The Flutter codec (`lib/features/cheatsheet/data/cheatsheet_codec.dart`) deliberately preserves rows it cannot parse (`unreadableRows`) and re-saves them untouched. The backend MUST do the same, and go further: round-tripping a card through the API (`GET` → `PUT`, or `POST` → `GET`) must preserve **every** byte of semantic content, including
  1. unknown top-level card fields,
  2. unknown row fields,
  3. unknown source fields,
  4. rows that are not JSON objects at all,
  5. known fields carrying an unexpected JSON type.
  A server that validates strictly and drops what it does not understand silently deletes data the client is protecting. Task 5 and Task 9 each own an explicit test for this.
- **Losslessness mechanism (one rule, applied at three levels).** A JSON property is *consumed* into a typed field **only** when it is present with the expected JSON type. Everything else — unknown keys and known-but-mistyped keys — is cloned into an `ExtraFields` dictionary and re-emitted verbatim on write, **after** the typed fields, so extras win on collision. A row that cannot be modelled at all (non-object, or `source` present but not an object) is kept whole in `CheatsheetRow.RawJson` and re-emitted verbatim in its original position.
- **`JsonElement` lifetime:** every preserved `JsonElement` MUST be `.Clone()`d. `JsonDocument` is disposed before the entity escapes the serializer; an un-cloned `JsonElement` becomes invalid and throws on later access.
- **Enum-shaped fields stay strings.** `valueAction` (`call`/`map`/`none`) and `kind` (`field`/`section`/`whole`) are stored as verbatim strings with named constants, not C# enums. Parsing to an enum would map an unknown token (e.g. a future `sms`) onto a default and re-emit the default — silent data loss. Constants live on `CheatsheetConstant`.
- **`ProcessingResult<T>`** is the return type of every manager and serializer operation. Factories: `Ok`, `EmptyOk`, `NotFound`, `Invalid`, `Conflict`, `Fail(msg, ErrorCategory)`, `FromException`. `Value` is get-only; `Success`, `IsNotFound`, `ErrorMessage`, `ErrorType`, `GetWholeMessage()` are the accessors used here.
- **Target framework** `net10.0` for every new project. Test packages must match the versions already used: `Microsoft.NET.Test.Sdk` 18.0.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5, `coverlet.collector` 6.0.4, `Moq` 4.20.72.
- **Commit after every task.** Conventional-commit prefixes (`feat:`, `test:`, `chore:`).

## File Structure

**New project `src/Hmm.Cheatsheet/`** (references `Hmm.Core`, `Hmm.Utility`):
- `CheatsheetConstant.cs` — catalog name, subject prefix, content key, schema version, default wallet group / template id, `valueAction` and `kind` tokens.
- `DomainEntity/CheatsheetCard.cs` — the card; owns `GetNoteSubject`.
- `DomainEntity/CheatsheetRow.cs` — one labelled row; owns `RawJson` (verbatim escape hatch).
- `DomainEntity/CheatsheetSource.cs` — the note reference.
- `ICheatsheetCatalogProvider.cs` / `CheatsheetCatalogProvider.cs` — cached `NoteCatalog` lookup by name (parallel to `Hmm.Automobile.NoteCatalogProvider`, single-catalog).
- `NoteSerialize/CheatsheetJsonNoteSerialize.cs` — `DefaultJsonNoteSerializer<CheatsheetCard>` subclass; the lossless read/write core.
- `Validator/CheatsheetValidator.cs` — `ValidatorBase<CheatsheetCard>`; card-level rules only, deliberately **no** row rules.
- `ICheatsheetManager.cs` / `CheatsheetManager.cs` — CRUD + `walletGroup`/`tag` filtering + in-memory pagination.

**New test project `src/Hmm.Cheatsheet.Tests/`** — one file per unit under test, plus `CheatsheetRoundTripTests.cs` for the interop rule.

**`src/Hmm.ServiceApi.DtoEntity/Cheatsheets/`** (new folder):
- `ApiCheatsheet.cs`, `ApiCheatsheetForCreate.cs`, `ApiCheatsheetForUpdate.cs`, `ApiCheatsheetRow.cs`, `ApiCheatsheetSource.cs`
- `ApiCheatsheetRowConverter.cs` — Newtonsoft converter that emits a raw row verbatim.
- `CheatsheetJsonInterop.cs` — `JsonElement` ⇄ `JToken` dictionary helpers used by the mapping profile.

**`src/Hmm.ServiceApi/Areas/CheatsheetService/`** (new area):
- `Controllers/CheatsheetsController.cs`
- `Filters/CheatsheetResultFilterAttribute.cs`, `Filters/CheatsheetsResultFilterAttribute.cs`
- `Infrastructure/CheatsheetMappingProfile.cs`, `Infrastructure/CheatsheetServiceStartup.cs`, `Infrastructure/CheatsheetAppStartupFilter.cs`

**Modified:** `Hmm.sln`, `src/Hmm.ServiceApi/Hmm.ServiceApi.csproj`, `src/Hmm.ServiceApi/Startup.cs`.

## Design decisions (do not re-litigate during execution)

1. **Route id is the card's string UUID, not the note's int id.** `GET /v1/cheatsheets/{id}` where `{id}` is `CheatsheetCard.Id`. That is the client's cross-device identity and the value embedded in the note subject.
2. **No `CollectionResultFilter` on the list endpoint.** The shared `CollectionResultFilter` (`Areas/HmmNoteService/Filters`) runs `ShapeData`, which reflects every public property into an `ExpandoObject`. That would surface `ExtraFields` as a nested `"ExtraFields": {…}` object instead of inlining it — i.e. it would change the wire shape of preserved data. `CheatsheetsResultFilter` therefore maps the page and writes the `X-Pagination` header itself, leaving the typed objects (and their Newtonsoft converters) intact.
3. **Filtering and pagination happen in memory**, after deserialising every card in the catalog. Card content is opaque JSON in a text column, so `walletGroup`/`tag` cannot be pushed into SQL, and paginating notes *before* deserialisation would page the wrong population. Wallets are tens of cards; correctness wins.
4. **`CheatsheetManager.UpdateAsync` carries the stored note's `Uuid`, `CreateDate`, `NoteDate`, `Version` and `Tags` forward.** `HmmNoteManager.UpdateAsync` mints a fresh `Uuid` whenever the incoming note has none, and the serializer builds a *fresh* `HmmNote` — so without this the card's cross-device identity would be regenerated on every save.

---

### Task 1: Scaffold the `Hmm.Cheatsheet` module and its domain model

**Files:**
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/Hmm.Cheatsheet.csproj`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/CheatsheetConstant.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/DomainEntity/CheatsheetCard.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/DomainEntity/CheatsheetRow.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/DomainEntity/CheatsheetSource.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj`
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetCardTests.cs`
- Modify: `/Users/fchy/Projects/Hmm/Hmm.sln`

**Interfaces:**
- Consumes: nothing.
- Produces: `Hmm.Cheatsheet.CheatsheetConstant` (const strings/ints listed below); `Hmm.Cheatsheet.DomainEntity.CheatsheetCard` with `int NoteId`, `int AuthorId`, `int SchemaVersion`, `string Id`, `string Title`, `string WalletGroup`, `IList<string> Tags`, `string TemplateId`, `bool Protected`, `IList<CheatsheetRow> Rows`, `IDictionary<string, JsonElement> ExtraFields`, `static string GetNoteSubject(string cardId)`; `CheatsheetRow` with `string Label`, `string ValueAction`, `bool OpenSource`, `CheatsheetSource Source`, `IDictionary<string, JsonElement> ExtraFields`, `JsonElement? RawJson`, `bool IsUnreadable`; `CheatsheetSource` with `string NoteUuid`, `string Kind`, `string Locator`, `IDictionary<string, JsonElement> ExtraFields`.

- [ ] **Step 1: Create the two project files and add them to the solution**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/Hmm.Cheatsheet.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Hmm.Core\Hmm.Core.csproj" />
    <ProjectReference Include="..\Hmm.Utility\Hmm.Utility.csproj" />
  </ItemGroup>

</Project>
```

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>

    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.1" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Hmm.Cheatsheet\Hmm.Cheatsheet.csproj" />
    <ProjectReference Include="..\Hmm.Core\Hmm.Core.csproj" />
    <ProjectReference Include="..\Hmm.Utility\Hmm.Utility.csproj" />
  </ItemGroup>

</Project>
```

Then run:

```bash
cd /Users/fchy/Projects/Hmm
dotnet sln Hmm.sln add src/Hmm.Cheatsheet/Hmm.Cheatsheet.csproj
dotnet sln Hmm.sln add src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj
```

Expected: `Project 'src/Hmm.Cheatsheet/Hmm.Cheatsheet.csproj' added to the solution.` and the same for the test project.

- [ ] **Step 2: Write the failing test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetCardTests.cs`:

```csharp
using Hmm.Cheatsheet;
using Hmm.Cheatsheet.DomainEntity;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetCardTests
    {
        [Fact]
        public void GetNoteSubject_PrefixesCardId()
        {
            // Arrange
            const string cardId = "6f1c9a1e-0000-4000-8000-abcdefabcdef";

            // Act
            var subject = CheatsheetCard.GetNoteSubject(cardId);

            // Assert
            Assert.Equal("Cheatsheet:6f1c9a1e-0000-4000-8000-abcdefabcdef", subject);
        }

        [Fact]
        public void NewCard_UsesClientDefaults()
        {
            // Arrange & Act
            var card = new CheatsheetCard();

            // Assert
            Assert.Equal(1, card.SchemaVersion);
            Assert.Equal("Ungrouped", card.WalletGroup);
            Assert.Equal("blank", card.TemplateId);
            Assert.False(card.Protected);
            Assert.Empty(card.Tags);
            Assert.Empty(card.Rows);
            Assert.Empty(card.ExtraFields);
        }

        [Fact]
        public void NewRow_DefaultsToUnboundOpenSourceNoAction()
        {
            // Arrange & Act
            var row = new CheatsheetRow();

            // Assert
            Assert.Equal(string.Empty, row.Label);
            Assert.Equal("none", row.ValueAction);
            Assert.True(row.OpenSource);
            Assert.Null(row.Source);
            Assert.False(row.IsUnreadable);
        }

        [Fact]
        public void NewSource_DefaultsToWholeNoteGranularity()
        {
            // Arrange & Act
            var source = new CheatsheetSource();

            // Assert
            Assert.Equal(string.Empty, source.NoteUuid);
            Assert.Equal("whole", source.Kind);
            Assert.Null(source.Locator);
        }

        [Fact]
        public void Constants_MatchTheClientContract()
        {
            Assert.Equal("Hmm.CheatsheetMan.Cheatsheet", CheatsheetConstant.CheatsheetCatalogName);
            Assert.Equal("Cheatsheet:", CheatsheetConstant.CheatsheetSubjectPrefix);
            Assert.Equal("Cheatsheet", CheatsheetConstant.CheatsheetContentKey);
            Assert.Equal(1, CheatsheetConstant.CurrentSchemaVersion);
        }
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetCardTests"`

Expected: FAIL — build errors `CS0246: The type or namespace name 'CheatsheetCard' could not be found` and `CS0103`/`CS0246` for `CheatsheetConstant`.

- [ ] **Step 4: Write the constants**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/CheatsheetConstant.cs`:

```csharp
namespace Hmm.Cheatsheet
{
    /// <summary>
    /// Wire-contract constants shared with the Flutter client
    /// (lib/features/cheatsheet/data/cheatsheet_codec.dart and
    /// lib/core/data/local/local_cheatsheet_repository.dart). Changing any
    /// value here breaks interoperability with already-persisted cards.
    /// </summary>
    public static class CheatsheetConstant
    {
        /// <summary>
        /// Three-segment name so the client's CatalogPalette.domainKeyFor
        /// groups cheatsheets as their own domain.
        /// </summary>
        public const string CheatsheetCatalogName = "Hmm.CheatsheetMan.Cheatsheet";

        /// <summary>Key the card object sits under inside note.content.</summary>
        public const string CheatsheetContentKey = "Cheatsheet";

        /// <summary>
        /// The note subject is an identity, never a label: "Cheatsheet:{cardId}".
        /// </summary>
        public const string CheatsheetSubjectPrefix = "Cheatsheet:";

        /// <summary>Current persisted card shape. Client: CheatsheetCodec.currentSchemaVersion.</summary>
        public const int CurrentSchemaVersion = 1;

        public const string DefaultWalletGroup = "Ungrouped";

        public const string DefaultTemplateId = "blank";

        // valueAction tokens - stored verbatim, never parsed into an enum.
        public const string ValueActionNone = "none";
        public const string ValueActionCall = "call";
        public const string ValueActionMap = "map";

        // source kind tokens - stored verbatim, never parsed into an enum.
        public const string SourceKindField = "field";
        public const string SourceKindSection = "section";
        public const string SourceKindWhole = "whole";
    }
}
```

- [ ] **Step 5: Write the domain entities**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/DomainEntity/CheatsheetSource.cs`:

```csharp
using System.Collections.Generic;
using System.Text.Json;

namespace Hmm.Cheatsheet.DomainEntity
{
    /// <summary>
    /// A reference to a piece of a note. The referenced note is addressed by
    /// <see cref="NoteUuid"/> - the cross-device-stable identity - never by the
    /// local int note id, which differs per device.
    /// </summary>
    public class CheatsheetSource
    {
        public string NoteUuid { get; set; } = string.Empty;

        /// <summary>
        /// "field" | "section" | "whole" - see <see cref="CheatsheetConstant"/>.
        /// Kept as a verbatim string: parsing to an enum would silently rewrite
        /// an unknown token to a default on the next save.
        /// </summary>
        public string Kind { get; set; } = CheatsheetConstant.SourceKindWhole;

        /// <summary>field -> dotted JSON path; section -> heading text; whole -> null.</summary>
        public string Locator { get; set; }

        /// <summary>
        /// Every source property this version did not consume as a typed field,
        /// cloned verbatim and re-emitted on write.
        /// </summary>
        public IDictionary<string, JsonElement> ExtraFields { get; set; } =
            new Dictionary<string, JsonElement>();
    }
}
```

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/DomainEntity/CheatsheetRow.cs`:

```csharp
using System.Collections.Generic;
using System.Text.Json;

namespace Hmm.Cheatsheet.DomainEntity
{
    /// <summary>
    /// One labelled line of a cheatsheet card. A row may be unbound
    /// (<see cref="Source"/> is null).
    /// </summary>
    public class CheatsheetRow
    {
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// "none" | "call" | "map" - see <see cref="CheatsheetConstant"/>.
        /// Verbatim string, not an enum; see <see cref="CheatsheetSource.Kind"/>.
        /// </summary>
        public string ValueAction { get; set; } = CheatsheetConstant.ValueActionNone;

        /// <summary>Whether the client offers "open the source note".</summary>
        public bool OpenSource { get; set; } = true;

        /// <summary>Null = unbound.</summary>
        public CheatsheetSource Source { get; set; }

        /// <summary>
        /// Every row property this version did not consume as a typed field,
        /// cloned verbatim and re-emitted on write.
        /// </summary>
        public IDictionary<string, JsonElement> ExtraFields { get; set; } =
            new Dictionary<string, JsonElement>();

        /// <summary>
        /// The whole row, kept verbatim, when this version cannot model it at
        /// all (not a JSON object, or a "source" that is not an object).
        /// Mirrors the Flutter codec's unreadableRows: saving rewrites the whole
        /// card, so a row dropped on read would be erased by the next unrelated
        /// edit. Emitted untouched, in its original position.
        /// </summary>
        public JsonElement? RawJson { get; set; }

        public bool IsUnreadable => RawJson.HasValue;
    }
}
```

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/DomainEntity/CheatsheetCard.cs`:

```csharp
using System.Collections.Generic;
using System.Text.Json;

namespace Hmm.Cheatsheet.DomainEntity
{
    /// <summary>
    /// A read-only "wallet" card: a titled, grouped list of labelled rows, each
    /// referencing a piece of some note. Persisted as one HmmNote's content
    /// under the Hmm.CheatsheetMan.Cheatsheet catalog.
    /// </summary>
    public class CheatsheetCard
    {
        /// <summary>The backing note's local int id. Not part of the card JSON.</summary>
        public int NoteId { get; set; }

        /// <summary>The owning author's id. Not part of the card JSON.</summary>
        public int AuthorId { get; set; }

        public int SchemaVersion { get; set; } = CheatsheetConstant.CurrentSchemaVersion;

        /// <summary>
        /// Stable v4 UUID minted once at create time and never regenerated on
        /// edit. Also the note's subject, so it must not track the mutable,
        /// non-unique <see cref="Title"/>.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string WalletGroup { get; set; } = CheatsheetConstant.DefaultWalletGroup;

        public IList<string> Tags { get; set; } = new List<string>();

        public string TemplateId { get; set; } = CheatsheetConstant.DefaultTemplateId;

        /// <summary>
        /// Stored verbatim. The server never gates, encrypts or rejects on this
        /// flag - it is a client-side UI concern.
        /// </summary>
        public bool Protected { get; set; }

        public IList<CheatsheetRow> Rows { get; set; } = new List<CheatsheetRow>();

        /// <summary>
        /// Every card property this version did not consume as a typed field,
        /// cloned verbatim and re-emitted on write.
        /// </summary>
        public IDictionary<string, JsonElement> ExtraFields { get; set; } =
            new Dictionary<string, JsonElement>();

        public static string GetNoteSubject(string cardId)
            => $"{CheatsheetConstant.CheatsheetSubjectPrefix}{cardId}";
    }
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `cd /Users/fchy/Projects/Hmm && dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetCardTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 5`.

- [ ] **Step 7: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add Hmm.sln src/Hmm.Cheatsheet src/Hmm.Cheatsheet.Tests
git commit -m "feat(cheatsheet): scaffold Hmm.Cheatsheet module and domain model"
```

---

### Task 2: Cheatsheet note-catalog provider

**Files:**
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/ICheatsheetCatalogProvider.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/CheatsheetCatalogProvider.cs`
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetCatalogProviderTests.cs`

**Interfaces:**
- Consumes: `CheatsheetConstant.CheatsheetCatalogName` (Task 1); `Hmm.Utility.Dal.Query.IEntityLookup.GetEntitiesAsync<T>(Expression<Func<T,bool>> query = null, ResourceCollectionParameters parameters = null)` returning `Task<ProcessingResult<PageList<T>>>`.
- Produces: `Hmm.Cheatsheet.ICheatsheetCatalogProvider` with `Task<NoteCatalog> GetCatalogAsync()`; `Hmm.Cheatsheet.CheatsheetCatalogProvider(IEntityLookup lookupRepo, ILogger<CheatsheetCatalogProvider> logger = null)`.

- [ ] **Step 1: Write the failing test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetCatalogProviderTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetCatalogProviderTests
    {
        private static Mock<IEntityLookup> LookupReturning(params NoteCatalog[] catalogs)
        {
            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(
                    new PageList<NoteCatalog>(catalogs, catalogs.Length, 1, 10)));
            return lookup;
        }

        [Fact]
        public async Task GetCatalogAsync_ReturnsTheCheatsheetCatalog()
        {
            // Arrange
            var catalog = new NoteCatalog { Id = 7, Name = CheatsheetConstant.CheatsheetCatalogName };
            var provider = new CheatsheetCatalogProvider(LookupReturning(catalog).Object);

            // Act
            var result = await provider.GetCatalogAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(7, result.Id);
        }

        [Fact]
        public async Task GetCatalogAsync_HitsTheLookupOnlyOnce()
        {
            // Arrange
            var catalog = new NoteCatalog { Id = 7, Name = CheatsheetConstant.CheatsheetCatalogName };
            var lookup = LookupReturning(catalog);
            var provider = new CheatsheetCatalogProvider(lookup.Object);

            // Act
            await provider.GetCatalogAsync();
            await provider.GetCatalogAsync();

            // Assert
            lookup.Verify(
                l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()),
                Times.Once);
        }

        [Fact]
        public async Task GetCatalogAsync_ReturnsNull_WhenCatalogMissing()
        {
            // Arrange
            var provider = new CheatsheetCatalogProvider(LookupReturning(Array.Empty<NoteCatalog>()).Object);

            // Act
            var result = await provider.GetCatalogAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCatalogAsync_ReturnsNull_WhenLookupFails()
        {
            // Arrange
            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Fail("boom"));
            var provider = new CheatsheetCatalogProvider(lookup.Object);

            // Act
            var result = await provider.GetCatalogAsync();

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Constructor_Throws_WhenLookupIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new CheatsheetCatalogProvider(null));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetCatalogProviderTests"`

Expected: FAIL — build error `CS0246: The type or namespace name 'CheatsheetCatalogProvider' could not be found`.

- [ ] **Step 3: Write the interface**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/ICheatsheetCatalogProvider.cs`:

```csharp
using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;

namespace Hmm.Cheatsheet
{
    /// <summary>
    /// Resolves the single NoteCatalog cheatsheet cards live under. Split out
    /// from the serializer so the serializer stays free of repository concerns,
    /// mirroring Hmm.Automobile.INoteCatalogProvider.
    /// </summary>
    public interface ICheatsheetCatalogProvider
    {
        /// <summary>
        /// Returns the cheatsheet catalog, or null when it does not exist yet.
        /// </summary>
        Task<NoteCatalog> GetCatalogAsync();
    }
}
```

- [ ] **Step 4: Write the implementation**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/CheatsheetCatalogProvider.cs`:

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Microsoft.Extensions.Logging;

namespace Hmm.Cheatsheet
{
    /// <summary>
    /// Caches the cheatsheet NoteCatalog after the first successful lookup so
    /// every serialize/deserialize does not re-query the database.
    /// </summary>
    public class CheatsheetCatalogProvider : ICheatsheetCatalogProvider
    {
        private readonly IEntityLookup _lookupRepo;
        private readonly ILogger<CheatsheetCatalogProvider> _logger;
        private NoteCatalog _cachedCatalog;

        public CheatsheetCatalogProvider(
            IEntityLookup lookupRepo,
            ILogger<CheatsheetCatalogProvider> logger = null)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);

            _lookupRepo = lookupRepo;
            _logger = logger;
        }

        public async Task<NoteCatalog> GetCatalogAsync()
        {
            if (_cachedCatalog != null)
            {
                return _cachedCatalog;
            }

            var catalogsResult = await _lookupRepo.GetEntitiesAsync<NoteCatalog>(
                c => c.Name == CheatsheetConstant.CheatsheetCatalogName);

            if (!catalogsResult.Success || catalogsResult.Value == null)
            {
                _logger?.LogWarning(
                    "Failed to retrieve catalog {CatalogName}: {Error}",
                    CheatsheetConstant.CheatsheetCatalogName,
                    catalogsResult.ErrorMessage);
                return null;
            }

            var catalog = catalogsResult.Value.FirstOrDefault();
            if (catalog == null)
            {
                _logger?.LogWarning(
                    "Catalog {CatalogName} does not exist yet",
                    CheatsheetConstant.CheatsheetCatalogName);
                return null;
            }

            _cachedCatalog = catalog;
            return catalog;
        }
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `cd /Users/fchy/Projects/Hmm && dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetCatalogProviderTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 5`.

- [ ] **Step 6: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add src/Hmm.Cheatsheet src/Hmm.Cheatsheet.Tests
git commit -m "feat(cheatsheet): add cached cheatsheet note-catalog provider"
```

---

### Task 3: `CheatsheetJsonNoteSerialize` — read path (`GetEntity`)

Deserialises note content into a `CheatsheetCard`, consuming a property into a typed field **only** when its JSON type is the expected one, and cloning everything else into `ExtraFields` / `RawJson`.

**Files:**
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/NoteSerialize/CheatsheetJsonNoteSerialize.cs`
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetJsonNoteSerializeReadTests.cs`

**Interfaces:**
- Consumes: `CheatsheetCard`/`CheatsheetRow`/`CheatsheetSource`/`CheatsheetConstant` (Task 1); `ICheatsheetCatalogProvider` (Task 2); `Hmm.Core.NoteSerializer.DefaultJsonNoteSerializer<T>(ILogger<T> logger)` with `protected ILogger Logger`, `protected JsonSerializerOptions JsonOptions`, `protected NoteCatalog Catalog`, `protected virtual Task<NoteCatalog> GetCatalogAsync()`, `public override Task<ProcessingResult<T>> GetEntity(HmmNote note)`, `public override Task<ProcessingResult<HmmNote>> GetNote(in T entity)`, `public virtual string GetNoteSerializationText(T entity)`.
- Produces: `Hmm.Cheatsheet.NoteSerialize.CheatsheetJsonNoteSerialize(ICheatsheetCatalogProvider catalogProvider, ILogger<CheatsheetCard> logger)` implementing `INoteSerializer<CheatsheetCard>`.

- [ ] **Step 1: Write the failing test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetJsonNoteSerializeReadTests.cs`:

```csharp
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.NoteSerialize;
using Hmm.Core.Map.DomainEntity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetJsonNoteSerializeReadTests
    {
        private static CheatsheetJsonNoteSerialize CreateSerializer()
        {
            var catalogProvider = new Mock<ICheatsheetCatalogProvider>();
            catalogProvider
                .Setup(p => p.GetCatalogAsync())
                .ReturnsAsync(new NoteCatalog
                {
                    Id = 7,
                    Name = CheatsheetConstant.CheatsheetCatalogName,
                    Schema = "{}"
                });

            return new CheatsheetJsonNoteSerialize(
                catalogProvider.Object,
                NullLogger<CheatsheetCard>.Instance);
        }

        private static HmmNote NoteWith(string cardJson, string cardId = "card-1")
            => new()
            {
                Id = 42,
                Subject = CheatsheetCard.GetNoteSubject(cardId),
                Content = "{\"note\":{\"content\":{\"Cheatsheet\":" + cardJson + "}}}",
                Author = new Author { Id = 9 }
            };

        [Fact]
        public async Task GetEntity_NullNote_Fails()
        {
            var result = await CreateSerializer().GetEntity(null);

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task GetEntity_EmptyContent_Fails()
        {
            var note = new HmmNote { Id = 1, Subject = "Cheatsheet:x", Content = string.Empty };

            var result = await CreateSerializer().GetEntity(note);

            Assert.False(result.Success);
            Assert.Contains("Empty note content", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEntity_MalformedJson_FailsWithoutThrowing()
        {
            var note = new HmmNote { Id = 1, Subject = "Cheatsheet:x", Content = "{not json" };

            var result = await CreateSerializer().GetEntity(note);

            Assert.False(result.Success);
            Assert.Contains("Invalid JSON format", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEntity_MissingCheatsheetPayload_Fails()
        {
            var note = new HmmNote
            {
                Id = 1,
                Subject = "Cheatsheet:x",
                Content = "{\"note\":{\"content\":{\"GasLog\":{}}}}"
            };

            var result = await CreateSerializer().GetEntity(note);

            Assert.False(result.Success);
            Assert.Contains("Cheatsheet", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEntity_ReadsEveryKnownField()
        {
            var note = NoteWith(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"Passport\"," +
                "\"walletGroup\":\"Travel\",\"tags\":[\"trip\",\"id\"]," +
                "\"templateId\":\"blank\",\"protected\":true," +
                "\"rows\":[{\"label\":\"Number\",\"valueAction\":\"call\",\"openSource\":false," +
                "\"source\":{\"noteUuid\":\"u-1\",\"kind\":\"field\",\"locator\":\"Passport.number\"}}]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            var card = result.Value;
            Assert.Equal(42, card.NoteId);
            Assert.Equal(9, card.AuthorId);
            Assert.Equal(1, card.SchemaVersion);
            Assert.Equal("card-1", card.Id);
            Assert.Equal("Passport", card.Title);
            Assert.Equal("Travel", card.WalletGroup);
            Assert.Equal(new[] { "trip", "id" }, card.Tags);
            Assert.Equal("blank", card.TemplateId);
            Assert.True(card.Protected);

            var row = Assert.Single(card.Rows);
            Assert.Equal("Number", row.Label);
            Assert.Equal("call", row.ValueAction);
            Assert.False(row.OpenSource);
            Assert.NotNull(row.Source);
            Assert.Equal("u-1", row.Source.NoteUuid);
            Assert.Equal("field", row.Source.Kind);
            Assert.Equal("Passport.number", row.Source.Locator);
        }

        [Fact]
        public async Task GetEntity_AppliesClientDefaults_WhenFieldsAbsent()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"rows\":[{\"label\":\"L\"}]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal("Ungrouped", result.Value.WalletGroup);
            Assert.Equal("blank", result.Value.TemplateId);
            Assert.False(result.Value.Protected);
            var row = Assert.Single(result.Value.Rows);
            Assert.Equal("none", row.ValueAction);
            Assert.True(row.OpenSource);
            Assert.Null(row.Source);
        }

        [Fact]
        public async Task GetEntity_FallsBackToSubject_WhenIdMissing()
        {
            var note = NoteWith("{\"title\":\"No id\"}", "subject-card");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal("subject-card", result.Value.Id);
        }

        [Fact]
        public async Task GetEntity_KeepsUnknownCardFields()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"futureFlag\":true,\"futureBag\":{\"a\":1}}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.True(result.Value.ExtraFields.ContainsKey("futureFlag"));
            Assert.Equal(JsonValueKind.True, result.Value.ExtraFields["futureFlag"].ValueKind);
            Assert.Equal("{\"a\":1}", result.Value.ExtraFields["futureBag"].GetRawText());
        }

        [Fact]
        public async Task GetEntity_KeepsMistypedKnownCardFields()
        {
            // "title" is a number, not a string: it must not be silently dropped.
            var note = NoteWith("{\"id\":\"card-1\",\"title\":17}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(string.Empty, result.Value.Title);
            Assert.Equal("17", result.Value.ExtraFields["title"].GetRawText());
        }

        [Fact]
        public async Task GetEntity_KeepsUnknownRowAndSourceFields()
        {
            var note = NoteWith(
                "{\"id\":\"card-1\",\"rows\":[{\"label\":\"L\",\"futureRowFlag\":3," +
                "\"source\":{\"noteUuid\":\"u\",\"kind\":\"whole\",\"futureSourceFlag\":\"x\"}}]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            var row = Assert.Single(result.Value.Rows);
            Assert.Equal("3", row.ExtraFields["futureRowFlag"].GetRawText());
            Assert.Equal("\"x\"", row.Source.ExtraFields["futureSourceFlag"].GetRawText());
        }

        [Fact]
        public async Task GetEntity_KeepsNonObjectRowVerbatim()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"rows\":[\"i am not a row\",{\"label\":\"L\"}]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal(2, result.Value.Rows.Count);
            Assert.True(result.Value.Rows[0].IsUnreadable);
            Assert.Equal("\"i am not a row\"", result.Value.Rows[0].RawJson.Value.GetRawText());
            Assert.False(result.Value.Rows[1].IsUnreadable);
        }

        [Fact]
        public async Task GetEntity_KeepsRowWithNonObjectSourceVerbatim()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"rows\":[{\"label\":\"L\",\"source\":\"oops\"}]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            var row = Assert.Single(result.Value.Rows);
            Assert.True(row.IsUnreadable);
            Assert.Contains("oops", row.RawJson.Value.GetRawText());
        }

        [Fact]
        public async Task GetEntity_KeepsNonArrayRowsVerbatim()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"rows\":\"nope\"}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Empty(result.Value.Rows);
            Assert.Equal("\"nope\"", result.Value.ExtraFields["rows"].GetRawText());
        }

        [Fact]
        public async Task GetEntity_KeepsTagsVerbatim_WhenNotAllStrings()
        {
            var note = NoteWith("{\"id\":\"card-1\",\"tags\":[\"ok\",7]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Empty(result.Value.Tags);
            Assert.Equal("[\"ok\",7]", result.Value.ExtraFields["tags"].GetRawText());
        }

        [Fact]
        public async Task GetEntity_PreservedElementsSurviveDocumentDisposal()
        {
            // Regression guard: JsonElements must be cloned, or reading them
            // after the JsonDocument is disposed throws ObjectDisposedException.
            var note = NoteWith("{\"id\":\"card-1\",\"futureBag\":{\"a\":1},\"rows\":[42]}");

            var result = await CreateSerializer().GetEntity(note);

            Assert.True(result.Success);
            Assert.Equal("{\"a\":1}", result.Value.ExtraFields["futureBag"].GetRawText());
            Assert.Equal("42", result.Value.Rows.Single().RawJson.Value.GetRawText());
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetJsonNoteSerializeReadTests"`

Expected: FAIL — build error `CS0246: The type or namespace name 'CheatsheetJsonNoteSerialize' could not be found`.

- [ ] **Step 3: Write the serializer read path**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/NoteSerialize/CheatsheetJsonNoteSerialize.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Core.NoteSerializer;
using Hmm.Utility.Misc;
using Microsoft.Extensions.Logging;

namespace Hmm.Cheatsheet.NoteSerialize
{
    /// <summary>
    /// Reads and writes a <see cref="CheatsheetCard"/> as the JSON content of an
    /// HmmNote, in the exact shape the Flutter client persists:
    /// { "note": { "content": { "Cheatsheet": { ... } } } }.
    ///
    /// Losslessness rule: a JSON property is consumed into a typed field ONLY
    /// when it is present with the expected JSON type. Unknown keys, and known
    /// keys carrying an unexpected type, are cloned into ExtraFields and
    /// re-emitted verbatim. A row that cannot be modelled at all is kept whole
    /// in CheatsheetRow.RawJson. This mirrors - and extends - the client's
    /// unreadableRows handling: a save must never destroy data this version did
    /// not understand.
    /// </summary>
    public class CheatsheetJsonNoteSerialize : DefaultJsonNoteSerializer<CheatsheetCard>
    {
        private const string KeySchemaVersion = "schemaVersion";
        private const string KeyId = "id";
        private const string KeyTitle = "title";
        private const string KeyWalletGroup = "walletGroup";
        private const string KeyTags = "tags";
        private const string KeyTemplateId = "templateId";
        private const string KeyProtected = "protected";
        private const string KeyRows = "rows";
        private const string KeyLabel = "label";
        private const string KeyValueAction = "valueAction";
        private const string KeyOpenSource = "openSource";
        private const string KeySource = "source";
        private const string KeyNoteUuid = "noteUuid";
        private const string KeyKind = "kind";
        private const string KeyLocator = "locator";

        private readonly ICheatsheetCatalogProvider _catalogProvider;

        public CheatsheetJsonNoteSerialize(
            ICheatsheetCatalogProvider catalogProvider,
            ILogger<CheatsheetCard> logger)
            : base(logger)
        {
            ArgumentNullException.ThrowIfNull(catalogProvider);

            _catalogProvider = catalogProvider;
        }

        protected override Task<NoteCatalog> GetCatalogAsync()
        {
            return _catalogProvider.GetCatalogAsync();
        }

        public override Task<ProcessingResult<CheatsheetCard>> GetEntity(HmmNote note)
        {
            if (note == null)
            {
                return Task.FromResult(ProcessingResult<CheatsheetCard>.Fail(
                    "Null note found when trying to deserialize cheatsheet card from note",
                    ErrorCategory.NotFound));
            }

            if (string.IsNullOrEmpty(note.Content))
            {
                return Task.FromResult(ProcessingResult<CheatsheetCard>.Fail(
                    "Empty note content found",
                    ErrorCategory.MappingError));
            }

            try
            {
                using var document = JsonDocument.Parse(note.Content);
                if (!TryGetCardElement(document, out var cardJson))
                {
                    return Task.FromResult(ProcessingResult<CheatsheetCard>.Fail(
                        $"Missing '{CheatsheetConstant.CheatsheetContentKey}' element in note content JSON",
                        ErrorCategory.MappingError));
                }

                var card = ReadCard(cardJson);
                card.NoteId = note.Id;
                card.AuthorId = note.Author?.Id ?? 0;

                if (string.IsNullOrEmpty(card.Id))
                {
                    // The subject is the identity of record; content may lag it.
                    card.Id = SubjectToCardId(note.Subject);
                }

                return Task.FromResult(ProcessingResult<CheatsheetCard>.Ok(card));
            }
            catch (JsonException ex)
            {
                Logger?.LogError(ex, "JSON parsing error while deserializing cheatsheet card");
                return Task.FromResult(ProcessingResult<CheatsheetCard>.Fail(
                    $"Invalid JSON format: {ex.Message}",
                    ErrorCategory.MappingError));
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error deserializing cheatsheet card from note");
                return Task.FromResult(ProcessingResult<CheatsheetCard>.FromException(ex));
            }
        }

        private static string SubjectToCardId(string subject)
        {
            if (string.IsNullOrEmpty(subject) ||
                !subject.StartsWith(CheatsheetConstant.CheatsheetSubjectPrefix, StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return subject.Substring(CheatsheetConstant.CheatsheetSubjectPrefix.Length);
        }

        private static bool TryGetCardElement(JsonDocument document, out JsonElement cardJson)
        {
            cardJson = default;

            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("note", out var noteElement) ||
                noteElement.ValueKind != JsonValueKind.Object ||
                !noteElement.TryGetProperty("content", out var contentElement) ||
                contentElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (contentElement.TryGetProperty(CheatsheetConstant.CheatsheetContentKey, out cardJson) &&
                cardJson.ValueKind == JsonValueKind.Object)
            {
                return true;
            }

            // Tolerate a camelCase writer, the way EntityJsonNoteSerializeBase does.
            return contentElement.TryGetProperty("cheatsheet", out cardJson) &&
                   cardJson.ValueKind == JsonValueKind.Object;
        }

        private static CheatsheetCard ReadCard(JsonElement cardJson)
        {
            var consumed = new HashSet<string>(StringComparer.Ordinal);

            var card = new CheatsheetCard
            {
                SchemaVersion = ReadInt(cardJson, KeySchemaVersion, CheatsheetConstant.CurrentSchemaVersion, consumed),
                Id = ReadString(cardJson, KeyId, consumed) ?? string.Empty,
                Title = ReadString(cardJson, KeyTitle, consumed) ?? string.Empty,
                WalletGroup = ReadString(cardJson, KeyWalletGroup, consumed) ?? CheatsheetConstant.DefaultWalletGroup,
                TemplateId = ReadString(cardJson, KeyTemplateId, consumed) ?? CheatsheetConstant.DefaultTemplateId,
                Protected = ReadBool(cardJson, KeyProtected, false, consumed),
                Tags = ReadStringList(cardJson, KeyTags, consumed),
                Rows = ReadRows(cardJson, consumed)
            };

            card.ExtraFields = ReadExtras(cardJson, consumed);
            return card;
        }

        private static IList<CheatsheetRow> ReadRows(JsonElement cardJson, HashSet<string> consumed)
        {
            var rows = new List<CheatsheetRow>();

            if (!cardJson.TryGetProperty(KeyRows, out var rowsJson) ||
                rowsJson.ValueKind != JsonValueKind.Array)
            {
                // Not an array: leave it unconsumed so it survives in ExtraFields.
                return rows;
            }

            consumed.Add(KeyRows);
            foreach (var rowJson in rowsJson.EnumerateArray())
            {
                rows.Add(ReadRow(rowJson));
            }

            return rows;
        }

        private static CheatsheetRow ReadRow(JsonElement rowJson)
        {
            if (rowJson.ValueKind != JsonValueKind.Object)
            {
                return new CheatsheetRow { RawJson = rowJson.Clone() };
            }

            // A non-object, non-null "source" is exactly the case the client
            // treats as an unreadable row. Keep the whole row rather than
            // guessing at a repair.
            if (rowJson.TryGetProperty(KeySource, out var probeSource) &&
                probeSource.ValueKind != JsonValueKind.Object &&
                probeSource.ValueKind != JsonValueKind.Null)
            {
                return new CheatsheetRow { RawJson = rowJson.Clone() };
            }

            var consumed = new HashSet<string>(StringComparer.Ordinal);
            var row = new CheatsheetRow
            {
                Label = ReadString(rowJson, KeyLabel, consumed) ?? string.Empty,
                ValueAction = ReadString(rowJson, KeyValueAction, consumed) ?? CheatsheetConstant.ValueActionNone,
                OpenSource = ReadBool(rowJson, KeyOpenSource, true, consumed)
            };

            if (rowJson.TryGetProperty(KeySource, out var sourceJson) &&
                sourceJson.ValueKind == JsonValueKind.Object)
            {
                consumed.Add(KeySource);
                row.Source = ReadSource(sourceJson);
            }
            else if (rowJson.TryGetProperty(KeySource, out var nullSource) &&
                     nullSource.ValueKind == JsonValueKind.Null)
            {
                // Explicit null means unbound; nothing to preserve.
                consumed.Add(KeySource);
            }

            row.ExtraFields = ReadExtras(rowJson, consumed);
            return row;
        }

        private static CheatsheetSource ReadSource(JsonElement sourceJson)
        {
            var consumed = new HashSet<string>(StringComparer.Ordinal);
            var source = new CheatsheetSource
            {
                NoteUuid = ReadString(sourceJson, KeyNoteUuid, consumed) ?? string.Empty,
                Kind = ReadString(sourceJson, KeyKind, consumed) ?? CheatsheetConstant.SourceKindWhole,
                Locator = ReadString(sourceJson, KeyLocator, consumed)
            };

            source.ExtraFields = ReadExtras(sourceJson, consumed);
            return source;
        }

        private static IDictionary<string, JsonElement> ReadExtras(JsonElement element, HashSet<string> consumed)
        {
            var extras = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

            foreach (var property in element.EnumerateObject())
            {
                if (consumed.Contains(property.Name))
                {
                    continue;
                }

                // Clone: the owning JsonDocument is disposed before this
                // dictionary escapes the serializer.
                extras[property.Name] = property.Value.Clone();
            }

            return extras;
        }

        private static string ReadString(JsonElement element, string name, HashSet<string> consumed)
        {
            if (element.TryGetProperty(name, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                consumed.Add(name);
                return property.GetString();
            }

            return null;
        }

        private static bool ReadBool(JsonElement element, string name, bool defaultValue, HashSet<string> consumed)
        {
            if (element.TryGetProperty(name, out var property))
            {
                if (property.ValueKind == JsonValueKind.True)
                {
                    consumed.Add(name);
                    return true;
                }

                if (property.ValueKind == JsonValueKind.False)
                {
                    consumed.Add(name);
                    return false;
                }
            }

            return defaultValue;
        }

        private static int ReadInt(JsonElement element, string name, int defaultValue, HashSet<string> consumed)
        {
            if (element.TryGetProperty(name, out var property) &&
                property.ValueKind == JsonValueKind.Number &&
                property.TryGetDouble(out var value))
            {
                consumed.Add(name);
                return (int)value;
            }

            return defaultValue;
        }

        private static IList<string> ReadStringList(JsonElement element, string name, HashSet<string> consumed)
        {
            var values = new List<string>();

            if (!element.TryGetProperty(name, out var property) ||
                property.ValueKind != JsonValueKind.Array)
            {
                return values;
            }

            foreach (var item in property.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    // Mixed array: do not consume, so the original survives
                    // verbatim in ExtraFields.
                    return new List<string>();
                }

                values.Add(item.GetString());
            }

            consumed.Add(name);
            return values;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetJsonNoteSerializeReadTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 15`.

- [ ] **Step 5: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add src/Hmm.Cheatsheet src/Hmm.Cheatsheet.Tests
git commit -m "feat(cheatsheet): lossless note-content read path for cheatsheet cards"
```

---

### Task 4: `CheatsheetJsonNoteSerialize` — write path (`GetNoteSerializationText`, `GetNote`)

**Files:**
- Modify: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/NoteSerialize/CheatsheetJsonNoteSerialize.cs` (add the write members below the read members)
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetJsonNoteSerializeWriteTests.cs`

**Interfaces:**
- Consumes: everything from Task 3.
- Produces: `public override string GetNoteSerializationText(CheatsheetCard entity)`; `public override Task<ProcessingResult<HmmNote>> GetNote(in CheatsheetCard entity)` — returns an `HmmNote` with `Id = entity.NoteId`, `Subject = CheatsheetCard.GetNoteSubject(entity.Id)`, `Content` = the serialized text, `Catalog` = the provider's catalog.

- [ ] **Step 1: Write the failing test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetJsonNoteSerializeWriteTests.cs`:

```csharp
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.NoteSerialize;
using Hmm.Core.Map.DomainEntity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetJsonNoteSerializeWriteTests
    {
        private static CheatsheetJsonNoteSerialize CreateSerializer()
        {
            var catalogProvider = new Mock<ICheatsheetCatalogProvider>();
            catalogProvider
                .Setup(p => p.GetCatalogAsync())
                .ReturnsAsync(new NoteCatalog
                {
                    Id = 7,
                    Name = CheatsheetConstant.CheatsheetCatalogName,
                    Schema = "{}"
                });

            return new CheatsheetJsonNoteSerialize(
                catalogProvider.Object,
                NullLogger<CheatsheetCard>.Instance);
        }

        private static JsonElement CardElementOf(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement
                .GetProperty("note")
                .GetProperty("content")
                .GetProperty("Cheatsheet")
                .Clone();
        }

        private static JsonElement Raw(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static CheatsheetCard SampleCard() => new()
        {
            NoteId = 42,
            Id = "card-1",
            Title = "Passport",
            WalletGroup = "Travel",
            TemplateId = "blank",
            Protected = true,
            Tags = new List<string> { "trip" },
            Rows = new List<CheatsheetRow>
            {
                new()
                {
                    Label = "Number",
                    ValueAction = "call",
                    OpenSource = false,
                    Source = new CheatsheetSource
                    {
                        NoteUuid = "u-1",
                        Kind = "field",
                        Locator = "Passport.number"
                    }
                }
            }
        };

        [Fact]
        public void GetNoteSerializationText_NullEntity_ReturnsEmptyString()
        {
            Assert.Empty(CreateSerializer().GetNoteSerializationText(null));
        }

        [Fact]
        public void GetNoteSerializationText_UsesTheClientEnvelopeAndCamelCaseKeys()
        {
            var json = CreateSerializer().GetNoteSerializationText(SampleCard());

            var card = CardElementOf(json);
            Assert.Equal(1, card.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("card-1", card.GetProperty("id").GetString());
            Assert.Equal("Passport", card.GetProperty("title").GetString());
            Assert.Equal("Travel", card.GetProperty("walletGroup").GetString());
            Assert.Equal("blank", card.GetProperty("templateId").GetString());
            Assert.True(card.GetProperty("protected").GetBoolean());
            Assert.Equal(JsonValueKind.Array, card.GetProperty("tags").ValueKind);
            Assert.Equal(JsonValueKind.Array, card.GetProperty("rows").ValueKind);
        }

        [Fact]
        public void GetNoteSerializationText_WritesRowAndSourceKeys()
        {
            var json = CreateSerializer().GetNoteSerializationText(SampleCard());

            var row = CardElementOf(json).GetProperty("rows")[0];
            Assert.Equal("Number", row.GetProperty("label").GetString());
            Assert.Equal("call", row.GetProperty("valueAction").GetString());
            Assert.False(row.GetProperty("openSource").GetBoolean());

            var source = row.GetProperty("source");
            Assert.Equal("u-1", source.GetProperty("noteUuid").GetString());
            Assert.Equal("field", source.GetProperty("kind").GetString());
            Assert.Equal("Passport.number", source.GetProperty("locator").GetString());
        }

        [Fact]
        public void GetNoteSerializationText_OmitsLocator_WhenNull()
        {
            var card = SampleCard();
            card.Rows[0].Source.Locator = null;

            var json = CreateSerializer().GetNoteSerializationText(card);

            var source = CardElementOf(json).GetProperty("rows")[0].GetProperty("source");
            Assert.False(source.TryGetProperty("locator", out _));
        }

        [Fact]
        public void GetNoteSerializationText_OmitsSource_WhenRowIsUnbound()
        {
            var card = SampleCard();
            card.Rows[0].Source = null;

            var json = CreateSerializer().GetNoteSerializationText(card);

            var row = CardElementOf(json).GetProperty("rows")[0];
            Assert.False(row.TryGetProperty("source", out _));
        }

        [Fact]
        public void GetNoteSerializationText_EmitsUnreadableRowVerbatimInPlace()
        {
            var card = SampleCard();
            card.Rows.Insert(0, new CheatsheetRow { RawJson = Raw("\"i am not a row\"") });

            var json = CreateSerializer().GetNoteSerializationText(card);

            var rows = CardElementOf(json).GetProperty("rows");
            Assert.Equal(2, rows.GetArrayLength());
            Assert.Equal("i am not a row", rows[0].GetString());
            Assert.Equal("Number", rows[1].GetProperty("label").GetString());
        }

        [Fact]
        public void GetNoteSerializationText_EmitsExtrasAtEveryLevel()
        {
            var card = SampleCard();
            card.ExtraFields["futureFlag"] = Raw("true");
            card.Rows[0].ExtraFields["futureRowFlag"] = Raw("3");
            card.Rows[0].Source.ExtraFields["futureSourceFlag"] = Raw("\"x\"");

            var json = CreateSerializer().GetNoteSerializationText(card);

            var cardJson = CardElementOf(json);
            Assert.True(cardJson.GetProperty("futureFlag").GetBoolean());
            var row = cardJson.GetProperty("rows")[0];
            Assert.Equal(3, row.GetProperty("futureRowFlag").GetInt32());
            Assert.Equal("x", row.GetProperty("source").GetProperty("futureSourceFlag").GetString());
        }

        [Fact]
        public void GetNoteSerializationText_ExtrasWinOverFabricatedDefaults()
        {
            // A mistyped "title" landed in ExtraFields on read; the fabricated
            // empty-string default must not overwrite the original value.
            var card = SampleCard();
            card.Title = string.Empty;
            card.ExtraFields["title"] = Raw("17");

            var json = CreateSerializer().GetNoteSerializationText(card);

            Assert.Equal(17, CardElementOf(json).GetProperty("title").GetInt32());
        }

        [Fact]
        public async Task GetNote_BuildsNoteWithSubjectContentAndCatalog()
        {
            var result = await CreateSerializer().GetNote(SampleCard());

            Assert.True(result.Success);
            Assert.Equal(42, result.Value.Id);
            Assert.Equal("Cheatsheet:card-1", result.Value.Subject);
            Assert.Contains("\"walletGroup\":\"Travel\"", result.Value.Content);
            Assert.NotNull(result.Value.Catalog);
            Assert.Equal(CheatsheetConstant.CheatsheetCatalogName, result.Value.Catalog.Name);
        }

        [Fact]
        public async Task GetNote_NullEntity_Fails()
        {
            var result = await CreateSerializer().GetNote(null);

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task GetNote_EmptyCardId_Fails()
        {
            var card = SampleCard();
            card.Id = string.Empty;

            var result = await CreateSerializer().GetNote(card);

            Assert.False(result.Success);
            Assert.Contains("id is required", result.ErrorMessage);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetJsonNoteSerializeWriteTests"`

Expected: FAIL — several assertions fail because the inherited base implementations return `string.Empty` / a `Fail` result, e.g. `Assert.Equal() Failure` on the envelope test and `Assert.True(result.Success)` failing with "GetNote must be overridden to serialize CheatsheetCard to JSON".

- [ ] **Step 3: Add the write members**

Add these members to `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/NoteSerialize/CheatsheetJsonNoteSerialize.cs`, immediately after the `GetEntity` method and before `SubjectToCardId`. Also add `using System.Linq;` to the file's using block.

```csharp
        public override Task<ProcessingResult<HmmNote>> GetNote(in CheatsheetCard entity)
        {
            if (entity == null)
            {
                return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                    "Null entity found when trying to serialize cheatsheet card to note",
                    ErrorCategory.NotFound));
            }

            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                    "Cheatsheet card id is required to build the note subject",
                    ErrorCategory.MappingError));
            }

            var content = GetNoteSerializationText(entity);
            if (string.IsNullOrEmpty(content))
            {
                return Task.FromResult(ProcessingResult<HmmNote>.Fail(
                    "Failed to serialize cheatsheet card content to JSON",
                    ErrorCategory.MappingError));
            }

            var note = new HmmNote
            {
                Id = entity.NoteId,
                Subject = CheatsheetCard.GetNoteSubject(entity.Id),
                Content = content,
                Catalog = Catalog
            };

            return Task.FromResult(ProcessingResult<HmmNote>.Ok(note));
        }

        public override string GetNoteSerializationText(CheatsheetCard entity)
        {
            if (entity == null)
            {
                Logger?.LogWarning("Null cheatsheet card provided for serialization");
                return string.Empty;
            }

            try
            {
                // Insertion order is the wire order; extras are copied last so a
                // preserved original always wins over a fabricated default.
                var cardData = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    [KeySchemaVersion] = entity.SchemaVersion,
                    [KeyId] = entity.Id ?? string.Empty,
                    [KeyTitle] = entity.Title ?? string.Empty,
                    [KeyWalletGroup] = entity.WalletGroup ?? CheatsheetConstant.DefaultWalletGroup,
                    [KeyTags] = entity.Tags ?? new List<string>(),
                    [KeyTemplateId] = entity.TemplateId ?? CheatsheetConstant.DefaultTemplateId,
                    [KeyProtected] = entity.Protected,
                    [KeyRows] = (entity.Rows ?? new List<CheatsheetRow>()).Select(WriteRow).ToList()
                };

                CopyExtras(entity.ExtraFields, cardData);

                var noteStructure = new
                {
                    note = new
                    {
                        content = new Dictionary<string, object>(StringComparer.Ordinal)
                        {
                            [CheatsheetConstant.CheatsheetContentKey] = cardData
                        }
                    }
                };

                return JsonSerializer.Serialize(noteStructure, JsonOptions);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error serializing cheatsheet card to JSON");
                return string.Empty;
            }
        }

        private static object WriteRow(CheatsheetRow row)
        {
            if (row == null)
            {
                return new Dictionary<string, object>(StringComparer.Ordinal);
            }

            // A row this version could not model is re-emitted byte-for-byte,
            // in its original position.
            if (row.RawJson.HasValue)
            {
                return row.RawJson.Value;
            }

            var rowData = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [KeyLabel] = row.Label ?? string.Empty,
                [KeyValueAction] = row.ValueAction ?? CheatsheetConstant.ValueActionNone,
                [KeyOpenSource] = row.OpenSource
            };

            if (row.Source != null)
            {
                var sourceData = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    [KeyNoteUuid] = row.Source.NoteUuid ?? string.Empty,
                    [KeyKind] = row.Source.Kind ?? CheatsheetConstant.SourceKindWhole
                };

                // JsonSerializer's WhenWritingNull only applies to POCO members,
                // not dictionary entries, so omit the key explicitly.
                if (row.Source.Locator != null)
                {
                    sourceData[KeyLocator] = row.Source.Locator;
                }

                CopyExtras(row.Source.ExtraFields, sourceData);
                rowData[KeySource] = sourceData;
            }

            CopyExtras(row.ExtraFields, rowData);
            return rowData;
        }

        private static void CopyExtras(
            IDictionary<string, JsonElement> extras,
            IDictionary<string, object> target)
        {
            if (extras == null)
            {
                return;
            }

            foreach (var extra in extras)
            {
                target[extra.Key] = extra.Value;
            }
        }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetJsonNoteSerializeWriteTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 11`.

- [ ] **Step 5: Run the whole module's tests to confirm the read path still passes**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj`

Expected: PASS — `Passed!  - Failed: 0, Passed: 36`.

- [ ] **Step 6: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add src/Hmm.Cheatsheet src/Hmm.Cheatsheet.Tests
git commit -m "feat(cheatsheet): lossless note-content write path for cheatsheet cards"
```

---

### Task 5: Lossless round-trip guarantee (THE CRITICAL INTEROP RULE)

This task adds no production code unless the tests find a hole. It is the executable statement of the rule: a card that goes through `GetEntity` → `GetNoteSerializationText` must come back out with **every** semantic byte intact — unknown card fields, unknown row fields, unknown source fields, non-object rows, and mistyped known fields.

**Files:**
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetRoundTripTests.cs`
- Modify (only if a test fails): `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/NoteSerialize/CheatsheetJsonNoteSerialize.cs`

**Interfaces:**
- Consumes: `CheatsheetJsonNoteSerialize` (Tasks 3 and 4).
- Produces: no new public API.

- [ ] **Step 1: Write the round-trip test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetRoundTripTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.NoteSerialize;
using Hmm.Core.Map.DomainEntity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    /// <summary>
    /// The client's codec deliberately preserves rows it cannot parse and
    /// re-saves them untouched. A backend that validated strictly and dropped
    /// what it did not understand would silently delete the very data the
    /// client is protecting. These tests are the contract.
    /// </summary>
    public class CheatsheetRoundTripTests
    {
        private static CheatsheetJsonNoteSerialize CreateSerializer()
        {
            var catalogProvider = new Mock<ICheatsheetCatalogProvider>();
            catalogProvider
                .Setup(p => p.GetCatalogAsync())
                .ReturnsAsync(new NoteCatalog
                {
                    Id = 7,
                    Name = CheatsheetConstant.CheatsheetCatalogName,
                    Schema = "{}"
                });

            return new CheatsheetJsonNoteSerialize(
                catalogProvider.Object,
                NullLogger<CheatsheetCard>.Instance);
        }

        private static HmmNote NoteWith(string cardJson)
            => new()
            {
                Id = 42,
                Subject = CheatsheetCard.GetNoteSubject("card-1"),
                Content = "{\"note\":{\"content\":{\"Cheatsheet\":" + cardJson + "}}}",
                Author = new Author { Id = 9 }
            };

        /// <summary>
        /// Order-insensitive structural equality: JSON object key order is not
        /// semantic, array order is.
        /// </summary>
        private static bool JsonEquals(JsonElement left, JsonElement right)
        {
            if (left.ValueKind != right.ValueKind)
            {
                return false;
            }

            switch (left.ValueKind)
            {
                case JsonValueKind.Object:
                    var leftProps = left.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                    var rightProps = right.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                    if (leftProps.Count != rightProps.Count)
                    {
                        return false;
                    }

                    foreach (var pair in leftProps)
                    {
                        if (!rightProps.TryGetValue(pair.Key, out var other) ||
                            !JsonEquals(pair.Value, other))
                        {
                            return false;
                        }
                    }

                    return true;

                case JsonValueKind.Array:
                    var leftItems = left.EnumerateArray().ToList();
                    var rightItems = right.EnumerateArray().ToList();
                    if (leftItems.Count != rightItems.Count)
                    {
                        return false;
                    }

                    return !leftItems.Where((t, i) => !JsonEquals(t, rightItems[i])).Any();

                case JsonValueKind.String:
                    return left.GetString() == right.GetString();

                case JsonValueKind.Number:
                    return left.GetDouble().Equals(right.GetDouble());

                default:
                    return true; // True / False / Null / Undefined - kind match is enough.
            }
        }

        private static JsonElement CardElementOf(string noteJson)
        {
            using var document = JsonDocument.Parse(noteJson);
            return document.RootElement
                .GetProperty("note")
                .GetProperty("content")
                .GetProperty("Cheatsheet")
                .Clone();
        }

        private static async Task AssertLosslessAsync(string cardJson)
        {
            // Arrange
            var serializer = CreateSerializer();
            var note = NoteWith(cardJson);

            // Act - read, then write back out.
            var readResult = await serializer.GetEntity(note);
            Assert.True(readResult.Success, readResult.ErrorMessage);
            var rewritten = serializer.GetNoteSerializationText(readResult.Value);

            // Assert
            using var originalDocument = JsonDocument.Parse(cardJson);
            var actual = CardElementOf(rewritten);
            Assert.True(
                JsonEquals(originalDocument.RootElement, actual),
                $"Round-trip lost data.\nExpected: {originalDocument.RootElement.GetRawText()}\nActual:   {actual.GetRawText()}");
        }

        [Fact]
        public Task RoundTrip_FullyKnownCard_IsLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"Passport\"," +
                "\"walletGroup\":\"Travel\",\"tags\":[\"trip\",\"id\"],\"templateId\":\"blank\"," +
                "\"protected\":true,\"rows\":[{\"label\":\"Number\",\"valueAction\":\"call\"," +
                "\"openSource\":false,\"source\":{\"noteUuid\":\"u-1\",\"kind\":\"field\"," +
                "\"locator\":\"Passport.number\"}}]}");

        [Fact]
        public Task RoundTrip_UnknownCardFields_AreLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":2,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[],\"templateId\":\"blank\",\"protected\":false,\"rows\":[]," +
                "\"quickAccess\":true,\"sortOrder\":5,\"future\":{\"nested\":[1,2,3]}}");

        [Fact]
        public Task RoundTrip_UnknownRowAndSourceFields_AreLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[],\"templateId\":\"blank\",\"protected\":false,\"rows\":[" +
                "{\"label\":\"L\",\"valueAction\":\"none\",\"openSource\":true," +
                "\"icon\":\"star\",\"copyOnTap\":true," +
                "\"source\":{\"noteUuid\":\"u\",\"kind\":\"whole\",\"revision\":12}}]}");

        [Fact]
        public Task RoundTrip_UnknownValueActionToken_IsLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[],\"templateId\":\"blank\",\"protected\":false,\"rows\":[" +
                "{\"label\":\"L\",\"valueAction\":\"sms\",\"openSource\":true," +
                "\"source\":{\"noteUuid\":\"u\",\"kind\":\"paragraph\"}}]}");

        [Fact]
        public Task RoundTrip_NonObjectRows_AreLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[],\"templateId\":\"blank\",\"protected\":false," +
                "\"rows\":[\"corrupt\",42,null,{\"label\":\"L\",\"valueAction\":\"none\",\"openSource\":true}]}");

        [Fact]
        public Task RoundTrip_RowWithNonObjectSource_IsLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[],\"templateId\":\"blank\",\"protected\":false," +
                "\"rows\":[{\"label\":\"L\",\"source\":\"oops\",\"keepMe\":9}]}");

        [Fact]
        public Task RoundTrip_MistypedKnownFields_AreLossless()
            => AssertLosslessAsync(
                "{\"schemaVersion\":\"one\",\"id\":\"card-1\",\"title\":17,\"walletGroup\":null," +
                "\"tags\":[\"ok\",7],\"templateId\":\"blank\",\"protected\":\"yes\",\"rows\":[]}");

        [Fact]
        public async Task RoundTrip_IsStableAcrossRepeatedSaves()
        {
            // A second save must not drift: extras carried on the first pass
            // have to survive the next read unchanged.
            const string cardJson =
                "{\"schemaVersion\":1,\"id\":\"card-1\",\"title\":\"T\",\"walletGroup\":\"G\"," +
                "\"tags\":[\"a\"],\"templateId\":\"blank\",\"protected\":false," +
                "\"rows\":[\"corrupt\",{\"label\":\"L\",\"valueAction\":\"none\"," +
                "\"openSource\":true,\"icon\":\"star\"}],\"future\":{\"x\":1}}";

            var serializer = CreateSerializer();

            var firstRead = await serializer.GetEntity(NoteWith(cardJson));
            Assert.True(firstRead.Success);
            var firstWrite = serializer.GetNoteSerializationText(firstRead.Value);

            var secondRead = await serializer.GetEntity(NoteWith(CardElementOf(firstWrite).GetRawText()));
            Assert.True(secondRead.Success);
            var secondWrite = serializer.GetNoteSerializationText(secondRead.Value);

            using var firstDocument = JsonDocument.Parse(firstWrite);
            using var secondDocument = JsonDocument.Parse(secondWrite);
            Assert.True(
                JsonEquals(firstDocument.RootElement, secondDocument.RootElement),
                $"Second save drifted.\nFirst:  {firstWrite}\nSecond: {secondWrite}");
        }

        [Fact]
        public async Task RoundTrip_PreservesRowOrder()
        {
            var serializer = CreateSerializer();
            var note = NoteWith(
                "{\"id\":\"card-1\",\"rows\":[{\"label\":\"one\"},\"corrupt\",{\"label\":\"three\"}]}");

            var readResult = await serializer.GetEntity(note);
            Assert.True(readResult.Success);
            var rewritten = serializer.GetNoteSerializationText(readResult.Value);

            var rows = CardElementOf(rewritten).GetProperty("rows");
            Assert.Equal(3, rows.GetArrayLength());
            Assert.Equal("one", rows[0].GetProperty("label").GetString());
            Assert.Equal("corrupt", rows[1].GetString());
            Assert.Equal("three", rows[2].GetProperty("label").GetString());
        }
    }
}
```

- [ ] **Step 2: Run the round-trip tests**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetRoundTripTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 9`.

If any test fails, the read/write pair from Tasks 3 and 4 has a hole. Fix it in `CheatsheetJsonNoteSerialize.cs` by widening the "not consumed → preserved verbatim" rule; never by relaxing the assertion. The two failure shapes to expect:
- a key present in the input but missing from the output → a read helper consumed it without a typed home, or `CopyExtras` skipped it;
- a key present with a changed value → a fabricated default overwrote a preserved extra, meaning `CopyExtras` ran before the typed writes instead of after.

- [ ] **Step 3: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add src/Hmm.Cheatsheet.Tests src/Hmm.Cheatsheet
git commit -m "test(cheatsheet): pin the lossless note round-trip contract"
```

---

### Task 6: `CheatsheetValidator`

Card-level rules only. Rows are deliberately **not** validated: rejecting a card because of a row this version cannot model is the failure mode the whole design exists to prevent.

**Files:**
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/Validator/CheatsheetValidator.cs`
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetValidatorTests.cs`

**Interfaces:**
- Consumes: `CheatsheetCard` (Task 1); `Hmm.Utility.Validation.ValidatorBase<T>` (a FluentValidation `AbstractValidator<T>` that also implements `IHmmValidator<T>`, exposing `Task<ProcessingResult<T>> ValidateEntityAsync(T entity)`; it throws `ArgumentNullException` on a null entity); `IEntityLookup.GetEntityAsync<T>(int id) where T : Entity`.
- Produces: `Hmm.Cheatsheet.Validator.CheatsheetValidator(IEntityLookup lookupRepo)` implementing `IHmmValidator<CheatsheetCard>`.

- [ ] **Step 1: Write the failing test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetValidatorTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.Validator;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetValidatorTests
    {
        private static CheatsheetValidator CreateValidator(bool authorExists = true)
        {
            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntityAsync<Author>(It.IsAny<int>()))
                .ReturnsAsync(authorExists
                    ? ProcessingResult<Author>.Ok(new Author { Id = 9 })
                    : ProcessingResult<Author>.NotFound());

            return new CheatsheetValidator(lookup.Object);
        }

        private static CheatsheetCard ValidCard() => new()
        {
            AuthorId = 9,
            Id = "card-1",
            Title = "Passport",
            WalletGroup = "Travel",
            TemplateId = "blank"
        };

        [Fact]
        public async Task ValidateEntityAsync_ValidCard_Succeeds()
        {
            var result = await CreateValidator().ValidateEntityAsync(ValidCard());

            Assert.True(result.Success);
        }

        [Fact]
        public async Task ValidateEntityAsync_MissingAuthor_Fails()
        {
            var result = await CreateValidator(authorExists: false).ValidateEntityAsync(ValidCard());

            Assert.False(result.Success);
            Assert.Contains("author", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateEntityAsync_EmptyId_Fails()
        {
            var card = ValidCard();
            card.Id = string.Empty;

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
            Assert.Contains("card id is required", result.GetWholeMessage(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateEntityAsync_EmptyTitle_Fails()
        {
            var card = ValidCard();
            card.Title = string.Empty;

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ValidateEntityAsync_OverlongTitle_Fails()
        {
            var card = ValidCard();
            card.Title = new string('x', 201);

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ValidateEntityAsync_EmptyWalletGroup_Fails()
        {
            var card = ValidCard();
            card.WalletGroup = string.Empty;

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ValidateEntityAsync_EmptyTemplateId_Fails()
        {
            var card = ValidCard();
            card.TemplateId = string.Empty;

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.False(result.Success);
        }

        [Fact]
        public async Task ValidateEntityAsync_UnreadableRowsAndExtras_StillSucceeds()
        {
            // The whole point: rows this version cannot model must not make the
            // card unsaveable, or the API becomes the thing that deletes them.
            using var document = JsonDocument.Parse("\"corrupt\"");
            var card = ValidCard();
            card.Rows = new List<CheatsheetRow>
            {
                new() { RawJson = document.RootElement.Clone() },
                new() { Label = string.Empty, ValueAction = "sms" }
            };
            card.ExtraFields["future"] = document.RootElement.Clone();

            var result = await CreateValidator().ValidateEntityAsync(card);

            Assert.True(result.Success);
        }

        [Fact]
        public void Constructor_Throws_WhenLookupIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new CheatsheetValidator(null));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetValidatorTests"`

Expected: FAIL — build error `CS0246: The type or namespace name 'CheatsheetValidator' could not be found`.

- [ ] **Step 3: Write the validator**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/Validator/CheatsheetValidator.cs`:

```csharp
using System;
using System.Threading.Tasks;
using FluentValidation;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Validation;

namespace Hmm.Cheatsheet.Validator
{
    /// <summary>
    /// Card-level rules only.
    ///
    /// There are deliberately NO rules on Rows, ExtraFields or RawJson. The
    /// client keeps rows it cannot parse so a save cannot destroy them; a
    /// validator that rejected such a card would turn this API into the thing
    /// that deletes them. Anything the serializer preserved is, by definition,
    /// valid enough to store.
    /// </summary>
    public class CheatsheetValidator : ValidatorBase<CheatsheetCard>
    {
        private readonly IEntityLookup _lookupRepo;

        public CheatsheetValidator(IEntityLookup lookupRepo)
        {
            ArgumentNullException.ThrowIfNull(lookupRepo);
            _lookupRepo = lookupRepo;

            RuleFor(c => c.AuthorId)
                .MustAsync(async (id, _) => await HasValidAuthor(id))
                .WithMessage("Have valid author for cheatsheet card");

            RuleFor(c => c.Id)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Cheatsheet card id is required and must be 100 characters or less");

            RuleFor(c => c.Title)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Cheatsheet title is required and must be 200 characters or less");

            RuleFor(c => c.WalletGroup)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Wallet group is required and must be 100 characters or less");

            RuleFor(c => c.TemplateId)
                .NotEmpty()
                .MaximumLength(100)
                .WithMessage("Template id is required and must be 100 characters or less");
        }

        private async Task<bool> HasValidAuthor(int authorId)
        {
            if (authorId <= 0)
            {
                return false;
            }

            var savedAuthorResult = await _lookupRepo.GetEntityAsync<Author>(authorId);
            return savedAuthorResult.Success && savedAuthorResult.Value != null;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetValidatorTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 9`.

- [ ] **Step 5: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add src/Hmm.Cheatsheet src/Hmm.Cheatsheet.Tests
git commit -m "feat(cheatsheet): add card validator with no row-level rules"
```

---

### Task 7: `ICheatsheetManager` / `CheatsheetManager` — read operations

Loads every card in the catalog for the current author, filters by `walletGroup`/`tag`, orders deterministically, and pages in memory. Also finds a card's note **by subject**, which is what makes a card with unreadable content still addressable.

**Files:**
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/ICheatsheetManager.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/CheatsheetManager.cs`
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetManagerReadTests.cs`

**Interfaces:**
- Consumes: `INoteSerializer<CheatsheetCard>` (`Task<ProcessingResult<CheatsheetCard>> GetEntity(HmmNote)`, `Task<ProcessingResult<HmmNote>> GetNote(in CheatsheetCard)`); `IHmmValidator<CheatsheetCard>`; `Hmm.Core.IHmmNoteManager` (`GetNotesAsync(Expression<Func<HmmNote,bool>>, bool includeDeleted, ResourceCollectionParameters)`, `CreateAsync(HmmNote, bool)`, `UpdateAsync(HmmNote, bool)`, `DeleteAsync(int)` → `ProcessingResult<Unit>`); `IEntityLookup`; `Hmm.Core.IAuthorProvider` (`Task<ProcessingResult<Author>> GetAuthorAsync()`, `Author CachedAuthor`); `ResourceCollectionParameters.GetPaginationTuple()` from `Hmm.Utility.Dal.Query`.
- Produces: `Hmm.Cheatsheet.ICheatsheetManager` with `Task<ProcessingResult<PageList<CheatsheetCard>>> GetCardsAsync(string walletGroup = null, string tag = null, ResourceCollectionParameters resourceCollectionParameters = null)` and `Task<ProcessingResult<CheatsheetCard>> GetCardByIdAsync(string cardId)`; `Hmm.Cheatsheet.CheatsheetManager(INoteSerializer<CheatsheetCard>, IHmmValidator<CheatsheetCard>, IHmmNoteManager, IEntityLookup, IAuthorProvider)`.

- [ ] **Step 1: Write the failing test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetManagerReadTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.NoteSerialize;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetManagerReadTests
    {
        private static readonly Author TestAuthor = new() { Id = 9, AccountName = "tester" };

        private static readonly NoteCatalog TestCatalog = new()
        {
            Id = 7,
            Name = CheatsheetConstant.CheatsheetCatalogName,
            Schema = "{}"
        };

        private static string ContentFor(
            string cardId,
            string title,
            string walletGroup,
            params string[] tags)
        {
            var tagJson = string.Join(",", tags.Select(t => "\"" + t + "\""));
            return "{\"note\":{\"content\":{\"Cheatsheet\":{" +
                   "\"schemaVersion\":1,\"id\":\"" + cardId + "\",\"title\":\"" + title + "\"," +
                   "\"walletGroup\":\"" + walletGroup + "\",\"tags\":[" + tagJson + "]," +
                   "\"templateId\":\"blank\",\"protected\":false,\"rows\":[]}}}}";
        }

        private static HmmNote NoteFor(int id, string cardId, string title, string walletGroup, params string[] tags)
            => new()
            {
                Id = id,
                Uuid = "uuid-" + id,
                Subject = CheatsheetCard.GetNoteSubject(cardId),
                Content = ContentFor(cardId, title, walletGroup, tags),
                Author = TestAuthor,
                Catalog = TestCatalog
            };

        private static CheatsheetJsonNoteSerialize CreateSerializer()
        {
            var catalogProvider = new Mock<ICheatsheetCatalogProvider>();
            catalogProvider.Setup(p => p.GetCatalogAsync()).ReturnsAsync(TestCatalog);
            return new CheatsheetJsonNoteSerialize(catalogProvider.Object, NullLogger<CheatsheetCard>.Instance);
        }

        private static Mock<IEntityLookup> CreateLookup()
        {
            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(
                    new PageList<NoteCatalog>(new[] { TestCatalog }, 1, 1, 10)));
            lookup
                .Setup(l => l.GetEntityAsync<Author>(It.IsAny<int>()))
                .ReturnsAsync(ProcessingResult<Author>.Ok(TestAuthor));
            return lookup;
        }

        private static Mock<IAuthorProvider> CreateAuthorProvider()
        {
            var provider = new Mock<IAuthorProvider>();
            provider.Setup(p => p.GetAuthorAsync()).ReturnsAsync(ProcessingResult<Author>.Ok(TestAuthor));
            provider.Setup(p => p.CachedAuthor).Returns(TestAuthor);
            return provider;
        }

        /// <summary>
        /// Serves the given notes through the paging loop the manager drives,
        /// honouring PageNumber / PageSize so pagination bugs surface.
        /// </summary>
        private static Mock<IHmmNoteManager> CreateNoteManager(IList<HmmNote> notes)
        {
            var noteManager = new Mock<IHmmNoteManager>();
            noteManager
                .Setup(m => m.GetNotesAsync(
                    It.IsAny<Expression<Func<HmmNote, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync((Expression<Func<HmmNote, bool>> _, bool __, ResourceCollectionParameters parameters) =>
                {
                    var (pageIndex, pageSize) = parameters.GetPaginationTuple();
                    var items = notes.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                    return ProcessingResult<PageList<HmmNote>>.Ok(
                        new PageList<HmmNote>(items, notes.Count, pageIndex, pageSize));
                });
            return noteManager;
        }

        private static CheatsheetManager CreateManager(IList<HmmNote> notes)
            => new(
                CreateSerializer(),
                Mock.Of<IHmmValidator<CheatsheetCard>>(),
                CreateNoteManager(notes).Object,
                CreateLookup().Object,
                CreateAuthorProvider().Object);

        private static IList<HmmNote> SampleNotes() =>
        [
            NoteFor(1, "c-1", "Passport", "Travel", "trip", "id"),
            NoteFor(2, "c-2", "Alarm code", "Home", "security"),
            NoteFor(3, "c-3", "Bike lock", "Home", "trip")
        ];

        [Fact]
        public async Task GetCardsAsync_ReturnsEveryCard()
        {
            var result = await CreateManager(SampleNotes()).GetCardsAsync();

            Assert.True(result.Success);
            Assert.Equal(3, result.Value.TotalCount);
            Assert.Equal(3, result.Value.Count);
        }

        [Fact]
        public async Task GetCardsAsync_OrdersByTitleThenId()
        {
            var result = await CreateManager(SampleNotes()).GetCardsAsync();

            Assert.True(result.Success);
            Assert.Equal(new[] { "Alarm code", "Bike lock", "Passport" }, result.Value.Select(c => c.Title));
        }

        [Fact]
        public async Task GetCardsAsync_FiltersByWalletGroup_CaseInsensitively()
        {
            var result = await CreateManager(SampleNotes()).GetCardsAsync(walletGroup: "home");

            Assert.True(result.Success);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.All(result.Value, c => Assert.Equal("Home", c.WalletGroup));
        }

        [Fact]
        public async Task GetCardsAsync_FiltersByTag_CaseInsensitively()
        {
            var result = await CreateManager(SampleNotes()).GetCardsAsync(tag: "TRIP");

            Assert.True(result.Success);
            Assert.Equal(new[] { "Bike lock", "Passport" }, result.Value.Select(c => c.Title));
        }

        [Fact]
        public async Task GetCardsAsync_CombinesBothFilters()
        {
            var result = await CreateManager(SampleNotes()).GetCardsAsync(walletGroup: "Home", tag: "trip");

            Assert.True(result.Success);
            Assert.Equal("Bike lock", Assert.Single(result.Value).Title);
        }

        [Fact]
        public async Task GetCardsAsync_PagesTheFilteredSet()
        {
            var parameters = new ResourceCollectionParameters { PageNumber = 2, PageSize = 2 };

            var result = await CreateManager(SampleNotes()).GetCardsAsync(resourceCollectionParameters: parameters);

            Assert.True(result.Success);
            Assert.Equal(3, result.Value.TotalCount);
            Assert.Equal(2, result.Value.CurrentPage);
            Assert.Equal("Passport", Assert.Single(result.Value).Title);
        }

        [Fact]
        public async Task GetCardsAsync_ReadsEveryNotePage()
        {
            // 250 notes with a 100-note page size means the manager must loop
            // three times; a single-page read would silently hide cards.
            var notes = Enumerable.Range(1, 250)
                .Select(i => NoteFor(i, "c-" + i, "Card " + i.ToString("D3"), "Home"))
                .ToList();

            var result = await CreateManager(notes).GetCardsAsync(
                resourceCollectionParameters: new ResourceCollectionParameters { PageNumber = 1, PageSize = 100 });

            Assert.True(result.Success);
            Assert.Equal(250, result.Value.TotalCount);
        }

        [Fact]
        public async Task GetCardsAsync_SkipsUndeserializableNotes_WithoutFailing()
        {
            var notes = SampleNotes();
            notes.Add(new HmmNote
            {
                Id = 99,
                Subject = "Cheatsheet:broken",
                Content = "{not json",
                Author = TestAuthor,
                Catalog = TestCatalog
            });

            var result = await CreateManager(notes).GetCardsAsync();

            Assert.True(result.Success);
            Assert.Equal(3, result.Value.TotalCount);
        }

        [Fact]
        public async Task GetCardsAsync_Fails_WhenCatalogMissing()
        {
            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(
                    new PageList<NoteCatalog>(Array.Empty<NoteCatalog>(), 0, 1, 10)));

            var manager = new CheatsheetManager(
                CreateSerializer(),
                Mock.Of<IHmmValidator<CheatsheetCard>>(),
                CreateNoteManager(SampleNotes()).Object,
                lookup.Object,
                CreateAuthorProvider().Object);

            var result = await manager.GetCardsAsync();

            Assert.False(result.Success);
            Assert.Contains(CheatsheetConstant.CheatsheetCatalogName, result.ErrorMessage);
        }

        [Fact]
        public async Task GetCardByIdAsync_ReturnsTheCard()
        {
            var result = await CreateManager(SampleNotes()).GetCardByIdAsync("c-2");

            Assert.True(result.Success);
            Assert.Equal("Alarm code", result.Value.Title);
            Assert.Equal(2, result.Value.NoteId);
        }

        [Fact]
        public async Task GetCardByIdAsync_MatchesOnSubject_NotOnDecodedContent()
        {
            // The content is unreadable, so the card can only be found by
            // subject - which is exactly what keeps it deletable and fixable.
            var notes = SampleNotes();
            notes.Add(new HmmNote
            {
                Id = 99,
                Subject = CheatsheetCard.GetNoteSubject("c-broken"),
                Content = "{not json",
                Author = TestAuthor,
                Catalog = TestCatalog
            });

            var result = await CreateManager(notes).GetCardByIdAsync("c-broken");

            // The note was found; only deserialization failed.
            Assert.False(result.Success);
            Assert.Contains("Invalid JSON format", result.ErrorMessage);
        }

        [Fact]
        public async Task GetCardByIdAsync_NotFound_WhenNoSubjectMatches()
        {
            var result = await CreateManager(SampleNotes()).GetCardByIdAsync("nope");

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task GetCardByIdAsync_EmptyId_IsInvalid()
        {
            var result = await CreateManager(SampleNotes()).GetCardByIdAsync("  ");

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetManagerReadTests"`

Expected: FAIL — build error `CS0246: The type or namespace name 'CheatsheetManager' could not be found`.

- [ ] **Step 3: Write the interface (read half)**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/ICheatsheetManager.cs`:

```csharp
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;

namespace Hmm.Cheatsheet
{
    /// <summary>
    /// CRUD over cheatsheet cards. Every card is one HmmNote under the
    /// Hmm.CheatsheetMan.Cheatsheet catalog, addressed by the note subject
    /// "Cheatsheet:{cardId}".
    /// </summary>
    public interface ICheatsheetManager
    {
        /// <summary>
        /// Returns the current author's cards, optionally narrowed by wallet
        /// group and/or tag (both case-insensitive), ordered by title then id,
        /// and paged.
        /// </summary>
        Task<ProcessingResult<PageList<CheatsheetCard>>> GetCardsAsync(
            string walletGroup = null,
            string tag = null,
            ResourceCollectionParameters resourceCollectionParameters = null);

        /// <summary>
        /// Returns one card by its stable card id (not the note's int id).
        /// </summary>
        Task<ProcessingResult<CheatsheetCard>> GetCardByIdAsync(string cardId);
    }
}
```

- [ ] **Step 4: Write the manager (read half)**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/CheatsheetManager.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;

namespace Hmm.Cheatsheet
{
    /// <summary>
    /// Cheatsheet CRUD over the note store.
    ///
    /// Card content is opaque JSON in a text column, so wallet-group and tag
    /// filtering cannot be pushed into SQL, and paginating notes before
    /// deserializing them would page the wrong population. Every read therefore
    /// loads the author's cards for the catalog and filters/pages in memory.
    /// Wallets hold tens of cards; correctness wins over the round trip.
    /// </summary>
    public class CheatsheetManager : ICheatsheetManager
    {
        /// <summary>Note-store page size for the internal read-everything loop.</summary>
        private const int NotePageSize = 100;

        private readonly INoteSerializer<CheatsheetCard> _noteSerializer;
        private readonly IHmmValidator<CheatsheetCard> _validator;
        private readonly IHmmNoteManager _noteManager;
        private readonly IEntityLookup _lookupRepo;
        private readonly IAuthorProvider _authorProvider;

        public CheatsheetManager(
            INoteSerializer<CheatsheetCard> noteSerializer,
            IHmmValidator<CheatsheetCard> validator,
            IHmmNoteManager noteManager,
            IEntityLookup lookupRepo,
            IAuthorProvider authorProvider)
        {
            ArgumentNullException.ThrowIfNull(noteSerializer);
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(noteManager);
            ArgumentNullException.ThrowIfNull(lookupRepo);
            ArgumentNullException.ThrowIfNull(authorProvider);

            _noteSerializer = noteSerializer;
            _validator = validator;
            _noteManager = noteManager;
            _lookupRepo = lookupRepo;
            _authorProvider = authorProvider;
        }

        public async Task<ProcessingResult<PageList<CheatsheetCard>>> GetCardsAsync(
            string walletGroup = null,
            string tag = null,
            ResourceCollectionParameters resourceCollectionParameters = null)
        {
            var cardsResult = await LoadCardsAsync();
            if (!cardsResult.Success)
            {
                return ProcessingResult<PageList<CheatsheetCard>>.Fail(
                    cardsResult.ErrorMessage, cardsResult.ErrorType);
            }

            IEnumerable<CheatsheetCard> cards = cardsResult.Value;

            if (!string.IsNullOrWhiteSpace(walletGroup))
            {
                cards = cards.Where(c =>
                    string.Equals(c.WalletGroup, walletGroup, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                cards = cards.Where(c => c.Tags != null && c.Tags.Any(t =>
                    string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)));
            }

            // Deterministic order: the wallet has no user reordering, so title
            // then id is the whole ordering contract.
            var ordered = cards
                .OrderBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Id, StringComparer.Ordinal)
                .ToList();

            var (pageIndex, pageSize) = resourceCollectionParameters.GetPaginationTuple();
            var pageItems = ordered.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            return ProcessingResult<PageList<CheatsheetCard>>.Ok(
                new PageList<CheatsheetCard>(pageItems, ordered.Count, pageIndex, pageSize));
        }

        public async Task<ProcessingResult<CheatsheetCard>> GetCardByIdAsync(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return ProcessingResult<CheatsheetCard>.Invalid("Cheatsheet card id cannot be empty");
            }

            var noteResult = await FindNoteForCardAsync(cardId);
            if (!noteResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            return await _noteSerializer.GetEntity(noteResult.Value);
        }

        /// <summary>
        /// Finds a card's note by SUBJECT, never by decoding its content. The
        /// subject is the card's identity and stays readable when the content
        /// does not - matching on a decoded id would make a card with broken
        /// JSON invisible, so a save would create a duplicate note under the
        /// same subject and a delete could never reach the original.
        /// </summary>
        private async Task<ProcessingResult<HmmNote>> FindNoteForCardAsync(string cardId)
        {
            var notesResult = await GetAllNotesAsync();
            if (!notesResult.Success)
            {
                return ProcessingResult<HmmNote>.Fail(notesResult.ErrorMessage, notesResult.ErrorType);
            }

            var subject = CheatsheetCard.GetNoteSubject(cardId);
            var note = notesResult.Value.FirstOrDefault(n =>
                string.Equals(n.Subject, subject, StringComparison.Ordinal));

            return note != null
                ? ProcessingResult<HmmNote>.Ok(note)
                : ProcessingResult<HmmNote>.NotFound($"Cannot find cheatsheet card '{cardId}'");
        }

        private async Task<ProcessingResult<IList<CheatsheetCard>>> LoadCardsAsync()
        {
            var notesResult = await GetAllNotesAsync();
            if (!notesResult.Success)
            {
                return ProcessingResult<IList<CheatsheetCard>>.Fail(
                    notesResult.ErrorMessage, notesResult.ErrorType);
            }

            var cards = new List<CheatsheetCard>();
            foreach (var note in notesResult.Value)
            {
                var cardResult = await _noteSerializer.GetEntity(note);
                if (cardResult.Success && cardResult.Value != null)
                {
                    // A single unreadable note must not take the wallet down
                    // with it; it stays reachable by id for repair or delete.
                    cards.Add(cardResult.Value);
                }
            }

            return ProcessingResult<IList<CheatsheetCard>>.Ok(cards);
        }

        /// <summary>
        /// Pages until exhausted. A fixed ceiling here would silently hide
        /// cards - the user would simply never see them again.
        /// </summary>
        private async Task<ProcessingResult<IList<HmmNote>>> GetAllNotesAsync()
        {
            var authorResult = await _authorProvider.GetAuthorAsync();
            if (!authorResult.Success)
            {
                return ProcessingResult<IList<HmmNote>>.Fail(
                    authorResult.ErrorMessage, authorResult.ErrorType);
            }

            var author = authorResult.Value;
            var catalogId = await GetCatalogIdAsync();
            if (catalogId <= 0)
            {
                return ProcessingResult<IList<HmmNote>>.Fail(
                    $"Cannot find note catalog '{CheatsheetConstant.CheatsheetCatalogName}'",
                    ErrorCategory.NotFound);
            }

            var notes = new List<HmmNote>();
            var page = 1;

            while (true)
            {
                var parameters = new ResourceCollectionParameters
                {
                    PageNumber = page,
                    PageSize = NotePageSize
                };

                var pageResult = await _noteManager.GetNotesAsync(
                    n => n.Author.Id == author.Id && n.Catalog.Id == catalogId,
                    false,
                    parameters);

                if (!pageResult.Success)
                {
                    return ProcessingResult<IList<HmmNote>>.Fail(
                        pageResult.ErrorMessage, pageResult.ErrorType);
                }

                var pageList = pageResult.Value;
                if (pageList == null || pageList.Count == 0)
                {
                    break;
                }

                notes.AddRange(pageList);

                if (page >= pageList.TotalPages)
                {
                    break;
                }

                page++;
            }

            return ProcessingResult<IList<HmmNote>>.Ok(notes);
        }

        private async Task<int> GetCatalogIdAsync()
        {
            var catalogsResult = await _lookupRepo.GetEntitiesAsync<NoteCatalog>(
                c => c.Name == CheatsheetConstant.CheatsheetCatalogName);

            if (!catalogsResult.Success || catalogsResult.Value == null)
            {
                return 0;
            }

            return catalogsResult.Value.FirstOrDefault()?.Id ?? 0;
        }
    }
}
```

Note: `_validator` is unused until Task 8; the compiler emits no warning for an assigned private field that is read nowhere in this file yet, and Task 8 adds its only use.

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetManagerReadTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 13`.

- [ ] **Step 6: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add src/Hmm.Cheatsheet src/Hmm.Cheatsheet.Tests
git commit -m "feat(cheatsheet): add cheatsheet manager read operations"
```

---

### Task 8: `CheatsheetManager` — create, update, delete

**Files:**
- Modify: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/ICheatsheetManager.cs`
- Modify: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/CheatsheetManager.cs`
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetManagerWriteTests.cs`

**Interfaces:**
- Consumes: everything from Task 7, plus `IHmmValidator<CheatsheetCard>.ValidateEntityAsync`.
- Produces, added to `ICheatsheetManager`:
  - `Task<ProcessingResult<CheatsheetCard>> CreateAsync(CheatsheetCard card, bool commitChanges = true)`
  - `Task<ProcessingResult<CheatsheetCard>> UpdateAsync(CheatsheetCard card, bool commitChanges = true)`
  - `Task<ProcessingResult<Unit>> DeleteAsync(string cardId)`

- [ ] **Step 1: Write the failing test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet.Tests/CheatsheetManagerWriteTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.NoteSerialize;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Hmm.Cheatsheet.Tests
{
    public class CheatsheetManagerWriteTests
    {
        private static readonly Author TestAuthor = new() { Id = 9, AccountName = "tester" };

        private static readonly NoteCatalog TestCatalog = new()
        {
            Id = 7,
            Name = CheatsheetConstant.CheatsheetCatalogName,
            Schema = "{}"
        };

        private readonly List<HmmNote> _notes = [];
        private readonly Mock<IHmmNoteManager> _noteManager = new();
        private readonly Mock<IHmmValidator<CheatsheetCard>> _validator = new();
        private readonly CheatsheetManager _manager;

        public CheatsheetManagerWriteTests()
        {
            _noteManager
                .Setup(m => m.GetNotesAsync(
                    It.IsAny<Expression<Func<HmmNote, bool>>>(),
                    It.IsAny<bool>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync((Expression<Func<HmmNote, bool>> _, bool __, ResourceCollectionParameters parameters) =>
                {
                    var (pageIndex, pageSize) = parameters.GetPaginationTuple();
                    var items = _notes.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
                    return ProcessingResult<PageList<HmmNote>>.Ok(
                        new PageList<HmmNote>(items, _notes.Count, pageIndex, pageSize));
                });

            _noteManager
                .Setup(m => m.CreateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()))
                .ReturnsAsync((HmmNote note, bool _) =>
                {
                    note.Id = _notes.Count + 1;
                    note.Uuid ??= "uuid-" + note.Id;
                    note.Author = TestAuthor;
                    _notes.Add(note);
                    return ProcessingResult<HmmNote>.Ok(note);
                });

            _noteManager
                .Setup(m => m.UpdateAsync(It.IsAny<HmmNote>(), It.IsAny<bool>()))
                .ReturnsAsync((HmmNote note, bool _) =>
                {
                    var index = _notes.FindIndex(n => n.Id == note.Id);
                    if (index >= 0)
                    {
                        _notes[index] = note;
                    }

                    return ProcessingResult<HmmNote>.Ok(note);
                });

            _noteManager
                .Setup(m => m.DeleteAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    _notes.RemoveAll(n => n.Id == id);
                    return ProcessingResult<Unit>.Ok(Unit.Value);
                });

            _validator
                .Setup(v => v.ValidateEntityAsync(It.IsAny<CheatsheetCard>()))
                .ReturnsAsync((CheatsheetCard card) => ProcessingResult<CheatsheetCard>.Ok(card));

            var lookup = new Mock<IEntityLookup>();
            lookup
                .Setup(l => l.GetEntitiesAsync(
                    It.IsAny<Expression<Func<NoteCatalog, bool>>>(),
                    It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<NoteCatalog>>.Ok(
                    new PageList<NoteCatalog>(new[] { TestCatalog }, 1, 1, 10)));

            var authorProvider = new Mock<IAuthorProvider>();
            authorProvider.Setup(p => p.GetAuthorAsync()).ReturnsAsync(ProcessingResult<Author>.Ok(TestAuthor));
            authorProvider.Setup(p => p.CachedAuthor).Returns(TestAuthor);

            var catalogProvider = new Mock<ICheatsheetCatalogProvider>();
            catalogProvider.Setup(p => p.GetCatalogAsync()).ReturnsAsync(TestCatalog);

            _manager = new CheatsheetManager(
                new CheatsheetJsonNoteSerialize(catalogProvider.Object, NullLogger<CheatsheetCard>.Instance),
                _validator.Object,
                _noteManager.Object,
                lookup.Object,
                authorProvider.Object);
        }

        private static CheatsheetCard NewCard(string id = "c-1") => new()
        {
            Id = id,
            Title = "Passport",
            WalletGroup = "Travel",
            TemplateId = "blank",
            Tags = new List<string> { "trip" }
        };

        [Fact]
        public async Task CreateAsync_StoresTheCardUnderTheSubjectIdentity()
        {
            var result = await _manager.CreateAsync(NewCard());

            Assert.True(result.Success);
            Assert.Equal("c-1", result.Value.Id);
            Assert.Equal("Passport", result.Value.Title);
            Assert.Equal(9, result.Value.AuthorId);
            var note = Assert.Single(_notes);
            Assert.Equal("Cheatsheet:c-1", note.Subject);
            Assert.Equal(TestAuthor, note.Author);
            Assert.Equal(TestCatalog, note.Catalog);
        }

        [Fact]
        public async Task CreateAsync_MintsAnId_WhenTheClientOmitsOne()
        {
            var card = NewCard();
            card.Id = string.Empty;

            var result = await _manager.CreateAsync(card);

            Assert.True(result.Success);
            Assert.True(Guid.TryParse(result.Value.Id, out _));
        }

        [Fact]
        public async Task CreateAsync_NullCard_IsInvalid()
        {
            var result = await _manager.CreateAsync(null);

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task CreateAsync_ValidationFailure_IsReported()
        {
            _validator
                .Setup(v => v.ValidateEntityAsync(It.IsAny<CheatsheetCard>()))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.Invalid("Title is required"));

            var result = await _manager.CreateAsync(NewCard());

            Assert.False(result.Success);
            Assert.Contains("Title is required", result.GetWholeMessage());
            Assert.Empty(_notes);
        }

        [Fact]
        public async Task CreateAsync_DuplicateCardId_Conflicts()
        {
            await _manager.CreateAsync(NewCard());

            var result = await _manager.CreateAsync(NewCard());

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.Conflict, result.ErrorType);
            Assert.Single(_notes);
        }

        [Fact]
        public async Task UpdateAsync_RewritesTheCardContent()
        {
            await _manager.CreateAsync(NewCard());
            var updated = NewCard();
            updated.Title = "Renewed passport";
            updated.WalletGroup = "Documents";

            var result = await _manager.UpdateAsync(updated);

            Assert.True(result.Success);
            Assert.Equal("Renewed passport", result.Value.Title);
            Assert.Equal("Documents", result.Value.WalletGroup);
            Assert.Single(_notes);
        }

        [Fact]
        public async Task UpdateAsync_CarriesTheStoredNoteIdentityForward()
        {
            // HmmNoteManager.UpdateAsync mints a fresh Uuid when the incoming
            // note has none, and the serializer builds a brand new note - so
            // without an explicit carry-forward the card would lose its
            // cross-device identity on every single save.
            await _manager.CreateAsync(NewCard());
            var stored = _notes.Single();
            stored.Uuid = "stable-uuid";
            stored.CreateDate = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            stored.NoteDate = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            stored.Version = [1, 2, 3];

            var updated = NewCard();
            updated.Title = "Changed";
            await _manager.UpdateAsync(updated);

            var note = _notes.Single();
            Assert.Equal("stable-uuid", note.Uuid);
            Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), note.CreateDate);
            Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), note.NoteDate);
            Assert.Equal(new byte[] { 1, 2, 3 }, note.Version);
        }

        [Fact]
        public async Task UpdateAsync_UnknownCard_IsNotFound()
        {
            var result = await _manager.UpdateAsync(NewCard("missing"));

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task UpdateAsync_NullCard_IsInvalid()
        {
            var result = await _manager.UpdateAsync(null);

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task UpdateAsync_EmptyCardId_IsInvalid()
        {
            var card = NewCard();
            card.Id = "  ";

            var result = await _manager.UpdateAsync(card);

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task DeleteAsync_RemovesTheBackingNote()
        {
            await _manager.CreateAsync(NewCard());

            var result = await _manager.DeleteAsync("c-1");

            Assert.True(result.Success);
            Assert.Empty(_notes);
            _noteManager.Verify(m => m.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_UnknownCard_IsNotFound()
        {
            var result = await _manager.DeleteAsync("missing");

            Assert.False(result.Success);
            Assert.True(result.IsNotFound);
        }

        [Fact]
        public async Task DeleteAsync_EmptyCardId_IsInvalid()
        {
            var result = await _manager.DeleteAsync(" ");

            Assert.False(result.Success);
            Assert.Equal(ErrorCategory.ValidationError, result.ErrorType);
        }

        [Fact]
        public async Task CreateThenReadBack_PreservesUnknownData()
        {
            using var document = System.Text.Json.JsonDocument.Parse("{\"nested\":[1,2]}");
            var card = NewCard();
            card.ExtraFields["future"] = document.RootElement.Clone();
            card.Rows = new List<CheatsheetRow>
            {
                new() { RawJson = System.Text.Json.JsonDocument.Parse("\"corrupt\"").RootElement.Clone() }
            };

            await _manager.CreateAsync(card);
            var result = await _manager.GetCardByIdAsync("c-1");

            Assert.True(result.Success);
            Assert.Equal("{\"nested\":[1,2]}", result.Value.ExtraFields["future"].GetRawText());
            Assert.Equal("\"corrupt\"", Assert.Single(result.Value.Rows).RawJson.Value.GetRawText());
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetManagerWriteTests"`

Expected: FAIL — build errors `CS1061: 'CheatsheetManager' does not contain a definition for 'CreateAsync'` (and the same for `UpdateAsync`, `DeleteAsync`).

- [ ] **Step 3: Extend the interface**

Add these members to `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/ICheatsheetManager.cs`, inside the interface, after `GetCardByIdAsync`:

```csharp
        /// <summary>
        /// Stores a new card. A blank <see cref="CheatsheetCard.Id"/> is filled
        /// with a fresh v4 GUID; a card id that already exists conflicts rather
        /// than creating a second note under the same subject.
        /// </summary>
        Task<ProcessingResult<CheatsheetCard>> CreateAsync(CheatsheetCard card, bool commitChanges = true);

        /// <summary>
        /// Replaces the stored card. The backing note's cross-device identity
        /// (Uuid), audit dates and concurrency token are carried forward.
        /// </summary>
        Task<ProcessingResult<CheatsheetCard>> UpdateAsync(CheatsheetCard card, bool commitChanges = true);

        /// <summary>Soft-deletes the card's backing note.</summary>
        Task<ProcessingResult<Unit>> DeleteAsync(string cardId);
```

- [ ] **Step 4: Implement the write operations**

Add these methods to `/Users/fchy/Projects/Hmm/src/Hmm.Cheatsheet/CheatsheetManager.cs`, immediately after `GetCardByIdAsync` and before `FindNoteForCardAsync`:

```csharp
        public async Task<ProcessingResult<CheatsheetCard>> CreateAsync(
            CheatsheetCard card,
            bool commitChanges = true)
        {
            if (card == null)
            {
                return ProcessingResult<CheatsheetCard>.Invalid("Cheatsheet card cannot be null");
            }

            var authorResult = await _authorProvider.GetAuthorAsync();
            if (!authorResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(
                    authorResult.ErrorMessage, authorResult.ErrorType);
            }

            if (string.IsNullOrWhiteSpace(card.Id))
            {
                // Guid format (8-4-4-4-12) matches the Dart uuid package's v4
                // output, so client- and server-minted ids are indistinguishable.
                card.Id = Guid.NewGuid().ToString();
            }

            card.AuthorId = authorResult.Value.Id;

            var validationResult = await _validator.ValidateEntityAsync(card);
            if (!validationResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Invalid(validationResult.GetWholeMessage());
            }

            var existingResult = await FindNoteForCardAsync(card.Id);
            if (existingResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Conflict(
                    $"Cheatsheet card '{card.Id}' already exists");
            }

            var noteResult = await _noteSerializer.GetNote(card);
            if (!noteResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Id = 0;
            note.Author = authorResult.Value;

            var createdResult = await _noteManager.CreateAsync(note, commitChanges);
            if (!createdResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(
                    createdResult.ErrorMessage, createdResult.ErrorType);
            }

            return await ReadBackAsync(createdResult.Value, authorResult.Value.Id);
        }

        public async Task<ProcessingResult<CheatsheetCard>> UpdateAsync(
            CheatsheetCard card,
            bool commitChanges = true)
        {
            if (card == null)
            {
                return ProcessingResult<CheatsheetCard>.Invalid("Cheatsheet card cannot be null");
            }

            if (string.IsNullOrWhiteSpace(card.Id))
            {
                return ProcessingResult<CheatsheetCard>.Invalid("Cheatsheet card id cannot be empty");
            }

            var authorResult = await _authorProvider.GetAuthorAsync();
            if (!authorResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(
                    authorResult.ErrorMessage, authorResult.ErrorType);
            }

            var existingNoteResult = await FindNoteForCardAsync(card.Id);
            if (!existingNoteResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.NotFound(
                    $"Cannot find cheatsheet card '{card.Id}'");
            }

            var existingNote = existingNoteResult.Value;
            card.AuthorId = authorResult.Value.Id;
            card.NoteId = existingNote.Id;

            var validationResult = await _validator.ValidateEntityAsync(card);
            if (!validationResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Invalid(validationResult.GetWholeMessage());
            }

            var noteResult = await _noteSerializer.GetNote(card);
            if (!noteResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(noteResult.ErrorMessage, noteResult.ErrorType);
            }

            var note = noteResult.Value;
            note.Author = authorResult.Value;

            // The serializer builds a FRESH note, so the stored row's identity
            // has to be carried across by hand. HmmNoteManager.UpdateAsync mints
            // a new Uuid whenever the incoming note has none - which would
            // silently re-identify the card on every save.
            note.Uuid = existingNote.Uuid;
            note.CreateDate = existingNote.CreateDate;
            note.NoteDate = existingNote.NoteDate;
            note.Version = existingNote.Version;
            note.Tags = existingNote.Tags;
            note.Catalog ??= existingNote.Catalog;

            var updatedResult = await _noteManager.UpdateAsync(note, commitChanges);
            if (!updatedResult.Success)
            {
                return ProcessingResult<CheatsheetCard>.Fail(
                    updatedResult.ErrorMessage, updatedResult.ErrorType);
            }

            return await ReadBackAsync(updatedResult.Value, authorResult.Value.Id);
        }

        public async Task<ProcessingResult<Unit>> DeleteAsync(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return ProcessingResult<Unit>.Invalid("Cheatsheet card id cannot be empty");
            }

            var noteResult = await FindNoteForCardAsync(cardId);
            if (!noteResult.Success)
            {
                return ProcessingResult<Unit>.NotFound($"Cannot find cheatsheet card '{cardId}'");
            }

            return await _noteManager.DeleteAsync(noteResult.Value.Id);
        }

        /// <summary>
        /// Re-reads the persisted note so callers always get exactly what was
        /// stored, and stamps the author id the persisted note may not carry.
        /// </summary>
        private async Task<ProcessingResult<CheatsheetCard>> ReadBackAsync(HmmNote note, int authorId)
        {
            var cardResult = await _noteSerializer.GetEntity(note);
            if (cardResult.Success && cardResult.Value != null)
            {
                cardResult.Value.AuthorId = authorId;
            }

            return cardResult;
        }
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetManagerWriteTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 14`.

- [ ] **Step 6: Run the whole module**

Run: `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj`

Expected: PASS — `Passed!  - Failed: 0, Passed: 81`.

- [ ] **Step 7: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add src/Hmm.Cheatsheet src/Hmm.Cheatsheet.Tests
git commit -m "feat(cheatsheet): add cheatsheet manager create/update/delete"
```

---

### Task 9: API DTOs, Newtonsoft row converter, and the mapping profile

The API formatter is Newtonsoft (`Startup.cs` calls `.AddNewtonsoftJson()`), and no camel-case contract resolver is configured, so **API property names are PascalCase** — unlike the note content, which is camelCase. Unknown fields ride `[Newtonsoft.Json.JsonExtensionData]`; an entirely unmodellable row rides a class-level `JsonConverter`.

**Files:**
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheetSource.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheetRow.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheetRowConverter.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheet.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheetForCreate.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheetForUpdate.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/CheatsheetJsonInterop.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Infrastructure/CheatsheetMappingProfile.cs`
- Modify: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Hmm.ServiceApi.csproj`
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.Core.Tests/CheatsheetMappingTests.cs`

**Interfaces:**
- Consumes: `CheatsheetCard`/`CheatsheetRow`/`CheatsheetSource` (Task 1); `Hmm.ServiceApi.DtoEntity.ApiEntity` (abstract, exposes `IEnumerable<Link> Links`); `Hmm.ServiceApi.DtoEntity.Profiles.PageListConverter<TSource, TDest>`.
- Produces: `ApiCheatsheet` (+ `CreateLinks(ResultExecutingContext, LinkGenerator)`), `ApiCheatsheetForCreate`, `ApiCheatsheetForUpdate`, `ApiCheatsheetRow`, `ApiCheatsheetSource`, `CheatsheetJsonInterop.ToJTokens` / `ToJsonElements` / `ToJToken` / `ToJsonElement`, and `CheatsheetMappingProfile`.

- [ ] **Step 1: Reference the module from the API project**

In `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Hmm.ServiceApi.csproj`, add to the `ItemGroup` that already holds `<ProjectReference Include="..\Hmm.Automobile\Hmm.Automobile.csproj" />`:

```xml
    <ProjectReference Include="..\Hmm.Cheatsheet\Hmm.Cheatsheet.csproj" />
```

- [ ] **Step 2: Write the failing test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.Core.Tests/CheatsheetMappingTests.cs`:

```csharp
using AutoMapper;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Hmm.ServiceApi.Core.Tests
{
    public class CheatsheetMappingTests
    {
        private readonly IMapper _mapper;

        public CheatsheetMappingTests()
        {
            var config = new MapperConfiguration(
                cfg => cfg.AddProfile<CheatsheetMappingProfile>(),
                NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
        }

        private static System.Text.Json.JsonElement Raw(string json)
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static CheatsheetCard SampleCard()
        {
            var card = new CheatsheetCard
            {
                NoteId = 42,
                AuthorId = 9,
                Id = "c-1",
                Title = "Passport",
                WalletGroup = "Travel",
                TemplateId = "blank",
                Protected = true,
                Tags = new List<string> { "trip" },
                Rows = new List<CheatsheetRow>
                {
                    new()
                    {
                        Label = "Number",
                        ValueAction = "call",
                        OpenSource = false,
                        Source = new CheatsheetSource
                        {
                            NoteUuid = "u-1",
                            Kind = "field",
                            Locator = "Passport.number"
                        }
                    }
                }
            };

            card.ExtraFields["future"] = Raw("{\"a\":1}");
            card.Rows[0].ExtraFields["icon"] = Raw("\"star\"");
            card.Rows[0].Source.ExtraFields["revision"] = Raw("12");
            card.Rows.Add(new CheatsheetRow { RawJson = Raw("\"corrupt\"") });
            return card;
        }

        [Fact]
        public void Configuration_IsValid()
        {
            var config = new MapperConfiguration(
                cfg => cfg.AddProfile<CheatsheetMappingProfile>(),
                NullLoggerFactory.Instance);

            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void Card_MapsToApiCheatsheet()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(SampleCard());

            Assert.Equal("c-1", api.Id);
            Assert.Equal(1, api.SchemaVersion);
            Assert.Equal("Passport", api.Title);
            Assert.Equal("Travel", api.WalletGroup);
            Assert.Equal("blank", api.TemplateId);
            Assert.True(api.Protected);
            Assert.Equal(new[] { "trip" }, api.Tags);
            Assert.Equal(2, api.Rows.Count);
            Assert.Equal("Number", api.Rows[0].Label);
            Assert.Equal("u-1", api.Rows[0].Source!.NoteUuid);
        }

        [Fact]
        public void Card_CarriesExtrasIntoTheDto()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(SampleCard());

            Assert.Equal("{\"a\":1}", api.ExtraFields["future"].ToString(Formatting.None));
            Assert.Equal("\"star\"", api.Rows[0].ExtraFields["icon"].ToString(Formatting.None));
            Assert.Equal("12", api.Rows[0].Source!.ExtraFields["revision"].ToString(Formatting.None));
            Assert.Equal("\"corrupt\"", api.Rows[1].Raw!.ToString(Formatting.None));
        }

        [Fact]
        public void ApiCheatsheet_MapsBackToCardLosslessly()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(SampleCard());

            var card = _mapper.Map<ApiCheatsheet, CheatsheetCard>(api);

            Assert.Equal("c-1", card.Id);
            Assert.Equal("{\"a\":1}", card.ExtraFields["future"].GetRawText());
            Assert.Equal("\"star\"", card.Rows[0].ExtraFields["icon"].GetRawText());
            Assert.Equal("12", card.Rows[0].Source!.ExtraFields["revision"].GetRawText());
            Assert.True(card.Rows[1].IsUnreadable);
            Assert.Equal("\"corrupt\"", card.Rows[1].RawJson!.Value.GetRawText());
        }

        [Fact]
        public void ForCreate_MapsToCard()
        {
            var forCreate = new ApiCheatsheetForCreate
            {
                Id = "c-9",
                Title = "Alarm",
                WalletGroup = "Home",
                TemplateId = "blank",
                Tags = new List<string> { "security" }
            };
            forCreate.ExtraFields["future"] = JToken.Parse("7");

            var card = _mapper.Map<ApiCheatsheetForCreate, CheatsheetCard>(forCreate);

            Assert.Equal("c-9", card.Id);
            Assert.Equal("Alarm", card.Title);
            Assert.Equal("Home", card.WalletGroup);
            Assert.Equal("7", card.ExtraFields["future"].GetRawText());
        }

        [Fact]
        public void ForUpdate_LeavesIdentityFieldsAlone()
        {
            var existing = SampleCard();
            var forUpdate = new ApiCheatsheetForUpdate
            {
                Title = "Renewed",
                WalletGroup = "Documents",
                TemplateId = "blank",
                Tags = new List<string>()
            };

            _mapper.Map(forUpdate, existing);

            Assert.Equal("Renewed", existing.Title);
            Assert.Equal("Documents", existing.WalletGroup);
            Assert.Equal("c-1", existing.Id);
            Assert.Equal(42, existing.NoteId);
            Assert.Equal(9, existing.AuthorId);
        }

        [Fact]
        public void ApiWireFormat_InlinesExtrasAndEmitsRawRowsVerbatim()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(SampleCard());

            var json = JsonConvert.SerializeObject(api);
            var parsed = JObject.Parse(json);

            Assert.Equal("c-1", parsed.Value<string>("Id"));
            // Card extras are inlined, not nested under an "ExtraFields" object.
            Assert.Null(parsed["ExtraFields"]);
            Assert.Equal("{\"a\":1}", parsed["future"]!.ToString(Formatting.None));

            var rows = (JArray)parsed["Rows"]!;
            Assert.Equal("star", rows[0]["icon"]!.Value<string>());
            Assert.Equal(12, rows[0]["Source"]!["revision"]!.Value<int>());
            // The unmodellable row is the raw token itself, not an object wrapper.
            Assert.Equal(JTokenType.String, rows[1].Type);
            Assert.Equal("corrupt", rows[1].Value<string>());
        }

        [Fact]
        public void ApiWireFormat_RoundTripsThroughNewtonsoft()
        {
            var api = _mapper.Map<CheatsheetCard, ApiCheatsheet>(SampleCard());
            var json = JsonConvert.SerializeObject(api);

            var back = JsonConvert.DeserializeObject<ApiCheatsheet>(json)!;

            Assert.Equal("c-1", back.Id);
            Assert.Equal("{\"a\":1}", back.ExtraFields["future"].ToString(Formatting.None));
            Assert.Equal(2, back.Rows.Count);
            Assert.Equal("star", back.Rows[0].ExtraFields["icon"].Value<string>());
            Assert.Equal(12, back.Rows[0].Source!.ExtraFields["revision"].Value<int>());
            Assert.Equal("corrupt", back.Rows[1].Raw!.Value<string>());
        }
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test src/Hmm.ServiceApi.Core.Tests/Hmm.ServiceApi.Core.Tests.csproj --filter "FullyQualifiedName~CheatsheetMappingTests"`

Expected: FAIL — build errors `CS0246: The type or namespace name 'ApiCheatsheet' could not be found` and `CS0246: ... 'CheatsheetMappingProfile' ...`.

- [ ] **Step 4: Write the source and row DTOs plus the row converter**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheetSource.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// A reference to a piece of a note. Addressed by NoteUuid - the
    /// cross-device-stable identity - never by the local int note id.
    /// </summary>
    public class ApiCheatsheetSource
    {
        public string NoteUuid { get; set; } = string.Empty;

        /// <summary>"field" | "section" | "whole", passed through verbatim.</summary>
        public string Kind { get; set; } = "whole";

        /// <summary>field -&gt; dotted JSON path; section -&gt; heading text; whole -&gt; null.</summary>
        public string Locator { get; set; }

        /// <summary>
        /// Source fields this API version does not model, inlined on the wire so
        /// a client that knows them keeps them across a GET/PUT cycle.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; } = new Dictionary<string, JToken>();
    }
}
```

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheetRow.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// One labelled line of a card. A row may be unbound (Source is null), and
    /// a row this API version cannot model at all is carried whole in
    /// <see cref="Raw"/> - see <see cref="ApiCheatsheetRowConverter"/>.
    /// </summary>
    [JsonConverter(typeof(ApiCheatsheetRowConverter))]
    public class ApiCheatsheetRow
    {
        public string Label { get; set; } = string.Empty;

        /// <summary>"none" | "call" | "map", passed through verbatim.</summary>
        public string ValueAction { get; set; } = "none";

        public bool OpenSource { get; set; } = true;

        /// <summary>Null = unbound.</summary>
        public ApiCheatsheetSource Source { get; set; }

        /// <summary>Row fields this API version does not model.</summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; } = new Dictionary<string, JToken>();

        /// <summary>
        /// The whole row, verbatim, when it is not a JSON object. Serialized in
        /// place of the object, so the wire shape is byte-identical to what was
        /// stored.
        /// </summary>
        [JsonIgnore]
        public JToken Raw { get; set; }
    }
}
```

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheetRowConverter.cs`:

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// Keeps rows the API cannot model on the wire unchanged.
    ///
    /// A POCO cannot represent a row that is a string, a number or null, but
    /// the client stores exactly such rows rather than destroying data it could
    /// not parse. When <see cref="ApiCheatsheetRow.Raw"/> is set this converter
    /// writes that token instead of an object, and on read anything that is not
    /// a JSON object is captured into Raw.
    /// </summary>
    public class ApiCheatsheetRowConverter : JsonConverter<ApiCheatsheetRow>
    {
        private static readonly HashSet<string> KnownKeys = new(StringComparer.Ordinal)
        {
            nameof(ApiCheatsheetRow.Label),
            nameof(ApiCheatsheetRow.ValueAction),
            nameof(ApiCheatsheetRow.OpenSource),
            nameof(ApiCheatsheetRow.Source)
        };

        public override void WriteJson(JsonWriter writer, ApiCheatsheetRow value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value.Raw != null)
            {
                value.Raw.WriteTo(writer);
                return;
            }

            var row = new JObject
            {
                [nameof(ApiCheatsheetRow.Label)] = value.Label ?? string.Empty,
                [nameof(ApiCheatsheetRow.ValueAction)] = value.ValueAction ?? "none",
                [nameof(ApiCheatsheetRow.OpenSource)] = value.OpenSource
            };

            if (value.Source != null)
            {
                row[nameof(ApiCheatsheetRow.Source)] = JObject.FromObject(value.Source, serializer);
            }

            if (value.ExtraFields != null)
            {
                foreach (var extra in value.ExtraFields)
                {
                    // Extras last: a preserved original always beats a default.
                    row[extra.Key] = extra.Value;
                }
            }

            row.WriteTo(writer);
        }

        public override ApiCheatsheetRow ReadJson(
            JsonReader reader,
            Type objectType,
            ApiCheatsheetRow existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var token = JToken.ReadFrom(reader);
            if (token.Type != JTokenType.Object)
            {
                return new ApiCheatsheetRow { Raw = token };
            }

            var source = (JObject)token;
            var row = new ApiCheatsheetRow
            {
                Label = source.Value<string>(nameof(ApiCheatsheetRow.Label)) ?? string.Empty,
                ValueAction = source.Value<string>(nameof(ApiCheatsheetRow.ValueAction)) ?? "none",
                OpenSource = source.Value<bool?>(nameof(ApiCheatsheetRow.OpenSource)) ?? true
            };

            if (source[nameof(ApiCheatsheetRow.Source)] is JObject sourceObject)
            {
                row.Source = sourceObject.ToObject<ApiCheatsheetSource>(serializer);
            }

            foreach (var property in source.Properties())
            {
                if (KnownKeys.Contains(property.Name))
                {
                    continue;
                }

                row.ExtraFields[property.Name] = property.Value;
            }

            return row;
        }
    }
}
```

- [ ] **Step 5: Write the card DTOs**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheet.cs`:

```csharp
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// A cheatsheet card in API responses. Property names are PascalCase - the
    /// API's Newtonsoft formatter is registered without a camel-case contract
    /// resolver, unlike the camelCase note content underneath.
    /// </summary>
    public class ApiCheatsheet : ApiEntity
    {
        /// <summary>Stable card id. Also the route id and the note subject suffix.</summary>
        public string Id { get; set; } = string.Empty;

        public int SchemaVersion { get; set; } = 1;

        public string Title { get; set; } = string.Empty;

        public string WalletGroup { get; set; } = "Ungrouped";

        public IList<string> Tags { get; set; } = new List<string>();

        public string TemplateId { get; set; } = "blank";

        /// <summary>Stored and returned verbatim; the server never acts on it.</summary>
        public bool Protected { get; set; }

        public IList<ApiCheatsheetRow> Rows { get; set; } = new List<ApiCheatsheetRow>();

        /// <summary>Card fields this API version does not model, inlined on the wire.</summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; } = new Dictionary<string, JToken>();

        public void CreateLinks(ResultExecutingContext context, LinkGenerator linkGen)
        {
            var id = Id;
            Links =
            [
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "GetCheatsheetById", new { id }),
                    Rel = "self",
                    Method = "GET"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "UpdateCheatsheet", new { id }),
                    Rel = "update_cheatsheet",
                    Method = "PUT"
                },
                new Link
                {
                    Href = linkGen.GetUriByRouteValues(context.HttpContext, "DeleteCheatsheet", new { id }),
                    Rel = "delete_cheatsheet",
                    Method = "DELETE"
                }
            ];
        }
    }
}
```

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheetForCreate.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// Payload for POST /v1/cheatsheets. <see cref="Id"/> is optional: the
    /// client normally mints the card's v4 UUID, and the server fills one in
    /// when it is absent.
    /// </summary>
    public class ApiCheatsheetForCreate
    {
        public string Id { get; set; }

        public int SchemaVersion { get; set; } = 1;

        public string Title { get; set; } = string.Empty;

        public string WalletGroup { get; set; } = "Ungrouped";

        public IList<string> Tags { get; set; } = new List<string>();

        public string TemplateId { get; set; } = "blank";

        public bool Protected { get; set; }

        public IList<ApiCheatsheetRow> Rows { get; set; } = new List<ApiCheatsheetRow>();

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; } = new Dictionary<string, JToken>();
    }
}
```

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/ApiCheatsheetForUpdate.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// Payload for PUT /v1/cheatsheets/{id}. The card id comes from the route,
    /// never from the body, so it is absent here - a body id could disagree with
    /// the route and silently re-identify the card.
    /// </summary>
    public class ApiCheatsheetForUpdate
    {
        public int SchemaVersion { get; set; } = 1;

        public string Title { get; set; } = string.Empty;

        public string WalletGroup { get; set; } = "Ungrouped";

        public IList<string> Tags { get; set; } = new List<string>();

        public string TemplateId { get; set; } = "blank";

        public bool Protected { get; set; }

        public IList<ApiCheatsheetRow> Rows { get; set; } = new List<ApiCheatsheetRow>();

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraFields { get; set; } = new Dictionary<string, JToken>();
    }
}
```

- [ ] **Step 6: Write the JSON interop helper**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.DtoEntity/Cheatsheets/CheatsheetJsonInterop.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.DtoEntity.Cheatsheets
{
    /// <summary>
    /// Bridges the two JSON stacks this feature straddles: note content is
    /// System.Text.Json (JsonElement), the API wire format is Newtonsoft
    /// (JToken). Conversion goes through raw text, so nothing is interpreted
    /// and nothing is lost.
    /// </summary>
    public static class CheatsheetJsonInterop
    {
        public static JToken ToJToken(JsonElement element) => JToken.Parse(element.GetRawText());

        public static JsonElement ToJsonElement(JToken token)
        {
            using var document = JsonDocument.Parse(token.ToString(Formatting.None));
            // Clone: the document is disposed on the way out of this method.
            return document.RootElement.Clone();
        }

        public static IDictionary<string, JToken> ToJTokens(IDictionary<string, JsonElement> source)
        {
            var result = new Dictionary<string, JToken>(StringComparer.Ordinal);
            if (source == null)
            {
                return result;
            }

            foreach (var pair in source)
            {
                result[pair.Key] = ToJToken(pair.Value);
            }

            return result;
        }

        public static IDictionary<string, JsonElement> ToJsonElements(IDictionary<string, JToken> source)
        {
            var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            if (source == null)
            {
                return result;
            }

            foreach (var pair in source)
            {
                result[pair.Key] = ToJsonElement(pair.Value);
            }

            return result;
        }
    }
}
```

- [ ] **Step 7: Write the mapping profile**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Infrastructure/CheatsheetMappingProfile.cs`:

```csharp
using AutoMapper;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Hmm.ServiceApi.DtoEntity.Profiles;
using Hmm.Utility.Dal.Query;
using Newtonsoft.Json.Linq;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure
{
    /// <summary>
    /// Domain-to-DTO mappings for the cheatsheet area.
    ///
    /// Every JSON-carrying member is mapped with an explicit delegate rather
    /// than by convention: JsonElement is a struct AutoMapper would otherwise
    /// try to map member-by-member, and the two JSON stacks have no common
    /// representation. The delegates route through CheatsheetJsonInterop, which
    /// converts via raw text and therefore cannot lose anything.
    /// </summary>
    public class CheatsheetMappingProfile : Profile
    {
        public CheatsheetMappingProfile()
        {
            CreateMap<CheatsheetSource, ApiCheatsheetSource>()
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJTokens(src.ExtraFields)));

            CreateMap<ApiCheatsheetSource, CheatsheetSource>()
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElements(src.ExtraFields)));

            CreateMap<CheatsheetRow, ApiCheatsheetRow>()
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJTokens(src.ExtraFields)))
                .ForMember(d => d.Raw, opt => opt.MapFrom(
                    (src, dest) => src.RawJson.HasValue
                        ? CheatsheetJsonInterop.ToJToken(src.RawJson.Value)
                        : null));

            CreateMap<ApiCheatsheetRow, CheatsheetRow>()
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElements(src.ExtraFields)))
                .ForMember(d => d.RawJson, opt => opt.MapFrom(
                    (src, dest) => src.Raw == null
                        ? (System.Text.Json.JsonElement?)null
                        : CheatsheetJsonInterop.ToJsonElement(src.Raw)));

            CreateMap<CheatsheetCard, ApiCheatsheet>()
                .ForMember(d => d.Links, opt => opt.Ignore())
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJTokens(src.ExtraFields)));

            CreateMap<ApiCheatsheet, CheatsheetCard>()
                .ForMember(d => d.NoteId, opt => opt.Ignore())
                .ForMember(d => d.AuthorId, opt => opt.Ignore())
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElements(src.ExtraFields)));

            CreateMap<ApiCheatsheetForCreate, CheatsheetCard>()
                .ForMember(d => d.NoteId, opt => opt.Ignore())
                .ForMember(d => d.AuthorId, opt => opt.Ignore())
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElements(src.ExtraFields)));

            // PUT replaces content only. Id, NoteId and AuthorId are identity:
            // they come from the route and the authenticated author, never from
            // a request body that could disagree with them.
            CreateMap<ApiCheatsheetForUpdate, CheatsheetCard>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.NoteId, opt => opt.Ignore())
                .ForMember(d => d.AuthorId, opt => opt.Ignore())
                .ForMember(d => d.ExtraFields, opt => opt.MapFrom(
                    (src, dest) => CheatsheetJsonInterop.ToJsonElements(src.ExtraFields)));

            CreateMap<PageList<CheatsheetCard>, PageList<ApiCheatsheet>>()
                .ConvertUsing(new PageListConverter<CheatsheetCard, ApiCheatsheet>());
        }
    }
}
```

- [ ] **Step 8: Run test to verify it passes**

Run: `dotnet test src/Hmm.ServiceApi.Core.Tests/Hmm.ServiceApi.Core.Tests.csproj --filter "FullyQualifiedName~CheatsheetMappingTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 8`.

- [ ] **Step 9: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add src/Hmm.ServiceApi.DtoEntity src/Hmm.ServiceApi src/Hmm.ServiceApi.Core.Tests
git commit -m "feat(cheatsheet): add API DTOs and lossless domain/DTO mapping"
```

---

### Task 10: `CheatsheetsController` and its result filters

**Files:**
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Filters/CheatsheetResultFilterAttribute.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Filters/CheatsheetsResultFilterAttribute.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Controllers/CheatsheetsController.cs`
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.Core.Tests/CheatsheetsControllerTests.cs`

**Interfaces:**
- Consumes: `ICheatsheetManager` (Tasks 7–8); `CheatsheetMappingProfile` (Task 9); `Hmm.ServiceApi.Filters.ResultFilterBase(IMapper mapper, LinkGenerator linkGenerator)` with `protected abstract Task TransformResultAsync(ResultExecutingContext, ObjectResult, ResultExecutionDelegate)`, `protected IMapper Mapper`, `protected LinkGenerator LinkGenerator`; `Hmm.ServiceApi.Models.ApiBadRequestResponse(string)`.
- Produces: `CheatsheetResultFilter`, `CheatsheetsResultFilter`, and `CheatsheetsController` with routes named `GetCheatsheets`, `GetCheatsheetById`, `AddCheatsheet`, `UpdateCheatsheet`, `DeleteCheatsheet`.

- [ ] **Step 1: Write the failing test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.Core.Tests/CheatsheetsControllerTests.cs`:

```csharp
using AutoMapper;
using Hmm.Cheatsheet;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.Areas.CheatsheetService.Controllers;
using Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Hmm.Utility.Misc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Hmm.ServiceApi.Core.Tests
{
    public class CheatsheetsControllerTests
    {
        private readonly Mock<ICheatsheetManager> _manager = new();
        private readonly CheatsheetsController _controller;

        public CheatsheetsControllerTests()
        {
            var config = new MapperConfiguration(
                cfg => cfg.AddProfile<CheatsheetMappingProfile>(),
                NullLoggerFactory.Instance);

            _controller = new CheatsheetsController(
                _manager.Object,
                config.CreateMapper(),
                new Mock<ILogger<CheatsheetsController>>().Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
        }

        private static CheatsheetCard Card(string id = "c-1", string title = "Passport") => new()
        {
            NoteId = 42,
            AuthorId = 9,
            Id = id,
            Title = title,
            WalletGroup = "Travel",
            TemplateId = "blank"
        };

        private static PageList<CheatsheetCard> Page(params CheatsheetCard[] cards)
            => new(cards, cards.Length, 1, 10);

        [Fact]
        public async Task Get_ReturnsOkWithThePage()
        {
            _manager
                .Setup(m => m.GetCardsAsync(null, null, It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<CheatsheetCard>>.Ok(Page(Card(), Card("c-2", "Alarm"))));

            var result = await _controller.Get(null, null, new ResourceCollectionParameters());

            var ok = Assert.IsType<OkObjectResult>(result);
            var page = Assert.IsType<PageList<CheatsheetCard>>(ok.Value);
            Assert.Equal(2, page.Count);
        }

        [Fact]
        public async Task Get_PassesFiltersThrough()
        {
            _manager
                .Setup(m => m.GetCardsAsync("Home", "trip", It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<CheatsheetCard>>.Ok(Page(Card())));

            await _controller.Get("Home", "trip", new ResourceCollectionParameters());

            _manager.Verify(
                m => m.GetCardsAsync("Home", "trip", It.IsAny<ResourceCollectionParameters>()),
                Times.Once);
        }

        [Fact]
        public async Task Get_ReturnsBadRequest_WhenManagerFails()
        {
            _manager
                .Setup(m => m.GetCardsAsync(null, null, It.IsAny<ResourceCollectionParameters>()))
                .ReturnsAsync(ProcessingResult<PageList<CheatsheetCard>>.Invalid("Database error"));

            var result = await _controller.Get(null, null, new ResourceCollectionParameters());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiBadRequestResponse>(badRequest.Value);
            Assert.Contains("Database error", response.Errors);
        }

        [Fact]
        public async Task GetById_ReturnsOk()
        {
            _manager
                .Setup(m => m.GetCardByIdAsync("c-1"))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.Ok(Card()));

            var result = await _controller.Get("c-1");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("c-1", Assert.IsType<CheatsheetCard>(ok.Value).Id);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound()
        {
            _manager
                .Setup(m => m.GetCardByIdAsync("missing"))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.NotFound());

            var result = await _controller.Get("missing");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Post_ReturnsCreatedAtRoute()
        {
            _manager
                .Setup(m => m.CreateAsync(It.IsAny<CheatsheetCard>(), true))
                .ReturnsAsync((CheatsheetCard card, bool _) => ProcessingResult<CheatsheetCard>.Ok(card));

            var result = await _controller.Post(new ApiCheatsheetForCreate
            {
                Id = "c-1",
                Title = "Passport",
                WalletGroup = "Travel",
                TemplateId = "blank"
            });

            var created = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal("GetCheatsheetById", created.RouteName);
            Assert.Equal("c-1", Assert.IsType<CheatsheetCard>(created.Value).Id);
        }

        [Fact]
        public async Task Post_NullBody_ReturnsBadRequest()
        {
            var result = await _controller.Post(null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Post_Conflict_ReturnsConflict()
        {
            _manager
                .Setup(m => m.CreateAsync(It.IsAny<CheatsheetCard>(), true))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.Conflict("already exists"));

            var result = await _controller.Post(new ApiCheatsheetForCreate { Id = "c-1", Title = "T" });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Put_ReturnsNoContent()
        {
            _manager
                .Setup(m => m.GetCardByIdAsync("c-1"))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.Ok(Card()));
            _manager
                .Setup(m => m.UpdateAsync(It.IsAny<CheatsheetCard>(), true))
                .ReturnsAsync((CheatsheetCard card, bool _) => ProcessingResult<CheatsheetCard>.Ok(card));

            var result = await _controller.Put("c-1", new ApiCheatsheetForUpdate
            {
                Title = "Renewed",
                WalletGroup = "Documents",
                TemplateId = "blank"
            });

            Assert.IsType<NoContentResult>(result);
            _manager.Verify(
                m => m.UpdateAsync(It.Is<CheatsheetCard>(c => c.Id == "c-1" && c.Title == "Renewed"), true),
                Times.Once);
        }

        [Fact]
        public async Task Put_UnknownCard_ReturnsNotFound()
        {
            _manager
                .Setup(m => m.GetCardByIdAsync("missing"))
                .ReturnsAsync(ProcessingResult<CheatsheetCard>.NotFound());

            var result = await _controller.Put("missing", new ApiCheatsheetForUpdate { Title = "T" });

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Put_NullBody_ReturnsBadRequest()
        {
            var result = await _controller.Put("c-1", null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _manager
                .Setup(m => m.DeleteAsync("c-1"))
                .ReturnsAsync(ProcessingResult<Unit>.Ok(Unit.Value));

            var result = await _controller.Delete("c-1");

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_UnknownCard_ReturnsNotFound()
        {
            _manager
                .Setup(m => m.DeleteAsync("missing"))
                .ReturnsAsync(ProcessingResult<Unit>.NotFound());

            var result = await _controller.Delete("missing");

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Hmm.ServiceApi.Core.Tests/Hmm.ServiceApi.Core.Tests.csproj --filter "FullyQualifiedName~CheatsheetsControllerTests"`

Expected: FAIL — build error `CS0246: The type or namespace name 'CheatsheetsController' could not be found`.

- [ ] **Step 3: Write the single-item result filter**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Filters/CheatsheetResultFilterAttribute.cs`:

```csharp
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Hmm.ServiceApi.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Filters;

/// <summary>
/// Transforms a single CheatsheetCard into ApiCheatsheet.
/// Apply using [TypeFilter(typeof(CheatsheetResultFilter))].
/// </summary>
public class CheatsheetResultFilter : ResultFilterBase
{
    public CheatsheetResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is CheatsheetCard card)
        {
            var apiCard = Mapper.Map<CheatsheetCard, ApiCheatsheet>(card);
            apiCard.CreateLinks(context, LinkGenerator);
            resultFromAction.Value = apiCard;
        }

        return next();
    }
}
```

- [ ] **Step 4: Write the collection result filter**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Filters/CheatsheetsResultFilterAttribute.cs`:

```csharp
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Hmm.ServiceApi.Filters;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Filters;

/// <summary>
/// Transforms a PageList of CheatsheetCard into a PageList of ApiCheatsheet and
/// writes the X-Pagination header.
///
/// This deliberately does NOT reuse the shared CollectionResultFilter: that
/// filter runs ShapeData, which reflects every public property into an
/// ExpandoObject. Cheatsheet DTOs carry preserved data through
/// [JsonExtensionData] and a row JsonConverter, both of which reflection
/// flattens - the response would nest extras under "ExtraFields" and lose the
/// verbatim row shape. Keeping the typed objects keeps the wire format honest.
/// </summary>
public class CheatsheetsResultFilter : ResultFilterBase
{
    public CheatsheetsResultFilter(IMapper mapper, LinkGenerator linkGenerator)
        : base(mapper, linkGenerator)
    {
    }

    protected override Task TransformResultAsync(
        ResultExecutingContext context,
        ObjectResult resultFromAction,
        ResultExecutionDelegate next)
    {
        if (resultFromAction.Value is PageList<CheatsheetCard> cards)
        {
            var apiCards = Mapper.Map<PageList<CheatsheetCard>, PageList<ApiCheatsheet>>(cards);
            foreach (var apiCard in apiCards)
            {
                apiCard.CreateLinks(context, LinkGenerator);
            }

            WritePaginationHeader(context, cards);
            resultFromAction.Value = apiCards;
        }

        return next();
    }

    private static void WritePaginationHeader(
        ResultExecutingContext context,
        PageList<CheatsheetCard> cards)
    {
        var metadata = new
        {
            totalCount = cards.TotalCount,
            pageSize = cards.PageSize,
            currentPage = cards.CurrentPage,
            totalPages = cards.TotalPages,
            maxPageSize = ResourceCollectionParameters.MaxPageSize
        };

        context.HttpContext?.Response.Headers.Append(
            "X-Pagination",
            JsonSerializer.Serialize(metadata));
    }
}
```

- [ ] **Step 5: Write the controller**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Controllers/CheatsheetsController.cs`:

```csharp
using System;
using System.Threading.Tasks;
using AutoMapper;
using Hmm.Cheatsheet;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.ServiceApi.Areas.CheatsheetService.Filters;
using Hmm.ServiceApi.DtoEntity;
using Hmm.ServiceApi.DtoEntity.Cheatsheets;
using Hmm.ServiceApi.Models;
using Hmm.Utility.Dal.Query;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Controllers
{
    /// <summary>
    /// Manages cheatsheet wallet cards. The {id} route value is the card's
    /// stable UUID - the same value the note subject "Cheatsheet:{id}" carries -
    /// not the backing note's int id.
    /// </summary>
    [Authorize]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("/v{version:apiVersion}/cheatsheets")]
    [Produces("application/json")]
    public class CheatsheetsController : Controller
    {
        private readonly ICheatsheetManager _cheatsheetManager;
        private readonly IMapper _mapper;
        private readonly ILogger<CheatsheetsController> _logger;

        public CheatsheetsController(
            ICheatsheetManager cheatsheetManager,
            IMapper mapper,
            ILogger<CheatsheetsController> logger)
        {
            ArgumentNullException.ThrowIfNull(cheatsheetManager);
            ArgumentNullException.ThrowIfNull(mapper);
            ArgumentNullException.ThrowIfNull(logger);

            _cheatsheetManager = cheatsheetManager;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a paginated list of cheatsheet cards.
        /// </summary>
        /// <param name="walletGroup">Optional wallet group filter (case-insensitive).</param>
        /// <param name="tag">Optional tag filter (case-insensitive).</param>
        /// <param name="resourceCollectionParameters">Pagination parameters.</param>
        [HttpGet(Name = "GetCheatsheets")]
        [TypeFilter(typeof(CheatsheetsResultFilter))]
        [ProducesResponseType(typeof(PageList<ApiCheatsheet>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> Get(
            [FromQuery] string walletGroup,
            [FromQuery] string tag,
            [FromQuery] ResourceCollectionParameters resourceCollectionParameters)
        {
            var result = await _cheatsheetManager.GetCardsAsync(
                walletGroup, tag, resourceCollectionParameters);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to get cheatsheets: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Retrieves a single cheatsheet card by its card id.
        /// </summary>
        [HttpGet("{id}", Name = "GetCheatsheetById")]
        [TypeFilter(typeof(CheatsheetResultFilter))]
        [ProducesResponseType(typeof(ApiCheatsheet), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(string id)
        {
            var result = await _cheatsheetManager.GetCardByIdAsync(id);
            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }

                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Creates a new cheatsheet card.
        /// </summary>
        [HttpPost(Name = "AddCheatsheet")]
        [TypeFilter(typeof(CheatsheetResultFilter))]
        [ProducesResponseType(typeof(ApiCheatsheet), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult> Post(ApiCheatsheetForCreate apiCard)
        {
            if (apiCard == null)
            {
                return BadRequest(new ApiBadRequestResponse("Cheatsheet data is required"));
            }

            var card = _mapper.Map<ApiCheatsheetForCreate, CheatsheetCard>(apiCard);
            var result = await _cheatsheetManager.CreateAsync(card);

            if (!result.Success)
            {
                if (result.ErrorType == Hmm.Utility.Misc.ErrorCategory.Conflict)
                {
                    return Conflict(new ApiBadRequestResponse(result.ErrorMessage));
                }

                _logger.LogWarning("Failed to create cheatsheet: {Error}", result.ErrorMessage);
                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return CreatedAtRoute("GetCheatsheetById", new { id = result.Value.Id }, result.Value);
        }

        /// <summary>
        /// Replaces a cheatsheet card. The card id comes from the route.
        /// </summary>
        [HttpPut("{id}", Name = "UpdateCheatsheet")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Put(string id, ApiCheatsheetForUpdate apiCard)
        {
            if (apiCard == null)
            {
                return BadRequest(new ApiBadRequestResponse("Cheatsheet data is required"));
            }

            var getResult = await _cheatsheetManager.GetCardByIdAsync(id);
            if (!getResult.Success)
            {
                if (getResult.IsNotFound)
                {
                    return NotFound();
                }

                return BadRequest(new ApiBadRequestResponse(getResult.ErrorMessage));
            }

            // Map onto the stored card so Id / NoteId / AuthorId survive: the
            // mapping profile ignores them precisely so a request body cannot
            // re-identify a card.
            var card = getResult.Value;
            _mapper.Map(apiCard, card);

            var updateResult = await _cheatsheetManager.UpdateAsync(card);
            if (!updateResult.Success)
            {
                if (updateResult.IsNotFound)
                {
                    return NotFound();
                }

                return BadRequest(new ApiBadRequestResponse(updateResult.ErrorMessage));
            }

            return NoContent();
        }

        /// <summary>
        /// Deletes a cheatsheet card (soft-deletes the backing note).
        /// </summary>
        [HttpDelete("{id}", Name = "DeleteCheatsheet")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _cheatsheetManager.DeleteAsync(id);
            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }

                return BadRequest(new ApiBadRequestResponse(result.ErrorMessage));
            }

            return NoContent();
        }
    }
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test src/Hmm.ServiceApi.Core.Tests/Hmm.ServiceApi.Core.Tests.csproj --filter "FullyQualifiedName~CheatsheetsControllerTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 13`.

- [ ] **Step 7: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add src/Hmm.ServiceApi src/Hmm.ServiceApi.Core.Tests
git commit -m "feat(cheatsheet): add /v1/cheatsheets controller and result filters"
```

---

### Task 11: DI wiring, catalog seeding, and full-solution verification

**Files:**
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Infrastructure/CheatsheetServiceStartup.cs`
- Create: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Infrastructure/CheatsheetAppStartupFilter.cs`
- Modify: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Startup.cs` (usings; the `AddAutoMapper` block around line 229; the module registration block around line 280)
- Modify: `/Users/fchy/Projects/Hmm/CLAUDE.md` (endpoint list)
- Test: `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.Core.Tests/CheatsheetServiceStartupTests.cs`

**Interfaces:**
- Consumes: `ICheatsheetCatalogProvider`/`CheatsheetCatalogProvider` (Task 2), `CheatsheetJsonNoteSerialize` (Tasks 3–4), `CheatsheetValidator` (Task 6), `ICheatsheetManager`/`CheatsheetManager` (Tasks 7–8), `CheatsheetMappingProfile` (Task 9); `Hmm.Core.INoteCatalogManager` (`GetEntitiesAsync(Expression<Func<NoteCatalog,bool>>)`, `CreateAsync(NoteCatalog)`).
- Produces: `CheatsheetServiceStartup(IServiceCollection services)` with `void ConfigureServices()`; `CheatsheetAppStartupFilter` implementing `IStartupFilter`.

- [ ] **Step 1: Write the failing test**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi.Core.Tests/CheatsheetServiceStartupTests.cs`:

```csharp
using Hmm.Cheatsheet;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Core;
using Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure;
using Hmm.Utility.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Hmm.ServiceApi.Core.Tests
{
    public class CheatsheetServiceStartupTests
    {
        private static ServiceCollection Configured()
        {
            var services = new ServiceCollection();
            new CheatsheetServiceStartup(services).ConfigureServices();
            return services;
        }

        [Theory]
        [InlineData(typeof(ICheatsheetCatalogProvider), typeof(CheatsheetCatalogProvider))]
        [InlineData(typeof(ICheatsheetManager), typeof(CheatsheetManager))]
        public void ConfigureServices_RegistersCheatsheetServices(Type serviceType, Type implementationType)
        {
            var descriptor = Assert.Single(Configured(), d => d.ServiceType == serviceType);

            Assert.Equal(implementationType, descriptor.ImplementationType);
        }

        [Fact]
        public void ConfigureServices_RegistersValidatorAsTransient()
        {
            var descriptor = Assert.Single(
                Configured(),
                d => d.ServiceType == typeof(IHmmValidator<CheatsheetCard>));

            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        [Fact]
        public void ConfigureServices_RegistersNoteSerializer()
        {
            var descriptor = Assert.Single(
                Configured(),
                d => d.ServiceType == typeof(INoteSerializer<CheatsheetCard>));

            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        }

        [Fact]
        public void ConfigureServices_RegistersTheCatalogStartupFilter()
        {
            Assert.Contains(
                Configured(),
                d => d.ImplementationType == typeof(CheatsheetAppStartupFilter));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Hmm.ServiceApi.Core.Tests/Hmm.ServiceApi.Core.Tests.csproj --filter "FullyQualifiedName~CheatsheetServiceStartupTests"`

Expected: FAIL — build error `CS0246: The type or namespace name 'CheatsheetServiceStartup' could not be found`.

- [ ] **Step 3: Write the module DI registration**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Infrastructure/CheatsheetServiceStartup.cs`:

```csharp
using Hmm.Cheatsheet;
using Hmm.Cheatsheet.DomainEntity;
using Hmm.Cheatsheet.NoteSerialize;
using Hmm.Cheatsheet.Validator;
using Hmm.Core;
using Hmm.Utility.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure
{
    /// <summary>
    /// Registers the cheatsheet module. Author resolution, IHmmNoteManager and
    /// IEntityLookup are already registered by the core Startup and the
    /// automobile module, so nothing here re-registers them.
    /// </summary>
    public class CheatsheetServiceStartup
    {
        private readonly IServiceCollection _services;

        public CheatsheetServiceStartup(IServiceCollection services)
        {
            _services = services;
        }

        public void ConfigureServices()
        {
            _services
                // Catalog lookup (Scoped - depends on IEntityLookup which is Scoped)
                .AddScoped<ICheatsheetCatalogProvider, CheatsheetCatalogProvider>()

                // Validator registered Transient for thread-safety, matching the
                // convention used for every other IHmmValidator in Startup.cs.
                .AddTransient<IHmmValidator<CheatsheetCard>, CheatsheetValidator>()

                // Note serializer
                .AddScoped<INoteSerializer<CheatsheetCard>, CheatsheetJsonNoteSerialize>()

                // Manager
                .AddScoped<ICheatsheetManager, CheatsheetManager>()

                // Ensures the cheatsheet NoteCatalog row exists before first use
                .AddTransient<IStartupFilter, CheatsheetAppStartupFilter>();
        }
    }
}
```

- [ ] **Step 4: Write the catalog-seeding startup filter**

Write `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Areas/CheatsheetService/Infrastructure/CheatsheetAppStartupFilter.cs`:

```csharp
using System;
using System.Linq;
using Hmm.Cheatsheet;
using Hmm.Core;
using Hmm.Core.Map.DomainEntity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure
{
    /// <summary>
    /// Creates the cheatsheet NoteCatalog row if it is missing.
    ///
    /// This is DATA, not schema: cheatsheets ride the existing Notes and
    /// NoteCatalogs tables, so the feature needs no EF migration. Schema
    /// creation itself is still owned by AutomobileAppStartupFilter, which runs
    /// EnsureCreated()/Migrate() per provider.
    ///
    /// The catalog schema is "{}" - an empty JSON schema that validates
    /// everything. A restrictive schema here would reject exactly the
    /// forward-compatible card content the serializer works to preserve.
    /// </summary>
    public class CheatsheetAppStartupFilter : IStartupFilter
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CheatsheetAppStartupFilter> _logger;

        public CheatsheetAppStartupFilter(
            IServiceProvider serviceProvider,
            ILogger<CheatsheetAppStartupFilter> logger = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            using var scope = _serviceProvider.CreateScope();
            EnsureCatalogExists(scope.ServiceProvider);
            return next;
        }

        private void EnsureCatalogExists(IServiceProvider serviceProvider)
        {
            var catalogManager = serviceProvider.GetService<INoteCatalogManager>();
            if (catalogManager == null)
            {
                _logger?.LogWarning("INoteCatalogManager not available, skipping cheatsheet catalog seeding");
                return;
            }

            try
            {
                var existingResult = catalogManager
                    .GetEntitiesAsync(c => c.Name == CheatsheetConstant.CheatsheetCatalogName)
                    .GetAwaiter()
                    .GetResult();

                if (existingResult.Success && existingResult.Value != null && existingResult.Value.Any())
                {
                    return;
                }

                _logger?.LogInformation(
                    "Creating missing NoteCatalog: {CatalogName}",
                    CheatsheetConstant.CheatsheetCatalogName);

                var catalog = new NoteCatalog
                {
                    Name = CheatsheetConstant.CheatsheetCatalogName,
                    Type = NoteContentFormatType.Json,
                    Schema = "{}",
                    IsDefault = false
                };

                var createResult = catalogManager.CreateAsync(catalog).GetAwaiter().GetResult();
                if (!createResult.Success)
                {
                    _logger?.LogWarning(
                        "Failed to create NoteCatalog {CatalogName}: {Error}",
                        CheatsheetConstant.CheatsheetCatalogName,
                        createResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Error ensuring NoteCatalog {CatalogName} exists",
                    CheatsheetConstant.CheatsheetCatalogName);
            }
        }
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test src/Hmm.ServiceApi.Core.Tests/Hmm.ServiceApi.Core.Tests.csproj --filter "FullyQualifiedName~CheatsheetServiceStartupTests"`

Expected: PASS — `Passed!  - Failed: 0, Passed: 5`.

- [ ] **Step 6: Wire the module into `Startup.cs`**

In `/Users/fchy/Projects/Hmm/src/Hmm.ServiceApi/Startup.cs`, add this using immediately after `using Hmm.ServiceApi.Areas.AutomobileInfoService.Infrastructure;` (line 9):

```csharp
using Hmm.ServiceApi.Areas.CheatsheetService.Infrastructure;
```

In the `AddAutoMapper` block (around line 229), change:

```csharp
                .AddAutoMapper(cfg =>
                {
                    cfg.AddProfile<ApiMappingProfile>();
                    cfg.AddProfile<HmmMappingProfile>();
                    cfg.AddProfile<AutomobileMappingProfile>();
                    cfg.AddProfile<UtilityServiceMappingProfile>();
                })
```

to:

```csharp
                .AddAutoMapper(cfg =>
                {
                    cfg.AddProfile<ApiMappingProfile>();
                    cfg.AddProfile<HmmMappingProfile>();
                    cfg.AddProfile<AutomobileMappingProfile>();
                    cfg.AddProfile<CheatsheetMappingProfile>();
                    cfg.AddProfile<UtilityServiceMappingProfile>();
                })
```

In the module registration block (around line 279), change:

```csharp
            // Register Automobile module services (managers, validators, serializers)
            var automobileStartup = new AutomobileInfoServiceStartup(services);
            automobileStartup.ConfigureServices();

            // Register Utility module services (geocoding, etc.)
            var utilityStartup = new UtilityServiceStartup(services, Configuration);
            utilityStartup.ConfigureServices();
```

to:

```csharp
            // Register Automobile module services (managers, validators, serializers)
            var automobileStartup = new AutomobileInfoServiceStartup(services);
            automobileStartup.ConfigureServices();

            // Register Cheatsheet module services (manager, validator, serializer).
            // Must follow the automobile registration: it reuses the author
            // providers and INoteCatalogProvider registered there.
            var cheatsheetStartup = new CheatsheetServiceStartup(services);
            cheatsheetStartup.ConfigureServices();

            // Register Utility module services (geocoding, etc.)
            var utilityStartup = new UtilityServiceStartup(services, Configuration);
            utilityStartup.ConfigureServices();
```

- [ ] **Step 7: Build the whole solution**

Run: `dotnet build Hmm.sln`

Expected: `Build succeeded.` with `0 Error(s)`.

- [ ] **Step 8: Run the whole test suite**

Run: `cd /Users/fchy/Projects/Hmm && dotnet test Hmm.sln`

Expected: every test project reports `Passed!`, `Failed: 0`. In particular `Hmm.Cheatsheet.Tests` reports 81 passed and `Hmm.ServiceApi.Core.Tests` gains 26 new passing tests (8 mapping + 13 controller + 5 startup).

- [ ] **Step 9: Prove no EF migration is needed**

Run:

```bash
cd /Users/fchy/Projects/Hmm
dotnet ef migrations has-pending-model-changes \
  --project src/Hmm.Core.Dal.EF --startup-project src/Hmm.ServiceApi --context HmmDataContext
```

Expected: `No changes have been made to the model since the last migration.`

If this reports pending changes, they are **not** from this feature — nothing in Tasks 1–11 touches `HmmDataContext`, adds a `DbSet`, or adds a DAO entity. Investigate as pre-existing drift (see the *Working with Migrations* section of `CLAUDE.md`); do **not** add a migration as part of this work.

- [ ] **Step 10: Document the endpoints**

In `/Users/fchy/Projects/Hmm/CLAUDE.md`, in the *API Versioning* section, add a new endpoint group immediately after the **ProfileService endpoints** block:

```markdown
**CheatsheetService endpoints:**
- `/v1/cheatsheets` - Cheatsheet wallet cards (GET list with `?walletGroup=&tag=`, POST)
- `/v1/cheatsheets/{id}` - Single card by its stable card UUID (GET/PUT/DELETE)
  - Each card is one `HmmNote` (subject `Cheatsheet:{cardId}`) under the `Hmm.CheatsheetMan.Cheatsheet` catalog, backed by `ICheatsheetManager` in `Hmm.Cheatsheet`.
  - **Round-tripping is lossless by contract**: unknown card/row/source fields, non-object rows, and mistyped known fields are preserved verbatim, matching the Flutter client's `unreadableRows` behaviour. See `docs/superpowers/plans/2026-08-10-backend-cheatsheets-api.md`.
```

- [ ] **Step 11: Commit**

```bash
cd /Users/fchy/Projects/Hmm
git add src/Hmm.ServiceApi src/Hmm.ServiceApi.Core.Tests CLAUDE.md
git commit -m "feat(cheatsheet): wire cheatsheet module into the API and seed its catalog"
```

- [ ] **Step 12: Close out tracker row #33**

Mark tracker row #33 ✅ finished with today's date via the feature-tracker workflow described in `CLAUDE.md`.

---

## Verification checklist

Run after Task 11; all four must hold before the branch is considered done.

- [ ] `dotnet build Hmm.sln` → `0 Error(s)`.
- [ ] `dotnet test Hmm.sln` → `Failed: 0` in every project.
- [ ] `dotnet test src/Hmm.Cheatsheet.Tests/Hmm.Cheatsheet.Tests.csproj --filter "FullyQualifiedName~CheatsheetRoundTripTests"` → 9 passed. This is the interop contract; a regression here means the API can delete client data.
- [ ] `dotnet ef migrations has-pending-model-changes --project src/Hmm.Core.Dal.EF --startup-project src/Hmm.ServiceApi --context HmmDataContext` → `No changes have been made to the model since the last migration.`
