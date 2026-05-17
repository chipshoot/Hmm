# Task Plan

## Objective
Design and implement an Obsidian-style file-vault for Hmm and a
**note-level attachments facility** on top of it — then use it to
attach a car photo (primary + optional gallery) to a vehicle's note.

Attachments belong to the `HmmNote` (a new nullable `attachments`
JSON column on the `Notes` table), so the same mechanism covers
every note type. The vault must work in **all three data modes**
(`local`, `cloudStorage`, `cloudApi`) and align with the migration
model in `docs/multi-device-cloud-sync.md`.

## Why
- Flutter's existing note-keyed attachment plumbing
  (`local_attachment_repository.dart` + Drift `Attachments` table)
  is half-built, doesn't survive cross-device sync, and uses an
  asset-catalog shape we're not keeping; the .NET API has nothing.
- Prior-art note apps (Obsidian, Apple Notes, OneNote, Notion)
  uniformly store bytes somewhere addressable by every viewing
  device — none rely on URI-only references to the user's photo
  library.
- Of those, **Obsidian's file-vault model** maps cleanly onto our
  three tiers: free `cloudStorage` mode gets multi-device sync
  "for free" via OneDrive's file-level sync; the paid tier needs a
  small file-server API but reuses the same path/reference shape.
- Storing the references on the `HmmNote` itself (not inside a
  domain payload) means plain-text/HTML notes can carry attachments
  too, and no domain serializer changes.

## Phases

### Phase 1: Research & Discovery — DONE
- [x] Confirm Flutter has half-built, note-keyed attachment plumbing
      (Drift `Attachments` table + `IAttachmentRepository`)
- [x] Confirm .NET side has zero attachment infrastructure
- [x] Survey Obsidian / OneNote / Apple Notes / Notion / Bear /
      Logseq prior art
- [x] Conclude: file-vault model fits our three tiers better than
      asset-catalog

### Phase 2: Design Doc — DONE
- [x] Pick storage model: file vault, identical layout per tier,
      transport varies (local FS / OneDrive / API)
- [x] Decide reference shape: tagged union (`vault`/`phasset`/
      `cloudFile`); `primaryImage` + `images[]`; **stored on the
      note in a new `attachments` JSON column**, not in the domain
      payload (revised 2026-05-11 — was "siblings in note content")
- [x] Decide MIME / size limits + server-side downsize
- [x] Decide EXIF / virus scan / thumbnail policy (defer)
- [x] Define API surface (`/v1/vault/{path}`) with subscription
      gating
- [x] Document iOS file-visibility flags + backup integration
- [x] Write `docs/attachments-design.md`
- [x] **GATE**: reviewed with user 2026-05-11 — 7 doc follow-ups
      raised (below), 4 code-shaping edits applied to the doc, the
      attachments-on-`HmmNote` pivot + sidecar-JSON-column decision
      taken; user chose the **Flutter local-mode vertical slice** as
      the first work.

## Design-doc follow-ups (from 2026-05-11 review)

Code-shaping decisions — all written into `docs/attachments-design.md`
(2026-05-11); implement in the phase noted:
- [x] `Hmm.Core.Vault` as its own project from day one → Phase 4
- [x] `primaryImage` / `images` are **disjoint** slots → enforce in
      the Phase 9 codec & Phase 12 projection
- [x] `byteSize` is **nullable** (only `vault` refs guarantee it) →
      `NoteAttachments.schema.json` in Phase 6 + the Phase 9 codec
- [x] The `attachments` column + `ApiNote*` DTOs are typed `VaultRef`,
      not polymorphic `AttachmentRef` → Phase 6
- [x] Attachments live on the `HmmNote`, in a sidecar `attachments`
      JSON column on `Notes` → Phases 6 (.NET) + 11 (Flutter)

Later-phase (Phase 16+ concerns, parked):
- [ ] HEIC on Android `cloudApi` viewers — document "view natively"
      vs. server-side transcode to JPEG
- [ ] `image_picker` vs `photo_manager` — verify which package
      actually yields a `PHAsset.localIdentifier` before Phase 16
