# Progress Log

## Session: 2026-05-09 — image/media for HmmNote (car photo on AutomobileInfo)

### Completed
- [x] Reset planning files (previous session was a finished
      reverse-geocoding feature; not relevant here)
- [x] Recon both codebases:
  - Flutter has `Attachments` Drift table +
    `local_attachment_repository.dart` (file-on-disk + DB-row
    metadata, soft-delete tombstones).
  - .NET API has zero attachment infrastructure today.
- [x] Surveyed prior art (Obsidian / OneNote / Apple Notes / Notion /
      Bear / Logseq). Universal pattern: bytes live on a transport
      every viewer can reach; no app stores URI-only references.
- [x] Considered URI-only-references shortcut → rejected as the
      sole strategy (breaks cross-device, breaks paid cloud-API
      tier, dies on permission revocation).
- [x] Considered Apple-Notes-style asset catalog → reasonable but
      heavier; loses the OneDrive sync-for-free win in
      `cloudStorage` mode.
- [x] **Picked tagged-union design**: Obsidian-style vault as
      universal fallback + `phasset` (iOS Photos) + `cloudFile`
      (OneDrive / iCloud Drive folder) smart references. Vault is
      the only kind that survives the Free → Paid migration —
      non-vault refs are resolved + uploaded at the boundary.
- [x] Wrote `docs/attachments-design.md` — full design doc mirroring
      the format of `docs/multi-device-cloud-sync.md`.
- [x] Wrote / rewrote `findings.md` and `task_plan.md` around the
      chosen design (3 iterations: asset-catalog → file-vault-only →
      tagged-union).

### In Progress
- [ ] **Design-doc gate**: awaiting user review of
      `docs/attachments-design.md` before starting Phase 3 (shared
      specs) and onward.

### Blocked
- All implementation work blocked behind the gate above.

### Errors
*(none yet)*

### Decisions snapshot
- Tagged-union references (`vault` / `phasset` / `cloudFile`) on
  top of an Obsidian-style file vault. Vault is universal; the
  other two save bytes when Apple / Microsoft already sync them.
- Paid `cloudApi` tier is vault-only by construction.
  Free → Paid migration resolves all non-vault refs to vault refs
  before upload.
- Reference shape: `primaryImage` + `images[]` siblings inside
  existing note JSON content. Per-note subfolders under
  `attachments/note-{noteId}/`. UUID filenames; original metadata
  stored alongside the reference.
- v1 picker emits `vault` only — smart kinds (`phasset`,
  `cloudFile`) light up in Phases 13–14 as performance/space
  optimisations layered on top.
- Allowed MIME on v1: jpeg / png / heic / webp; 8 MB cap; server
  downsizes originals beyond 4096px long edge.
- Subscription gating per `docs/multi-device-cloud-sync.md`:
  `Active` = read+write, `Grace` = read-only, `Lapsed` = export-only.
- iOS Info.plist gets the visibility flags so the vault appears in
  Files app.
- The Flutter Drift `Attachments` table + `IAttachmentRepository`
  are deprecated; removal after one release window.
