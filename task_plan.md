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

### Phase 7: .NET migration endpoints integration — DONE 2026-05-18
- [x] `POST /v1/migration/upload` (built greenfield — accepts vault
      bytes via multipart/form-data: one `manifest` JSON field +
      one `IFormFile` per blob; form-field name = vault relative
      path)
- [x] `GET /v1/migration/export` streams a zip — `records.json` at
      the root + every vault file at its full
      `attachments/note-N/...` path
- [x] `POST /v1/migration/replace` wipes vault + hard-deletes the
      author's notes (FK cascade clears NoteTagRefs), then runs
      the upload flow with `Kind=CloudReplaced`
- [x] `MigrationLog.RecordCounts` carries `{ notes, notesFailed,
      vaultFiles, vaultBytes, ...clientCounts }`. Client-supplied
      counters (`resolvedPhAssets`, `resolvedCloudFiles`,
      `unresolvedRefs`) are preserved verbatim; server-computed
      keys win on collision.
- [x] `GET /v1/migration/log?take=N` for audit replay
- [x] Subscription gating deferred (same place as
      `NoteVaultController` — picks up the future
      `RequireActiveSubscriptionAttribute`)
- [x] Devices entity not yet built; `MigrationLog.DeviceIdentifier`
      is a string column (max 80 chars). When `Devices` lands as
      part of cloud-sync work, widen into a FK + backfill.
- [x] 18 new tests: 9 manager (upload happy + 4 per-record/blob
      error paths + log row + Replace wipe + Export zip + GetLog),
      9 controller (auth, manifest parsing, dispatch, zip return,
      DTO mapping)

### Phase 8: .NET deploy + backup integration — DONE 2026-05-18
- [x] `api-vault-data` named volume in `compose.api.yml`, mounted
      at `/var/lib/hmm-vault` on `hmm-api`. Matches
      `AttachmentSettings.RootDir` in `appsettings.Docker.json`.
- [x] `hmm-deploy.sh --backup` adds a `hmm-vault-<ts>.tar.gz` next
      to the two pg dumps. The tar runs inside the container
      (`docker exec hmm-api tar -C /var/lib/hmm-vault -czf -`) so
      paths inside the archive are relative + the host platform
      doesn't matter.
- [x] `--help` text spells out the restore order — Postgres dumps
      first, then the vault tarball. DB rows in `Notes.attachments`
      reference vault paths, so byte-recovery without DB rows is
      orphan bytes; DB without bytes is placeholder UI.

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

### Phase 15: Flutter — API vault store + mode-aware provider — DONE 2026-05-18
- [x] **15a — vault**: `ApiVaultStore` (Dio-backed against the
      nested `/v1/notes/{noteId}/vault/{filename}` route the .NET
      side actually ships); `vaultStoreProvider` branches on
      `dataModeProvider` so cloudApi swaps the on-disk root for
      direct HTTP. Single-file `list(prefix)` falls back to HEAD;
      empty-prefix throws UnimplementedError until the cross-note
      `/v1/migration/manifest` endpoint lands.
- [x] **15b — sync provider**: real `ApiSyncProvider` replacing
      the Phase 11.5 stub.
  - Server-side prerequisites: `HmmNoteDao.Uuid` (string?, unique
    index, EF migration); auto-assigned by the manager on
    Create/Update; new `GET /v1/notes/by-uuid/{uuid}`; `?includeDeleted`
    on the collection endpoint so tombstones propagate;
    `ApiNote.CatalogName` exposed for catalog matching by name.
  - Flutter provider: paginated `pullManifest` (reads
    `X-Pagination`); `pullNoteBody` via by-uuid translates ApiNote
    → orchestrator body; `pushNoteBody` does an existence probe
    then POST / PUT / DELETE (deletedAt → DELETE; tombstone for a
    server-side-missing note is a no-op); `pushManifest` is an
    intentional no-op (server-side rows ARE the manifest);
    `signIn`/`signOut` defer to the app's IdP login flow.
- [x] Tests: 18 ApiSyncProvider cases (auth, paginated pull,
      pull body translation, push CRUD branches, catalog-name
      lookup error). Server side: +11 (Uuid manager + by-uuid
      controller + includeDeleted).

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

## Active scope (user, 2026-05-18 — superseded the 2026-05-15 boundary)

**Local + cloudStorage tier work is complete; user signed off
2026-05-18. The `Hmm.ServiceApi`-side work is now the active
scope.** Phases 4 → 5 → 6 → 7 → 8 → 15 in order. Phase 11.5
(SyncOrchestrator rewire + dropped old `Attachments` Drift table)
already landed 2026-05-17.

In scope:
- Phase 4 (.NET `Hmm.Core.Vault`) — **starting now**
- Phase 5 (.NET `VaultController` + image downsize)
- Phase 6 (.NET `Notes.attachments` column + DTOs)
- Phase 7 (.NET migration endpoints)
- Phase 8 (.NET deploy + backup)
- Phase 15 (Flutter `ApiVaultStore` + real `ApiSyncProvider`)

Loose ends on the local/cloudStorage tier — not blocking, can
land alongside or after:
- Vault orphan GC (Replace / Remove leave bytes on disk)
- Phase 19 — iOS `UIFileSharingEnabled` plist flags
- Real-world cloudStorage multi-device validation by the user
  (two macOS machines + shared OneDrive folder)

Still parked (not part of cloudApi tier):
- Phase 16 — `PhAssetResolver` (iOS smart refs)
- Phase 17 — `CloudFileResolver` (desktop smart refs)
- Phase 18 — Free → Paid migration extension

## Status: local + cloudStorage tiers complete (2026-05-18, user signed off). Tests: 431 pass on Flutter 3.41.9. **Now starting the API-side work — Phase 4 (`Hmm.Core.Vault` project).**

### Phase 11.5 — DONE 2026-05-17
- [x] SyncOrchestrator stops touching attachment bytes; pull/push is
      notes-only. Attachment refs ride inside `note.content` /
      `note.attachments` and travel with the note body. Old
      attachment manifest entries are tolerated on read (parsed but
      ignored); new manifests always write `attachments: []`.
- [x] Schema v4 → v5 migration drops the legacy `Attachments` Drift
      table. Older migrations rewritten to raw SQL so the migrator
      no longer needs the typed `Attachments` reference; the class
      itself is gone. `database.g.dart` regenerated.
- [x] `CloudSyncProvider.pullAttachmentBytes` /
      `pushAttachmentBytes` removed from the interface + both impls
      (ApiSyncProvider stub, OneDriveSyncProvider). `OneDriveGraphClient`
      attachment helpers (getAttachment/putAttachment/deleteAttachment)
      removed. `AttachmentBlob` removed from `sync_models.dart`.
- [x] **Cloud-root detection** (in scope with 11.5): user-configurable
      vault folder via Settings (FilePicker → SharedPreferences key
      `cloud_storage_vault_path`); `vaultRootDirectoryProvider`
      honors the configured path in `cloudStorage` mode on non-iOS;
      iOS falls back to `<app docs>/vault/` per the user decision.
      Settings UI shows the current path + Choose Folder / Reset
      buttons under cloudStorage mode (hidden on iOS).
- [x] 5 new unit tests for the persistence helpers; 422→427 in the
      full suite.
