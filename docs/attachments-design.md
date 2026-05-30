# Attachments — Design

Owner: Flutter `hmm_console` + .NET `Hmm.ServiceApi`
Status: **Draft / pre-implementation**
Audience: anyone touching photo / file storage on either side.
Sibling doc: `docs/multi-device-cloud-sync.md` (defines the tier
model that this doc builds on).

## Why

Hmm needs to attach images and other files to notes. Attachments are
a property of the **`HmmNote`** itself, so the same mechanism covers
every note type — the first user-visible use is a car photo on a
vehicle's note, and it extends unchanged to receipts on service
records, policy PDFs on insurance policies, etc. Today there is no
attachment story on the .NET API at all, and the Flutter side has
half-built, note-keyed infrastructure that doesn't survive a
cross-device sync.

This doc picks an **Obsidian-style file vault** as the universal
fallback, *plus* two smart-reference shortcuts so we don't copy
bytes that are already syncing through the user's iCloud Photos /
OneDrive / iCloud Drive.

## Model in one sentence

> A note's attachments are a list of references. Each reference is
> either a copy in our vault, a pointer into the user's iOS Photos
> library, or a pointer into a folder they already keep in OneDrive /
> iCloud Drive. Whichever is cheapest and most stable for that
> specific photo.

## Reference shape (on the note)

Attachments belong to the **`HmmNote`**, not to whatever domain
object lives inside its `Content` — so *every* note can carry
photos/files (a vehicle, a service record, a plain-text note, all
the same way), with no per-type opt-in and no change to any domain
serializer.

They're stored in a new nullable **`attachments` column on the
`Notes` table** (a JSON string column, mirroring how `Content` and
`NoteCatalog.Schema` already store structured JSON in a column). The
column holds one object:

```jsonc
// Notes.attachments  (null when the note has no attachments)
{
  "primaryImage": {
    "kind": "phasset",
    "id": "ABC-123-DEF/L0/001",
    "originalName": "IMG_2031.HEIC",
    "contentType": "image/heic",
    "byteSize": 423112
  },
  "images": [
    { "kind": "phasset", "id": "...", ... },
    {
      "kind": "cloudFile",
      "provider": "oneDrive",
      "path": "Pictures/Cars/IMG_2031.jpg",
      "originalName": "IMG_2031.jpg",
      "contentType": "image/jpeg",
      "byteSize": 423112
    },
    {
      "kind": "vault",
      "path": "attachments/note-5/9c8a-photo.jpg",
      "originalName": "old-photo.jpg",
      "contentType": "image/jpeg",
      "byteSize": 423112
    }
  ]
}
```

Two slots: a singular `primaryImage` plus a list `images`. The two
slots are **disjoint** — a photo lives in exactly one of them, never
both; promoting a gallery image to primary moves it out of `images`.

Each reference is a tagged union on `kind`. `originalName`,
`contentType`, and `byteSize` are common to all kinds (best-effort —
`byteSize` may be `null` for `phasset`/`cloudFile` when the OS
doesn't expose it cheaply). **Only `vault` refs are guaranteed to
carry `byteSize`.** The column value validates against a small JSON
Schema (`src/Hmm.Core/Schemas/NoteAttachments.schema.json`) that
declares `byteSize` nullable; it's checked whenever the column is
written (same pattern as the per-domain content schemas).

Why a column rather than embedding in `Content`: `Content` is a raw
string for plain-text/HTML notes, so it can't carry structured
sibling fields without breaking that contract; a dedicated column
makes attachments genuinely note-universal and keeps domain
serializers untouched. (A relational child table was the alternative
considered; the JSON column wins on cost — one migration, no new
repo/manager/controller — and we don't need attachments to be
SQL-queryable. Revisit a child table if orphan-GC or dedup ever
needs server-side queries.)

## Reference kinds and resolvers

| Kind | Where the bytes live | Read by | Cross-device sync mechanism | Survives Free → Paid migration? |
| --- | --- | --- | --- | --- |
| `vault` | App vault folder (per tier — see below) | `IVaultStore` | Local: none. cloudStorage: OneDrive client. cloudApi: API endpoints. | Yes — already vault-shaped. |
| `phasset` | iOS Photos library | `PhotoKitResolver` (PhotoKit on iOS via `photo_manager` package) | iCloud Photos (free, Apple-side, **iOS↔iPadOS↔macOS only**) | No — must be **resolved + uploaded** to `vault` before the switch completes. |
| `cloudFile` | User's cloud-synced folder (OneDrive root, iCloud Drive root) | `CloudFileResolver` (OS-level file paths; Microsoft Graph for OneDrive on mobile if folder isn't locally mounted) | The cloud provider's own file sync | No — must be **resolved + uploaded** to `vault` before the switch completes. |