- [ ] Spell out the migration-gate — feature-flag the picker by tier
      so non-vault refs only get created in `local`/`cloudStorage`
      modes (Phase 18 must land before Phases 16–17 reach paid users)

### Phase 3: Shared specs — DONE 2026-05-13
- [x] `docs/attachments-path-spec.md` — POSIX-only, no `..`, ASCII-
      only allowed-char set, Windows-reserved names rejected,
      verbatim test-vector tables; both sides implement to this.
- [x] `src/Hmm.Core/Schemas/NoteAttachments.schema.json` — embedded
      resource on `Hmm.Core`; covers the `{primaryImage, images}`
      wrapper + the vault/phasset/cloudFile tagged-union refs;
      `byteSize` nullable for non-vault kinds.
- [x] Dart `vaultRelativePathJoin` / `vaultRelativePathValidate` in
      `lib/core/data/vault/vault_path.dart`; 36 tests pass.

### Phase 4: .NET vault store (`Hmm.Core.Vault` — new project)
- [ ] New `Hmm.Core.Vault` project; `IVaultBlobStore` interface
- [ ] `FilesystemVaultBlobStore`, root from `AttachmentSettings`
- [ ] `VaultRef` value object (`{Path, OriginalName, ContentType,
      ByteSize}`)
- [ ] xUnit tests covering put / get / delete / list / sanitisation
- [ ] DI registration in `Hmm.ServiceApi`; `AttachmentSettings`
      bound from `appsettings.json`

### Phase 5: .NET vault HTTP surface
- [ ] `VaultController` with the five endpoints
      (`POST/GET/HEAD/DELETE` per-file + list)
- [ ] `RequireActiveSubscriptionAttribute` (from sync doc) on writes
- [ ] Validation: MIME allow-list, max bytes, max long-edge pixels
- [ ] Server-side image downsize via `SkiaSharp`
- [ ] xUnit tests for controller + integration tests for upload
      round-trip

### Phase 6: .NET `Notes.attachments` column wiring
- [ ] Add nullable `string? Attachments` column to `HmmNoteDao` →
      EF migration (`Attachments NVARCHAR(MAX) NULL` + SQLite /
      PostgreSQL equivalents)
- [ ] Add `VaultRef? PrimaryImage` + `IList<VaultRef> Images` to the
      `HmmNote` domain entity
- [ ] AutoMapper value converter: JSON column ↔ the two domain props
- [ ] `NoteAttachments.schema.json` validation on write →
      `ProcessingResult` failure on invalid
- [ ] `ApiNote`, `ApiNoteForCreate`, `ApiNoteForUpdate` surface
      `primaryImage` + `images`; `ApiMappingProfile` + note result
      filters pass them through
- [ ] Tests: column round-trip, schema rejection, DTO mapping

### Phase 7: .NET migration endpoints integration
- [ ] Extend `POST /v1/migration/upload` to accept vault bytes
- [ ] Extend `GET /v1/migration/export` to stream the vault as a
      zip alongside the record JSON (incl. the `attachments` column)
- [ ] Extend `POST /v1/migration/replace` to wipe the vault on the
      server before re-upload
- [ ] Update `MigrationLog.RecordCounts` → `vaultFiles`,
      `vaultBytes`, `resolvedPhAssets`, `resolvedCloudFiles`,
      `unresolvedRefs`

### Phase 8: .NET deploy + backup integration
- [ ] Add `/var/lib/hmm-vault` Docker volume to `compose.api.yml`
- [ ] Extend `docker/hmm-deploy.sh --backup` to tar the vault
- [ ] Document restore order (pg first, then vault) in the script's
      help

### Phase 9: Flutter — `AttachmentRef` + codec — DONE 2026-05-13
- [x] `AttachmentRef` sealed class in `lib/core/data/attachments/`
      (`VaultRef` / `PhAssetRef` / `CloudFileRef`)
- [x] `AttachmentRefCodec` + `NoteAttachmentsCodec` (the
      `{primaryImage, images}` wrapper, disjoint slots)
