# Findings

## Research Notes — current state

### Flutter side
- `lib/core/data/local/local_attachment_repository.dart` already
  implements an asset-catalog model: `Attachments` Drift table +
  bytes on disk, with soft-delete tombstones. **Under the chosen
  tagged-union design, this layer is unnecessary** — we'll
  downgrade it (or remove it) in favour of files-on-disk addressed
  by relative path + smart references for sources that already
  sync via Apple / Microsoft.
- `lib/core/data/local/database.dart` declares the `Attachments`
  table at line 66.
- `lib/core/data/repository_providers.dart` exposes a stub
  `attachmentRepositoryProvider` that throws for `cloudApi` (line
  47) — also obsolete under the new model.
- `pubspec.yaml`: `file_picker: ^8.0.0` and `path_provider: ^2.x`
  are present. Need to add `image_picker` (Phase 11) and
  `photo_manager` (Phase 13).

### .NET side
- `HmmNote` (`src/Hmm.Core.Map/DomainEntity/HmmNote.cs`) carries
  only `Subject`, `Content` (JSON string), `Catalog`, `Author`,
  `Tags`, `IsDeleted`, etc. No file/blob storage today.
- No `Attachment` / `Image` / `Media` entity in `Hmm.Core`,
  `Hmm.Core.Map`, or `Hmm.Core.Dal.EF`.
- `AutomobileInfo` has no image / photo / picture / attachment
  fields today.
- `docker/hmm-deploy.sh --backup` only `pg_dump`s — needs to learn
  to tar the vault directory once one exists.

### How established note apps handle media (prior art)

| App | Bytes location | Reference style | Multi-device |
| --- | --- | --- | --- |
| Obsidian | Plain files in vault folder (`attachments/`) | Markdown `![[image.png]]` | Whatever syncs the vault (iCloud / Sync / Git / Syncthing) |
| OneNote | Embedded in proprietary section/page format, server-stored when synced | Internal asset id | Microsoft-hosted SaaS |
| Apple Notes / Journal | CloudKit asset catalog + on-device cache | Stable internal asset id | iCloud assets |
| Notion | Notion's CDN | URL on a "image block" | Cloud-only |
| Bear | App container + CloudKit assets | Embedded references | iCloud |
| Logseq | Files in graph dir, like Obsidian | Markdown | File sync |

Universal pattern: bytes live somewhere addressable by every
viewing device. **None store URI-only references** to user photo
libraries because of cross-device portability + permission
fragility.

We borrow Obsidian's vault as the universal fallback and bolt on
two smart-reference shortcuts (PHAsset, cloud-folder file) so we
don't duplicate bytes when Apple / Microsoft are already syncing
them.

## Chosen design — tagged-union references on top of an Obsidian vault

### Reference kinds

| Kind | Where the bytes live | Read by | Cross-device sync |
| --- | --- | --- | --- |
| `vault` | App vault folder (per tier) | `IVaultStore` | Local: none. cloudStorage: OneDrive client. cloudApi: API endpoints. |
| `phasset` | iOS Photos library | `PhotoKitResolver` | iCloud Photos (iOS↔iPadOS↔macOS only) |
| `cloudFile` | User's cloud-synced folder root (OneDrive / iCloud Drive) | `CloudFileResolver` | Provider's own file sync |

The picker decides which kind to write per photo. Vault is the
universal fallback. The paid `cloudApi` tier is vault-only — the
Free → Paid upgrade resolves all non-vault refs into vault refs
before upload, then uploads bytes via API.

### Vault layout (for `kind: vault` only)

```
<vault root>/
└── attachments/
    └── note-{noteId}/
        └── {uuid}.{ext}
```

Per-note subfolder, UUID-stamped filenames, POSIX paths.

### Tier-to-vault-transport mapping

| Tier | Vault root | Cross-device sync |
| --- | --- | --- |
| `local` | App container, e.g. `<app docs>/vault/` | none |
| `cloudStorage` | OneDrive folder, e.g. `<OneDrive>/Hmm/vault/` | OS-level OneDrive client; "free" multi-device |
| `cloudApi` | `/var/lib/hmm-vault/{authorId}/` on the VPS | API endpoints `POST/GET/DELETE /v1/vault/{path}` |

## Why this beats alternatives

- **Cheaper than the asset-catalog model** — no per-attachment DB
  rows, no schema migration, no per-attachment FK.