The picker decides which kind to write per photo:
- Came from the iOS Photos picker → `phasset`.
- Picked from a known cloud-folder root (`<OneDrive>/...` or
  `<iCloudDrive>/...` detected by path prefix) → `cloudFile`.
- Anything else → copy into the vault → `vault`.

Users can always force `vault` (a "make a permanent copy" toggle in
the picker) for photos they expect to delete from Photos.

## Vault layout (for `kind: vault` only)

The Obsidian-style vault — used for the universal fallback and
exclusively in `cloudApi`.

```
<vault root>/
└── attachments/
    └── note-{noteId}/
        └── {uuid}.{ext}
```

Per-note subfolder (deleting a note removes the whole folder).
UUID-stamped filenames so simultaneous picks with the same source
name don't collide. POSIX separators on every platform.

## Tier × transport

| Tier | `vault` root | `phasset` resolver | `cloudFile` resolver |
| --- | --- | --- | --- |
| `local` | `<app docs>/vault/` | iOS only — PhotoKit | iOS / macOS — file path detection |
| `cloudStorage` | `<OneDrive>/Hmm/vault/` | iOS only — PhotoKit | OneDrive folder detection on every platform |
| `cloudApi` | `/var/lib/hmm-vault/{authorId}/` on the VPS, served via API | n/a — see migration | n/a — see migration |

`phasset` and `cloudFile` only exist in the free tiers. The paid
tier is **vault-only**, by construction, because the .NET server
can't read into the user's photo library or arbitrary cloud folder
on a device far away.

### cloudStorage byte sync is desktop-only (deliberate — 2026-05-30)

The `cloudStorage` `vault` root is `<OneDrive>/Hmm/vault/` **on
desktop only**. iOS/Android have no OS-mounted OneDrive folder, so
the vault falls back to `<app docs>/vault/` and the **bytes are not
replicated** — only the note JSON + the `vault` reference sync (via
the Graph AppFolder, Phase 11.5). Consequence:

- **Desktop → any device**: a desktop device's vault sits inside the
  synced OneDrive folder, so the OS OneDrive client uploads the
  bytes; other devices read them. Photos sync.
- **Mobile-only (e.g. two iPhones on the free tier)**: bytes never
  leave the originating phone. The second device pulls the note +
  ref and shows the render-time placeholder below.

**This is intentional, not a bug.** Full cross-device photo sync —
including mobile-to-mobile — is a **`cloudApi` (paid) feature**,
where every device uploads/reads bytes through the API vault. We
deliberately do **not** push `cloudStorage` bytes through the Graph
API: that would re-introduce a parallel byte-sync layer into the
free tier (a byte manifest + diff, resumable upload sessions for the
4–8 MB photos that exceed Graph's 4 MB simple-PUT limit, and remote
orphan deletes) — complexity the paid tier already absorbs. The free
tier's promise is therefore: **notes everywhere; photos everywhere
on desktop, on the originating device on mobile.**

## Render-time fallback