- [x] 28 codec tests pass

### Phase 10: Flutter — vault store — DONE 2026-05-13
- [x] `IVaultStore` interface in `lib/core/data/vault/`
- [x] `LocalVaultStore` (path_provider + dart:io); atomic
      put-then-rename writes
- [ ] In-memory cache wrapper for repeat reads — deferred to a
      perf-needs-it phase
- [x] 17 tests against a tmp-dir pass

### Phase 11: Flutter — `Notes.attachments` column + `HmmNote` model — DONE 2026-05-13
- [x] New nullable `attachments` text column on the Drift `Notes`
      table + Drift migration (v3 → v4)
- [x] `HmmNote` model gains nullable `NoteAttachments? attachments`
      + `effectiveAttachments` getter; `LocalHmmNoteRepository`
      round-trips the column via the codec
- [x] Remove the truly-unused `IAttachmentRepository`,
      `LocalAttachmentRepository`, `attachmentRepositoryProvider`
- [ ] **Phase 11.5 (deferred)**: Drift `Attachments` child table
      stays in place — `SyncOrchestrator` still uses it; once the
      sync layer is rewired to the vault + the new `attachments`
      column, drop the table.
- [x] 7 repository round-trip tests pass; full suite (379) clean

### Phase 12: Flutter — `Automobile` read-through projection — DONE 2026-05-13
- [x] `Automobile` gains `AttachmentRef? primaryImage` +
      `List<AttachmentRef> images` (default null / `[]`)
- [x] `LocalAutomobileRepository._deserialize` reads them from the
      owning note's `attachments` column; `_serialize` does NOT
      include them in the JSON content payload.
- [x] `createAutomobile` / `updateAutomobile` pass
      `_attachmentsFor(auto)` through `HmmNoteCreate/Update`; a car
      with no photos clears the column.
- [x] 6 tests pass.

### Phase 13: Flutter — picker plumbing (vault-only v1) — DONE 2026-05-13
- [x] `image_picker ^1.1.2` added.
- [x] `VaultImageAttachmentPicker` — `pickForNote(noteId, source)`
      returns a `VaultRef` after copying bytes into the vault; pure
      `persistToVault(...)` helper for headless callers/tests.
- [x] Validates MIME against allow-list (jpeg/png/heic/webp);
      rejects empty / oversized files; resolves content-type from
      hint or extension (hint loses for vague values like
      `image/*`).
- [x] 8 tests pass.

### Phase 14: Flutter — viewer + vehicle screen UI — DONE 2026-05-16
- [x] **Data layer (2026-05-13)**: `VaultResolver` +
      `CompositeAttachmentResolver` + `AttachmentImage` widget +
      `attachment_providers.dart` (mode-aware vault root, vault
      store, resolver, picker). 9 tests pass.
- [x] **Screen integration (2026-05-16)**: Photo `EditableInfoCard`
      added above the identity card on `AutomobileEditScreen`.
      Display mode = 96×96 thumbnail (tap → fullscreen
      `InteractiveViewer` dialog) or "No photo" italics. Editor mode
      = 120×120 pending preview + "Choose photo / Replace / Remove"
      buttons + busy spinner during picker async. Pending state on
      the screen; `_cloneWith` gained a sentinel-guarded
      `primaryImage` so a real null (Remove) round-trips. Save uses
      the existing `_persist` path.
- [x] `ios/Runner/Info.plist`:
      `NSPhotoLibraryUsageDescription` added so `image_picker`
      doesn't hard-abort the first time it's invoked on iOS.
- [ ] **Known limitation (deferred)**: picker writes bytes to the
      vault on every successful pick, so cancel-after-pick or
      replace-after-pick leaves orphaned vault files. A GC sweep
      (compare every `Notes.attachments` ref against the on-disk
      vault listing, delete stragglers) will reclaim them in a
      future phase.
- [x] flutter analyze clean; full suite 402 tests pass.

