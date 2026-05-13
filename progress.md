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
- *(nothing actively in progress — see next session below)*

### Blocked
- All implementation work blocked behind the gate above.

### Errors
*(none yet)*

## Session: 2026-05-11 — design-doc review + gate cleared

### Completed
- [x] Walked the user through `docs/attachments-design.md` section
      by section. Raised 7 follow-ups (4 code-shaping, 3 later-phase);
      recorded in `task_plan.md` under "Design-doc follow-ups".
- [x] **Gate cleared.** User chose the Flutter local-mode vertical
      slice as the first coding work.
- [x] Decision: `primaryImage` / `images` are disjoint slots.
- [x] Applied the 4 code-shaping edits to the design doc
      (`Hmm.Core.Vault` project up front, disjoint slots, nullable
      `byteSize`, `VaultRef`-typed DTOs). Committed `8abdc00`.
- [x] **Architecture pivot (user-directed): attachments are a
      `HmmNote`-level facility, not per-domain.** Earlier the doc
      embedded refs inside `AutomobileInfo`'s JSON content; now they
      live on the note itself in a **new nullable `attachments` JSON
      column on the `Notes` table**. Chosen over a relational
      `NoteAttachment` child table — JSON column is cheaper (one EF
      migration, one Drift migration, no new repo/manager/controller)
      and we don't need attachments SQL-queryable yet.
      - Rewrote `docs/attachments-design.md`: "Reference shape" →
        column-on-`Notes`; new ".NET → HmmNote attachments" subsec;
        new "Flutter → Note storage" subsec; reworked the 19-step
        implementation order (added a `.NET Notes.attachments` step
        + a Flutter `Notes.attachments` step; re-noted the
        old-table removal).
      - Rewrote `task_plan.md` phases to match (now 20 phases; the
        local-mode slice is Phases 3 → 9 → 10 → 11 → 12 → 13 → 14).
      - `NoteAttachments.schema.json` (new) validates the column
        value on write; `byteSize` nullable there.

### In Progress
- [ ] Flutter local-mode slice — see `task_plan.md` "Active work".

### Blocked
- **Cannot start**: `~/Projects/hmm_console` is not in the working
  directories. User needs `/add-dir ~/Projects/hmm_console` (or to
  restart the session from that repo) before any Dart edits.

### Notes
- Flutter repo state checked: `hmm_console` on `main`, clean tree,
  HEAD `9fc1e86` "Local-mode records, editable info cards, dashboard
  intro".
- Hmm (.NET) `main` is 7 commits ahead of `origin/main`; unpushed.
- **Branch `feature/note-attachments` created off `main` in BOTH
  repos** (`Hmm` and `hmm_console`) — all attachments work lands
  there from now on. Hmm's branch carries the unpushed design +
  planning commits; hmm_console's branch is at `9fc1e86`.

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