When a viewing device can't resolve a reference (wrong OS, photo
deleted from source, permission revoked, cloud folder not mounted,
or — on the free `cloudStorage` tier — the `vault` bytes haven't
reached this device because the origin was mobile; see "cloudStorage
byte sync is desktop-only" above):
- Render a placeholder thumbnail with an icon.
- Show a one-line reason ("This photo lives in iCloud Photos and
  isn't accessible on this device").
- Surface a **Replace** action that lets the user pick again — the
  new pick goes into `vault` so it works everywhere afterwards.

This is the same UI in every reason-it-failed case.

## API surface (`Hmm.ServiceApi`, `cloudApi` only)

Auth: bearer JWT. Per-note routes are gated by the existing
"does the JWT subject own this note?" check that the rest of the
note endpoints already use, so the authorId scope is implicit in
the note ownership — no separate prefix dance needed.

### Per-note vault endpoints (the normal case)

Vault files belong to a specific note, so the routes nest under
`/v1/notes/{noteId}/vault/`. `{filename}` is the within-note file
name (e.g. `9c8a3f12-7d6e-4a8b-90d1-2b4e5a6f7c01.jpg`) — declared
catchall so future subfoldering within a note (thumbnails,
variants) doesn't require a new route shape.

| Verb | Path | Purpose |
| --- | --- | --- |
| `POST` | `/v1/notes/{noteId}/vault/{*filename}` | Upload bytes. Body: file. Headers: `Content-Type`, `Content-Length`. Returns `{path, contentType, byteSize}` echoing the now-canonical metadata; `path` is the full vault relative path (`attachments/note-{noteId}/{filename}`). Idempotent on path collision (overwrite). |
| `GET` | `/v1/notes/{noteId}/vault/{*filename}` | Stream bytes. Sets `Content-Type` from server-stored value. |
| `HEAD` | `/v1/notes/{noteId}/vault/{*filename}` | Existence + size check. |
| `DELETE` | `/v1/notes/{noteId}/vault/{*filename}` | Delete a single file. Returns `204`. |
| `GET` | `/v1/notes/{noteId}/vault` | List every vault file for one note. Used by per-note GC and the Replace flow. |

`VaultRef.path` in `Notes.attachments` stays the full POSIX vault
path (`attachments/note-{noteId}/{filename}`) — self-contained, no
data-model migration needed. The Flutter `ApiVaultStore` (Phase 15)
parses the path to extract `noteId` + `filename` when building URLs.

### Cross-note bulk endpoints (migration only)

Migration is the only place that needs a view across all of a
user's notes. These stay at the flat `/v1/migration/` prefix
because nesting them under a single note doesn't fit:

| Verb | Path | Purpose |
| --- | --- | --- |
| `POST` | `/v1/migration/upload` | Bulk push notes + vault bytes from a local-mode user (Free → Paid). |
| `GET` | `/v1/migration/export` | Stream the vault as a zip alongside the record JSON. Available in `Lapsed` state too. |
| `POST` | `/v1/migration/replace` | Wipe the user's vault on the server before re-upload. |
| `GET` | `/v1/migration/manifest` | List every vault file across the user's notes. Used by server-side GC + audit. |

Subscription gating per `docs/multi-device-cloud-sync.md`:
- `Active` — full read/write on the per-note endpoints.
- `Grace` — read + delete only on per-note endpoints (write
  returns `402`).
- `Lapsed` / `Cancelled` — read only via
  `GET /v1/migration/export`, not via the per-note endpoints.

The migration endpoints (`/v1/migration/upload`,
`/v1/migration/export`, `/v1/migration/replace` — defined in the
sync doc) extend to include vault contents and are responsible for
turning `phasset` / `cloudFile` references into `vault` references
during a Free → Paid upgrade (see "Migration alignment" below).

## Storage policy

| Policy | v1 default | Notes |
| --- | --- | --- |
| Allowed MIME types | `image/jpeg`, `image/png`, `image/heic`, `image/webp` | Server rejects others with `415 Unsupported Media Type`. |
| Max upload size | 8 MB | Server returns `413 Payload Too Large`. |
| Max original dimensions | 4096px on the long edge | Larger images are downsized server-side. |
| EXIF | Pass-through in v1 | Stripping is on the v2 list; document for the privacy-conscious. |
| Virus scan | Out of scope v1 | Add when we accept non-image MIME types. |
| Server-generated thumbnails | Out of scope v1 | Client resizes on the fly. Add server thumbnails when grids feel slow. |

The same MIME / size limits apply at pick-time on the client for
all kinds — even `phasset` and `cloudFile` references must point at
acceptable types so the eventual upgrade upload doesn't fail.

### Link vs. copy, and downsize-on-copy (decided 2026-05-30)

A picked image is always **copied** into the vault — we do **not**
offer a "link to the photo in your library instead of copying" toggle
for the `vault` kind. Copy is the default and only v1 behaviour
because it gives a hard guarantee: *your car photo survives whatever
you do in Photos* (delete it, toggle iCloud "Optimize Storage",
revoke permission). A link (`phasset`) trades that guarantee for disk
space and creates a debt that must be paid — and can fail — at the
Free → Paid boundary (every non-vault ref has to be resolved to bytes
and uploaded; if the source is gone, it's unresolvable). For a
vehicle-records app (a few photos per car), the storage saved by
linking doesn't justify the fragility + support cost.

The legitimate "don't waste storage" concern is instead handled by
**downsize-on-copy**: before writing to the vault the client shrinks
the image to a long-edge cap (default **2048 px**, **JPEG q85**) via
native codecs (`flutter_image_compress` → `ImageDownsizer`). This
keeps the copy guarantee while cutting a 2–5 MB phone photo to a few
hundred KB, and transcodes HEIC → JPEG as a side benefit (so HEIC
never reaches an Android `cloudApi` viewer). Implemented in
`VaultImageAttachmentPicker.persistToVault`; the `VaultRef`'s
`byteSize` / `contentType` reflect the stored (downsized) bytes. The
downsizer is injected, defaulting to a no-op for headless/test
construction; production wires `NativeImageDownsizer`.

The `phasset` / `cloudFile` smart-reference *kinds* remain in the
model (Phases 16/17, parked) for a possible future large-gallery use
case — but only ever as an explicit, warned, Phase-18-gated option,
never the default.

## Migration alignment (the critical bit)

Per `docs/multi-device-cloud-sync.md`:

### Free → Paid upload
The client iterates every note's attachment refs and resolves
non-vault kinds **before** the switch flips:

1. For each `phasset` ref → load bytes via PhotoKit, write to a
   temp `vault://...` path, replace the ref in the note's
   `attachments` column with the new `vault` reference.
2. For each `cloudFile` ref → load bytes via the OS / Graph SDK,
   same swap.
3. Then upload all `vault` files via
   `POST /v1/notes/{noteId}/vault/{filename}` alongside the JSON
   push (or bulk via `POST /v1/migration/upload`).
4. Counts surfaced in the consent dialog:
   "This will copy 8 photos from your camera roll and 3 from
    OneDrive into Hmm's cloud (14 MB). Continue?"

Any unresolvable ref (deleted from source, permission denied) is
flagged in the consent dialog so the user knows what they'll lose
before they confirm.

### Paid → Local snapshot
The vault comes back as `vault://...` references. The original
`phasset` / `cloudFile` refs are not restored — they were rewritten
during upgrade and we don't know how to find them again. User can
re-link via the **Replace** action if they care.

### Lapsed export
Same as the snapshot — references in the export are all `vault://`.

### `MigrationLog.RecordCounts`
```json
{
  "automobiles": 3,
  "gasLogs": 42,
  "vaultFiles": 8,
  "vaultBytes": 14680064,
  "resolvedPhAssets": 5,
  "resolvedCloudFiles": 2,
  "unresolvedRefs": 1
}
```

## iOS / Android visibility (vault folder)

| Platform | Vault visible to user via OS file browser? | What we do |
| --- | --- | --- |
| iOS | No by default | Set `UIFileSharingEnabled = YES` and `LSSupportsOpeningDocumentsInPlace = YES` in `Info.plist` so the vault appears in Files app. |
| Android | Yes if vault lives in external storage | Vault inside app container by default; "expose to Files" toggle in Settings (defer to v2 — needs MANAGE_EXTERNAL_STORAGE handling). |
| macOS / Windows / Linux | Yes — vault is a normal folder | No-op. |

## Backup story

Extend `docker/hmm-deploy.sh --backup` to:

1. `pg_dump HmmNotes` (existing).
2. `pg_dump HmmIdp` (existing).
3. `tar -czf vault-{timestamp}.tar.gz /var/lib/hmm-vault/` (new).

Restore is the reverse. Document the order in the deploy script's
help: pg first, then vault, because record references must exist
before files are placed.

## Flutter implementation

### Stack
- `AttachmentRef` sealed class in `lib/core/data/attachments/`:
  - `VaultRef(path, originalName, contentType, byteSize)`
  - `PhAssetRef(id, originalName, contentType, byteSize)`
  - `CloudFileRef(provider, path, originalName, contentType,
    byteSize)`
- `IAttachmentResolver` — resolves any `AttachmentRef` to bytes for
  display. Implementations:
  - `VaultResolver` — delegates to `IVaultStore`.
  - `PhAssetResolver` — uses `photo_manager` package; iOS-only,
    returns `null` on other platforms (caller falls back to
    placeholder).
  - `CloudFileResolver` — OS-level file read for paths inside
    detected cloud roots.
- `IVaultStore` interface in `lib/core/data/vault/`:
  - `Future<void> putBytes(String relativePath, Uint8List bytes, {String contentType})`
  - `Future<Uint8List> getBytes(String relativePath)`
  - `Future<bool> exists(String relativePath)`
  - `Future<void> delete(String relativePath)`
  - `Future<List<VaultEntry>> list(String prefix)`
- Three vault implementations:
  - `LocalVaultStore` — `path_provider` + `dart:io`.
  - `OneDriveVaultStore` — picks up the OneDrive folder root once
    OneDrive integration lands; otherwise alias for
    `LocalVaultStore` and rely on the OS-level OneDrive client.
  - `ApiVaultStore` — Dio-backed; parses `VaultRef.path`
    (`attachments/note-{id}/{filename}`) to extract `noteId` + the
    within-note filename, then calls
    `/v1/notes/{noteId}/vault/{filename}`.
- Mode-aware provider in `repository_providers.dart` returns the
  right vault store based on `dataModeProvider`.

### Note storage
- New nullable `attachments` text column on the Drift `Notes` table
  (a Drift migration); holds the `{ "primaryImage": ..., "images":
  [...] }` JSON described above.
- The `HmmNote` model gains `AttachmentRef? primaryImage` +
  `List<AttachmentRef> images`; `LocalNoteRepository` round-trips
  the column via the `AttachmentRef` JSON codec.
- Domain entities that display attachments (e.g. `Automobile`)
  surface `primaryImage` / `images` as a **read-through projection
  of the owning note**; on save, the domain repo writes them into
  the note's `attachments` column alongside the serialized content.
  No attachment data lives inside the domain payload.
- The half-built `Attachments` Drift table + `IAttachmentRepository`
  + `local_attachment_repository.dart` + `attachmentRepositoryProvider`
  are removed (they're unused) — the column-on-`Notes` model
  replaces them.

### Picker
- iOS / Android: `image_picker` (gives PHAsset on iOS, MediaStore on
  Android). Add to `pubspec.yaml`.
- Detection: after pick, if iOS and we got a PHAsset id, build
  `PhAssetRef`. Otherwise, take the file path; if it sits under a
  detected cloud folder root, build `CloudFileRef`; else copy into
  vault and build `VaultRef`.
- Cloud folder root detection (best-effort, per platform):
  - macOS: `~/Library/Mobile Documents/com~apple~CloudDocs/` and
    `~/OneDrive/`.
  - iOS: NSFileManager URLForUbiquityContainer for iCloud Drive
    (own container only — system-wide root requires entitlements
    we likely don't want).
  - Windows: `%OneDrive%` env var, `%USERPROFILE%\iCloudDrive\`.
  - Linux: defer (no native OneDrive client).
- A "Make a permanent copy" toggle in the picker forces `VaultRef`
  for users who want stability over efficiency.

### Display
- `AttachmentImage(AttachmentRef ref)` widget routes to the right
  resolver, shows shimmer while loading, falls back to placeholder
  + Replace button on resolution failure.

## .NET implementation

### Stack
- `IVaultBlobStore` abstraction in a **new `Hmm.Core.Vault`
  project** (created up front — the interface, the filesystem impl,
  the `VaultRef` value object, and their tests already justify a
  project, and splitting it out later means relocating public types
  across packages).
- `FilesystemVaultBlobStore` reads/writes
  `${AttachmentSettings.RootDir}/{authorId}/{relativePath}`.
- `AttachmentSettings` — `RootDir`, `MaxBytes`,
  `AllowedContentTypes`, `MaxLongEdgePixels`, bound from
  `appsettings`.
- `NoteVaultController` (route `[ApiVersion("1.0")]
  [Route("v{version:apiVersion}/notes/{noteId:int}/vault")]`)
  exposes the per-note endpoints listed above. Migration-tier
  bulk endpoints live on a separate `MigrationController`.
- `RequireActiveSubscriptionAttribute` (defined in the sync doc)
  decorates the write endpoints.

### `HmmNote` attachments
- `HmmNote` (`src/Hmm.Core.Map/DomainEntity/HmmNote.cs`) gains
  `VaultRef? PrimaryImage` + `IList<VaultRef> Images`.
- `HmmNoteDao` gains a nullable `string? Attachments` column → one
  EF migration: add `Attachments NVARCHAR(MAX) NULL` to `Notes`
  (and the SQLite / PostgreSQL equivalents).
- The AutoMapper profile maps the JSON column ↔ the two domain
  properties via a value converter that uses the `NoteAttachments`
  codec.
- On write, the serialized column is validated against
  `NoteAttachments.schema.json` (same place the per-domain content
  schemas live); invalid → `ProcessingResult` failure.
- `ApiNote`, `ApiNoteForCreate`, `ApiNoteForUpdate` surface
  `primaryImage` + `images`; `ApiMappingProfile` maps them; the
  note result filters pass them through.
- Domain modules that want a flattened view (e.g. an `ApiAutomobile`
  that shows the car's photo) project from the owning note's fields
  — they don't add their own storage.

### `VaultRef` value object
The .NET side only knows about `VaultRef` (`{Path, OriginalName,
ContentType, ByteSize}`) and lives in the `Hmm.Core.Vault` project.
It never sees `phasset` / `cloudFile` references — those are
client-side concepts that get rewritten to `vault` during the
migration upload.

The `attachments` column and the `ApiNote*` DTOs carry `VaultRef`
**directly** — not a polymorphic `AttachmentRef` with a `kind`
discriminator. There is no non-vault shape on the wire to .NET, so a
`phasset` / `cloudFile` payload deserializes to a `VaultRef` with a
null `Path` and is rejected by `NoteAttachments.schema.json` — it
can never become a valid server-side object. (Belt-and-braces: the
codec also rejects an explicit `kind` that isn't `vault` with a
`400`, in case a hand-crafted payload sets `path` *and*
`kind: phasset`.)

### Server-side image processing
- Use `SkiaSharp` (cross-platform, no native deps on Linux Docker)
  for the downsize step.
- Apply only on `image/*` MIME and only when source dimensions
  exceed the cap; pass through originals that already fit.

## Out of scope (deliberately)

- Real-time push notifications when a peer uploads a new image.
- Per-attachment ACLs (every attachment is owned by the note's
  author, period).
- Collaborative editing of the same image.
- EXIF stripping, virus scanning, content moderation.
- Server-generated thumbnails / responsive variants.
- Selective sync.
- Cross-account sharing.
- Cross-OS portability of `phasset` (iOS-only by Apple's design —
  user uploading on iPhone and viewing on Android stays on a
  placeholder until they Replace).
- Android `cloudFile` resolver (Android cloud-folder UX is messier;
  defer until a real user asks).

These can be added later without breaking the tagged-union model.

## Implementation order

Each step ships independently.

1. **Spec the path utility shared between client and server.**
   POSIX-style joins, sanitisation rules.
2. **Spec the JSON schemas shared between client and server**: the
   `AttachmentRef` tagged union, and the `NoteAttachments` wrapper
   (`{ primaryImage, images }`).
3. **.NET: new `Hmm.Core.Vault` project — `IVaultBlobStore` +
   `FilesystemVaultBlobStore` + `VaultRef` + tests.**
4. **.NET: `NoteVaultController` (routes nested under
   `/v1/notes/{noteId}/vault`) + `AttachmentSettings` + DI wiring.**
5. **.NET: server-side image downsize on upload.**
6. **.NET: `Notes.attachments` column — `HmmNoteDao` column + EF
   migration + `HmmNote.PrimaryImage`/`Images` + AutoMapper
   converter + `NoteAttachments.schema.json` validation + `ApiNote*`
   DTOs + result-filter pass-through + tests.**
7. **.NET: `/v1/migration/{upload,replace}` (multipart) +
   `/v1/migration/export` (zip) + `/v1/migration/log`; new
   `MigrationLog` table, `IMigrationManager`,
   `MigrationController`. Vault bytes ride alongside the manifest
   on upload/replace, and the export zip contains `records.json`
   at its root + every vault file at its full
   `attachments/note-N/...` path.**
8. **.NET: `api-vault-data` Docker volume on `hmm-api` mounted at
   `/var/lib/hmm-vault`; `hmm-deploy.sh --backup` produces
   `hmm-vault-<ts>.tar.gz` alongside the pg dumps, and `--help`
   documents the restore order (Postgres first, vault second).**
9. **Flutter: `AttachmentRef` sealed class + JSON codec + the
   `NoteAttachments` wrapper codec.**
10. **Flutter: `IVaultStore` interface + `LocalVaultStore`.**
11. **Flutter: `attachments` column on the Drift `Notes` table +
    Drift migration; `HmmNote` model gains `primaryImage`/`images`;
    `LocalNoteRepository` round-trips it; remove the old
    `Attachments` table + `IAttachmentRepository` +
    `local_attachment_repository.dart` + `attachmentRepositoryProvider`.**
12. **Flutter: surface read-through `primaryImage`/`images` on the
    `Automobile` entity; the local automobile repo writes them to
    the owning note's `attachments` column on save.**
13. **Flutter: `image_picker` integration + the picker →
    `AttachmentRef` decision logic. v1 picker only emits `VaultRef`
    until the PHAsset resolver lands; bytes always copied into vault
    for safety.**
14. **Flutter: `VaultResolver` (renders `VaultRef`) + image picker /
    viewer widget on the vehicle screen.** ← first visible feature
15. **Flutter: `ApiVaultStore` + mode-aware vault-store provider.**
16. **Flutter: `PhAssetResolver` (iOS) — picker now emits
    `PhAssetRef` instead of copying.**
17. **Flutter: `CloudFileResolver` for macOS / Windows OneDrive /
    iCloud Drive — picker now emits `CloudFileRef` when applicable.**
18. **Flutter: Free → Paid migration extension — resolve all
    non-vault refs to vault before upload.**
19. **iOS: set `Info.plist` flags so the vault is browsable in
    Files.**

Steps 1–8 ship without any client UX change. Step 14 is the first
visible feature (vault-only photos, local mode). Step 15 lights up
the paid tier. Steps 16–17 are the smart-reference power-ups; the
app works without them, just less efficiently. Step 18 is required
*before* anyone in `phasset` / `cloudFile` mode can upgrade to paid
— gate it.

**The Flutter local-mode vertical slice** (the chosen first body of
work) is steps 1 → 2 → 9 → 10 → 11 → 12 → 13 → 14. No .NET work
required.