### Phase 15: Flutter — API vault store + mode-aware provider
- [ ] `ApiVaultStore` (Dio-backed `/v1/vault/{path}`)
- [ ] Mode-aware `vaultStoreProvider` in `repository_providers.dart`
      keyed off `dataModeProvider`

### Phase 16: Flutter — `PhAssetResolver` (iOS)
- [ ] `PhAssetResolver` via `photo_manager`; iOS-only, null
      elsewhere
- [ ] Picker emits `PhAssetRef` for iOS Photos picks instead of
      copying

### Phase 17: Flutter — `CloudFileResolver` (macOS / Windows)
- [ ] Detect OneDrive / iCloud Drive roots by path prefix
- [ ] `CloudFileResolver` (OS-level file read)
- [ ] Picker emits `CloudFileRef` for picks under a detected root

### Phase 18: Flutter — Free → Paid migration extension
- [ ] Resolve every non-vault ref to a `vault` ref before upload;
      rewrite the note's `attachments` column
- [ ] Consent dialog surfaces counts + unresolvable refs
- [ ] Gate Phases 16–17 picker output by tier so this is always
      possible

### Phase 19: iOS visibility
- [ ] Set `UIFileSharingEnabled = YES` +
      `LSSupportsOpeningDocumentsInPlace = YES` in
      `ios/Runner/Info.plist`
- [ ] Verify the vault appears in iOS Files app on a device

### Phase 20: Verification
- [ ] `flutter analyze` + `flutter test` clean
- [ ] `dotnet build Hmm.sln && dotnet test Hmm.sln` clean
- [ ] Smoke: pick photo on iOS sim → save → reopen → still there
      (local mode)
- [ ] Smoke: switch to API mode → upload existing local photo →
      open from second device → renders
- [ ] Smoke: trigger a backup, untar the vault, verify files

## Decisions Log
| Decision | Rationale | Date |
|----------|-----------|------|
| Obsidian-style file vault, not asset catalog | Reuses OneDrive sync for `cloudStorage`; simpler code; matches mental model of "your files in your folder" | 2026-05-09 |
| Filesystem-backed `IVaultBlobStore` v1, abstraction in place for S3 later | Personal-use VPS today; keep optionality cheap | 2026-05-09 |
| Implement `primaryImage` + `images[]` data shape, wire only `primaryImage` UI in v1 | Cheap to land; future gallery doesn't need a schema bump | 2026-05-09 |
| Defer EXIF strip, virus scan, server-side thumbnails | Personal-use scale; revisit when the cap stings | 2026-05-09 |
| Tagged-union refs (`vault`/`phasset`/`cloudFile`); paid tier vault-only; non-vault refs resolved at the Free→Paid boundary | Don't duplicate bytes Apple/Microsoft already sync; server can't reach a remote device's Photos/cloud folder | 2026-05-09 |
| Design-doc gate cleared; first work = Flutter local-mode vertical slice (Phases 3 → 9 → 10 → 11 → 12 → 13 → 14, `vault` kind only) | Shortest path to a visible feature (photo on a car in local mode); de-risks the data shape; no .NET dependency | 2026-05-11 |
| `primaryImage` / `images` are disjoint slots | Cheaper to reason about; avoids "is the primary also in the gallery?" ambiguity in the codec | 2026-05-11 |
| **Attachments are a `HmmNote`-level facility, stored in a new sidecar `attachments` JSON column on `Notes`** (replaces the earlier "siblings in note content" decision) | `Content` is a raw string for plain-text/HTML notes, so it can't carry structured siblings; a column makes attachments note-universal, leaves every domain serializer untouched, costs one EF migration + one Drift migration, and adds no new repo/manager/controller. A relational `NoteAttachment` child table was the alternative — rejected because we don't need attachments SQL-queryable yet (revisit for orphan-GC/dedup later). | 2026-05-11 |
| `Hmm.Core.Vault` as its own project from day one | Interface + filesystem impl + `VaultRef` + tests already justify it; splitting later relocates public types across packages | 2026-05-11 |
| The `attachments` column + `ApiNote*` DTOs carry `VaultRef` directly, no `kind` discriminator on the wire to .NET | A `phasset`/`cloudFile` payload deserializes to a `VaultRef` with null `Path` → dies in schema validation; can never become a valid server-side object | 2026-05-11 |