- **`cloudStorage` multi-device sync becomes free** — OneDrive's
  client handles file-level sync.
- **Smart shortcuts save bytes for the common iOS case** — if the
  user takes a photo with their phone (which is the common case),
  iCloud Photos already has it on every Apple device; the app just
  remembers the asset id.
- **Universal fallback (`vault`) means the app always works**
  even when the smart shortcut is unavailable (Android, photo
  deleted, permission revoked, viewing in cloudApi mode).

## Trade-offs to live with

1. **iOS hides the vault by default.** Sandboxed apps don't expose
   a user-browsable folder unless we set `UIFileSharingEnabled` +
   `LSSupportsOpeningDocumentsInPlace` in Info.plist. Phase 16
   does this.
2. **`phasset` is iOS-only by Apple's design.** A user uploading on
   iPhone and viewing on Android stays on the placeholder until
   they Replace.
3. **OneDrive sync of many small files is slow.** Acceptable for
   "a few photos per car"; documented as future concern.
4. **Reference rot** — user deletes the source from Photos /
   OneDrive, the app shows a "missing source" placeholder + Replace
   button. The Replace flow always copies into vault, fixing the
   reference for good.
5. **Free → Paid upgrade has a resolution step**: every non-vault
   ref is resolved + bytes uploaded to vault before the switch.
   Surface in consent dialog ("Resolving 8 iCloud photos and
   uploading 14 MB...").
6. **No automatic dedup or thumbnails.** Same photo stored twice =
   stored twice. Server-side improvements can be bolted on.
7. **Conflict resolution under simultaneous edits** is file-system
   level (last writer wins) — fine for personal-use single-user.

## Tier alignment with the multi-device sync doc

`docs/multi-device-cloud-sync.md` defines the three migration
scenarios. Attachments slot in:

- **Free → Paid upload**: resolves non-vault refs into vault refs,
  then uploads vault files alongside the JSON push. Consent
  dialog should mention `"X photos, Y cloud-folder files, Z MB"`.
- **Paid → Local snapshot**: pulls vault files. Refs stay `vault`
  kind (originals from `phasset` / `cloudFile` were lost during
  upgrade — user can Replace if they want).
- **Lapsed export**: same — export endpoint streams the vault
  alongside the JSON dump.

## Key Files

| Component | Path |
| --- | --- |
| Multi-device sync sibling doc | `docs/multi-device-cloud-sync.md` |
| Attachments design doc | `docs/attachments-design.md` |
| AutomobileInfo domain | `src/Hmm.Automobile/DomainEntity/AutomobileInfo.cs` |
| AutomobileInfo serializer | `src/Hmm.Automobile/NoteSerialize/AutomobileJsonNoteSerialize.cs` |
| AutomobileInfo schema | `src/Hmm.Automobile/Schemas/AutomobileInfo.schema.json` |
| HmmNote domain | `src/Hmm.Core.Map/DomainEntity/HmmNote.cs` |
| Hmm-deploy backup script (needs vault tar) | `docker/hmm-deploy.sh` |
| Flutter Drift `Attachments` table (to deprecate) | `~/Projects/hmm_console/lib/core/data/local/database.dart:66` |
| Flutter local attachment repo (to deprecate) | `~/Projects/hmm_console/lib/core/data/local/local_attachment_repository.dart` |

## Open Questions (before code)

- **Backup of large vaults** — `pg_dump` is fast; tar of a multi-GB
  vault is not. Acceptable for v1, or do we need incremental
  backup? Recommend incremental for v2; v1 takes the full tar.
- **Vault visibility on iOS** — set `UIFileSharingEnabled` + the
  documents-in-place flag? Recommend yes (Phase 16).
- **Server-side image policing** (max original dimensions, EXIF
  stripping, virus scan) — defer EXIF + virus scan; enforce
  max-pixels + MIME at upload time on day one.
- **PHAsset stability across iCloud Photos** — empirically the
  PHAsset `localIdentifier` is stable across the same Apple ID's
  devices, but Apple doesn't formally guarantee this. Worth a
  mention in the user-facing docs ("Replace if the image
  disappears").
- **Cloud-folder root detection on Android** — Android's cloud-app
  ecosystem is messier than iOS / macOS / Windows. v1 detects on
  iOS / macOS / Windows only; Android falls back to vault.
