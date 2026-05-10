# Attachments — Design

Owner: Flutter `hmm_console` + .NET `Hmm.ServiceApi`
Status: **Draft / pre-implementation**
Audience: anyone touching photo / file storage on either side.
Sibling doc: `docs/multi-device-cloud-sync.md` (defines the tier
model that this doc builds on).

## Why

Hmm needs to attach images and other files to notes — starting with
a car photo on `AutomobileInfo` and growing to receipts on service
records, policy PDFs on insurance policies, etc. Today there is no
attachment story on the .NET API at all, and the Flutter side has
half-built infrastructure that doesn't survive a cross-device sync.

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

## Reference shape (inside note content)

A tagged union. Stored as siblings of the existing fields in the
note's JSON content. Two slots: a singular `primaryImage` plus a
list `images`.

```jsonc
{
  "note": {
    "content": {
      "AutomobileInfo": {
        "vin": "...",
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
    }
  }
}
```

`originalName`, `contentType`, and `byteSize` are common to all
kinds (best-effort — `byteSize` may be `null` for `phasset`/`cloudFile`
when the OS doesn't expose it cheaply).

The reference shape generalises to other note types — receipts on
service records, policy PDFs on insurance policies — by adding the
same two field names.

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

## Render-time fallback

When a viewing device can't resolve a reference (wrong OS, photo
deleted from source, permission revoked, cloud folder not mounted):
- Render a placeholder thumbnail with an icon.
- Show a one-line reason ("This photo lives in iCloud Photos and
  isn't accessible on this device").
- Surface a **Replace** action that lets the user pick again — the
  new pick goes into `vault` so it works everywhere afterwards.

This is the same UI in every reason-it-failed case.

## API surface (`Hmm.ServiceApi`, `cloudApi` only)

Auth: bearer JWT. Server prefixes every path with `/{authorId}/` so
the JWT subject scopes the vault namespace; clients never see the
prefix.

| Verb | Path | Purpose |
| --- | --- | --- |
| `POST` | `/v1/vault/{*relativePath}` | Upload bytes. Body: file. Headers: `Content-Type`, `Content-Length`. Returns `{path, contentType, byteSize}` echoing the now-canonical metadata. Idempotent on path collision (overwrite). |
| `GET` | `/v1/vault/{*relativePath}` | Stream bytes. Sets `Content-Type` from server-stored value. |
| `HEAD` | `/v1/vault/{*relativePath}` | Existence + size check. |
| `DELETE` | `/v1/vault/{*relativePath}` | Delete a single file. Returns `204`. |
| `GET` | `/v1/vault?prefix={prefix}` | List metadata under a prefix. Used by migration / GC. |

Subscription gating per `docs/multi-device-cloud-sync.md`:
- `Active` — full read/write.
- `Grace` — read + delete only (write returns `402`).
- `Lapsed` / `Cancelled` — read only via `GET /v1/migration/export`,
  not via the bare vault endpoints.

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

## Migration alignment (the critical bit)

Per `docs/multi-device-cloud-sync.md`:

### Free → Paid upload
The client iterates every note's attachment refs and resolves
non-vault kinds **before** the switch flips:

1. For each `phasset` ref → load bytes via PhotoKit, write to a
   temp `vault://...` path, replace the ref in note content with the
   new `vault` reference.
2. For each `cloudFile` ref → load bytes via the OS / Graph SDK,
   same swap.
3. Then upload all `vault` files via `POST /v1/vault/{path}`
   alongside the JSON push.
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
  - `ApiVaultStore` — Dio-backed `/v1/vault/{path}` calls.
- Mode-aware provider in `repository_providers.dart` returns the
  right vault store based on `dataModeProvider`.

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

### Cleanup
- Drift `Attachments` table and `local_attachment_repository.dart`
  go away (or are reduced to a deprecation stub).

## .NET implementation

### Stack
- `IVaultBlobStore` abstraction in `Hmm.Core` (or a new
  `Hmm.Core.Vault` project if the area grows).
- `FilesystemVaultBlobStore` reads/writes
  `${AttachmentSettings.RootDir}/{authorId}/{relativePath}`.
- `AttachmentSettings` — `RootDir`, `MaxBytes`,
  `AllowedContentTypes`, `MaxLongEdgePixels`, bound from
  `appsettings`.
- `VaultController` exposes the endpoints listed above.
- `RequireActiveSubscriptionAttribute` (defined in the sync doc)
  decorates the write endpoints.

### `VaultRef` value object
The .NET side only knows about `VaultRef` (`{Path, OriginalName,
ContentType, ByteSize}`). It never sees `phasset` / `cloudFile`
references — those are client-side concepts that get rewritten to
`vault` during the migration upload. The serializer should reject
non-`vault` kinds at the API boundary with a `400` (defensive — the
client should already have rewritten them).

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
2. **Spec the `AttachmentRef` JSON schema** (the tagged union)
   shared between client and server.
3. **.NET: `IVaultBlobStore` + `FilesystemVaultBlobStore` + tests.**
4. **.NET: `VaultController` + `AttachmentSettings` + DI wiring.**
5. **.NET: server-side image downsize on upload.**
6. **.NET: extend `/v1/migration/{upload,export,replace}` for
   vault contents.**
7. **.NET: extend `hmm-deploy.sh --backup` to tar the vault dir +
   add `/var/lib/hmm-vault` Docker volume.**
8. **Flutter: `AttachmentRef` sealed class + JSON codec.**
9. **Flutter: `IVaultStore` interface + `LocalVaultStore`.**
10. **Flutter: `ApiVaultStore` + mode-aware provider.**
11. **Flutter: `VaultResolver` (renders `VaultRef`).**
12. **Flutter: `image_picker` integration + the picker → `AttachmentRef`
    decision logic. v1 picker only emits `VaultRef` until the
    PHAsset resolver lands; bytes always copied into vault for
    safety.**
13. **Flutter: `PhAssetResolver` (iOS) — picker now emits
    `PhAssetRef` instead of copying.**
14. **Flutter: `CloudFileResolver` for macOS / Windows OneDrive /
    iCloud Drive — picker now emits `CloudFileRef` when applicable.**
15. **Flutter: extend `Automobile` domain entity with `primaryImage`
    + `images`; serialize round-trip.**
16. **Flutter: image picker + viewer widget on the vehicle screen.**
17. **Flutter: Free → Paid migration extension — resolve all
    non-vault refs to vault before upload.**
18. **iOS: set `Info.plist` flags so the vault is browsable in
    Files.**
19. **Sunset: deprecate the Drift `Attachments` table and the
    `IAttachmentRepository` interface; remove after one release if
    nothing depends on it.**

Steps 1–7 ship without any client UX change. Step 16 is the first
visible feature (vault-only photos). Steps 13–14 are the
smart-reference power-ups; the app works without them, just less
efficiently. Step 17 is required *before* anyone in `phasset` /
`cloudFile` mode can upgrade to paid — gate it.
