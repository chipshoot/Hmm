# Task Plan

## Objective
Design and implement an Obsidian-style file-vault for Hmm — and use
it to attach a car photo (primary + optional gallery) to
`AutomobileInfo`.

The vault must work in **all three data modes** (`local`,
`cloudStorage`, `cloudApi`) and align with the migration model in
`docs/multi-device-cloud-sync.md`.

## Why
- Flutter's existing asset-catalog plumbing
  (`local_attachment_repository.dart` + Drift `Attachments` table)
  doesn't survive cross-device sync, and the .NET API has nothing.
- Prior-art note apps (Obsidian, Apple Notes, OneNote, Notion)
  uniformly store bytes somewhere addressable by every viewing
  device — none rely on URI-only references to the user's photo
  library.
- Of those, **Obsidian's file-vault model** maps cleanly onto our
  three tiers: free `cloudStorage` mode gets multi-device sync
  "for free" via OneDrive's file-level sync; the paid tier needs a
  small file-server API but reuses the same path/reference
  shape.

## Phases

### Phase 1: Research & Discovery — DONE
- [x] Confirm Flutter has half-built attachment plumbing (Drift
      `Attachments` table + `IAttachmentRepository`)
- [x] Confirm .NET side has zero attachment infrastructure
- [x] Survey Obsidian / OneNote / Apple Notes / Notion / Bear /
      Logseq prior art
- [x] Conclude: file-vault model fits our three tiers better than
      asset-catalog

### Phase 2: Design Doc — DONE
- [x] Decide reference shape: `primaryImage` + `images[]` siblings
      inside the existing note JSON content (no per-entity FK)
- [x] Pick storage model: file vault, identical layout per tier,
      transport varies (local FS / OneDrive / API)
- [x] Decide MIME / size limits + server-side downsize
- [x] Decide EXIF / virus scan / thumbnail policy (defer)
- [x] Define API surface (`/v1/vault/{path}`) with subscription
      gating
- [x] Document iOS file-visibility flags
- [x] Document backup integration
- [x] Write `docs/attachments-design.md`
- [ ] **GATE**: review with user before any code

### Phase 3: Path utility (shared spec)
- [ ] Spec the relative-path rules (POSIX separators, no `..`, no
      leading `/`, allowed chars)
- [ ] Implement once on each side as a pure function with unit tests

### Phase 4: .NET vault store
- [ ] `IVaultBlobStore` interface in `Hmm.Core` (or new
      `Hmm.Core.Vault` project)
- [ ] `FilesystemVaultBlobStore` implementation, root from
      `AttachmentSettings`
- [ ] xUnit tests covering put / get / delete / list / sanitisation
- [ ] DI registration in `Hmm.ServiceApi`
- [ ] `AttachmentSettings` bound from `appsettings.json`

### Phase 5: .NET vault HTTP surface
- [ ] `VaultController` with the five endpoints
      (`POST/GET/HEAD/DELETE` per-file + list)
- [ ] `RequireActiveSubscriptionAttribute` (from sync doc) on writes
- [ ] Validation: MIME allow-list, max bytes, max long-edge pixels
- [ ] Server-side image downsize via `SkiaSharp`
- [ ] xUnit tests for controller + integration tests for upload
      round-trip

### Phase 6: .NET migration endpoints integration
- [ ] Extend `POST /v1/migration/upload` to accept vault bytes
- [ ] Extend `GET /v1/migration/export` to stream the vault as a
      zip alongside the record JSON
- [ ] Extend `POST /v1/migration/replace` to wipe the vault on the
      server before re-upload
- [ ] Update `MigrationLog.RecordCounts` to include `vaultFiles` +
      `vaultBytes`

### Phase 7: .NET deploy + backup integration
- [ ] Add `/var/lib/hmm-vault` Docker volume to `compose.api.yml`
- [ ] Extend `docker/hmm-deploy.sh --backup` to tar the vault
- [ ] Document restore order in the deploy script's help

### Phase 8: AutomobileInfo model wiring (.NET)
- [ ] Add `VaultRef PrimaryImage` (nullable) +
      `List<VaultRef> Images` to the `AutomobileInfo` domain entity
- [ ] Define `VaultRef` value object (`{Path, OriginalName,
      ContentType, ByteSize}`) in `Hmm.Core` (shared)
- [ ] Update `AutomobileJsonNoteSerialize` to round-trip the new
      fields
- [ ] Update `Schemas/AutomobileInfo.schema.json`
- [ ] Update `ApiAutomobile`, `ApiAutomobileForCreate`,
      `ApiAutomobileForUpdate` DTOs
- [ ] Update `AutomobileMappingProfile`
- [ ] Tests: serializer round-trip + manager pass-through

### Phase 9: Flutter vault store
- [ ] `IVaultStore` interface in `lib/core/data/vault/`
- [ ] `LocalVaultStore` implementation (path_provider + dart:io)
- [ ] `ApiVaultStore` implementation (Dio-backed)
- [ ] Mode-aware `vaultStoreProvider` in `repository_providers.dart`
- [ ] In-memory cache wrapper for repeat reads
- [ ] Unit tests on each impl with a tmp-dir fake

### Phase 10: Flutter — Automobile model + UI
- [ ] Extend `Automobile` domain entity with `primaryImage` +
      `images`
- [ ] Update `LocalAutomobileRepository` JSON envelope round-trip
- [ ] Update API model (`api_automobile.dart` + create/update DTOs)
      and mapper
- [ ] Image picker via `file_picker` (camera capture later if
      needed)
- [ ] Image viewer widget with thumbnail + tap-to-fullscreen
- [ ] Wire into the `AutomobileEditScreen` as a new editable card
      above the identity card

### Phase 11: Flutter — sunset old attachment code
- [ ] Mark `IAttachmentRepository` + `LocalAttachmentRepository` +
      Drift `Attachments` table deprecated; add migration that
      drops the table after one release window
- [ ] Remove `attachmentRepositoryProvider`

### Phase 12: iOS visibility
- [ ] Set `UIFileSharingEnabled = YES` and
      `LSSupportsOpeningDocumentsInPlace = YES` in
      `ios/Runner/Info.plist`
- [ ] Verify the vault appears in iOS Files app on a device

### Phase 13: Verification
- [ ] `flutter analyze` clean
- [ ] `flutter test` clean
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
| Reference attachments via `path` + sibling metadata in note JSON content | Avoids per-entity FK migrations; aligns with the existing JSON-in-note serializer pattern | 2026-05-09 |
| Implement `primaryImage` + `images[]` data shape, wire only `primaryImage` UI in v1 | Cheap to land; future gallery doesn't need a schema bump | 2026-05-09 |
| Defer EXIF strip, virus scan, server-side thumbnails | Personal-use scale; revisit when the cap stings | 2026-05-09 |
| Drop the Drift `Attachments` table | Files-on-disk-by-relative-path is the source of truth; the table was building toward a different model | 2026-05-09 |

## Status: PHASE 2 COMPLETE — design-doc gate (review by user) is the next blocker before any code.