## Active work: Flutter local-mode vertical slice

Phases 3 → 9 → 10 → 11 → 12 → 13 → 14 (design-doc steps
1 → 2 → 9 → 10 → 11 → 12 → 13 → 14). No .NET work required.

Sequence: shared specs (path util + JSON schemas) → `AttachmentRef`
sealed class + `NoteAttachments` codec (`vault` kind only) →
`IVaultStore` + `LocalVaultStore` → `attachments` column on the
Drift `Notes` table + `HmmNote` model round-trip + remove the old
`Attachments` table → `Automobile` read-through projection +
`LocalAutomobileRepository` writes to the owning note's column →
`image_picker` (vault-only) → `VaultResolver` + viewer widget on the
vehicle screen.

Done when: pick a photo on the iOS sim → save the car → reopen →
the photo's still there.

**Blocker**: `~/Projects/hmm_console` must be added as a working
directory before any Dart edits.

## Backlog (deferred feature work)

These are user-requested enhancements outside the current
attachments scope. Pick up after the attachments feature is
finished on both tiers (local/cloudStorage AND cloudApi).

### Registration card expansion (logged 2026-05-17)
The `AutomobileEditScreen` Registration card currently captures
only `registrationExpiryDate` (one field for renewal reminders).
Insurance has provider + policy number + expiry; Registration is
asymmetrically thin. Expand to capture a real vehicle registration
document.

Field menu (MVP = first two):
- `registrationNumber` — document/permit ID (proof of ownership).
- `registrationJurisdiction` — state / province / country.
- `registrationIssuedDate` — pair with expiry to compute renewal
  cycle.
- `registrationLastRenewalFee` — Money type; budget tracking.
- (later) `registrationOwnerName`, `registrationClass`.

Implementation also requires .NET-side changes
(`AutomobileInfo.schema.json` + DTOs + AutoMapper) when that scope
re-opens. While we're there, consider expanding Insurance similarly
(coverage limits, deductible, insurance-card photo via the
attachments facility) so the two cards stay symmetric.

Details in project memory:
`~/.claude/projects/.../memory/registration-card-expansion.md`.

## Active scope (user, 2026-05-15)

**Current phase exercises the local + cloudStorage tiers only. Do
NOT touch `Hmm.ServiceApi` (or any .NET-side attachment plumbing)
until the local/cloudStorage test cycle is complete and the user
explicitly switches to the API tier.**

Out of scope for current turns (revisit when user signals):
- Phase 4 (.NET `Hmm.Core.Vault`)
- Phase 5 (.NET `VaultController` + downsize)
- Phase 6 (.NET `Notes.attachments` column wiring + `ApiNote*` DTOs)
- Phase 7 (.NET migration endpoints integration)
- Phase 8 (.NET deploy + backup)
- Phase 11.5 (rewire `SyncOrchestrator` + drop old Drift
  `Attachments` table — same API-side boundary)
- Phase 15 (Flutter `ApiVaultStore` + cloudApi mode-aware provider)

In scope: Phase 14 screen integration on `AutomobileEditScreen`,
Phases 16–17 / 19 if the smart-reference or iOS-visibility paths
come up, plus any local/cloudStorage testing or fixes.

The Docker stack (`hmm-deploy.sh --start --rebuild` ran 2026-05-15)
is the existing baseline — no redeploy needed for attachment work
during this phase.

## Status: Phases 3, 9–14 complete (2026-05-16). Local-mode vertical slice is feature-complete: pick a photo on the vehicle screen → save → reopen → photo persists. Tests: 402 pass; analyze clean; iOS Info.plist permission added. Vault-orphan GC deferred. Phase 11.5 + .NET-side phases (4–8) still deferred per the active scope note above; revisit when user signals the local/cloudStorage test cycle is complete.
