# Apple-ecosystem photo-byte sync — cloudStorage tier (design note)

> Companion to [`attachments-design.md`](attachments-design.md) §"cloudStorage
> byte sync is desktop-only" and [`multi-device-cloud-sync.md`](multi-device-cloud-sync.md).
> Explores how an **iPhone + iPad (same Apple ID)** user on the free
> `cloudStorage` tier could get attachment **bytes** on both devices
> without us uploading them — and why it's parked, not built.

## Problem

On the free `cloudStorage` (OneDrive) tier, the note JSON + the
`vault` reference sync via the Graph AppFolder, but the image **bytes**
don't: on iOS the vault is sandboxed (`<app docs>/vault/`) with no
OS-mounted OneDrive folder to carry them. So a mobile-only user's
second device renders a placeholder. (Recap of the desktop-only
decision, 2026-05-30.)

But an iPhone + iPad on one Apple ID **already** have a working
cross-device byte channel — **iCloud**. The idea: lean on iCloud to
move the bytes instead of duplicating them through Hmm. This is the
reason the `phasset` / `cloudFile` reference kinds exist in the model.

## Two mechanisms

### A. `phasset` reference — lean on iCloud Photos  *(= parked Phase 16)*

Don't copy the photo into the vault; store a reference to the original
by its `PHAsset.localIdentifier`. iCloud Photos syncs the user's
library across their Apple devices, so the iPad resolves the **same**
identifier and reads the bytes from its own synced Photos library.

- **Notes** ride OneDrive (Graph); **bytes** ride iCloud Photos. Hmm
  uploads nothing.
- Picker emits a `PhAssetRef` (the kind is already in `attachment_ref.dart`).

### B. Vault in an iCloud Drive container — the iOS analog of "vault in the OneDrive folder"  *(≈ parked Phase 19 follow-up)*

Place the Hmm vault inside an iCloud Drive **ubiquity container**. iOS
then syncs those vault files across the user's Apple devices
automatically — the same shape as the desktop "vault sits inside the
OneDrive folder" story, but using iCloud Drive on iOS (where no
OneDrive folder mount exists).

- **Notes** ride OneDrive; **bytes** ride iCloud Drive.
- Vault stays a normal `vault`-kind copy — no smart-ref fragility, no
  Free→Paid resolution debt. Needs an iCloud container entitlement.

## The hard boundary

Both paths only work **within the same Apple iCloud account**
(iPhone ↔ iPad ↔ Mac). A Windows/Android second device can't reach
either iCloud channel.

| Scenario (cloudStorage) | Bytes on 2nd device? | Via |
| --- | --- | --- |
| iPhone → iPad (same iCloud) | ✅ *(if built)* | iCloud Photos (A) / iCloud Drive (B) |
| Desktop → any (vault in OneDrive folder) | ✅ today | OneDrive client |
| iPhone → Windows / Android | ❌ placeholder | needs **cloudApi** (paid) |

## Trade-offs

| | A. `phasset` (iCloud Photos) | B. iCloud Drive vault |
| --- | --- | --- |
| Bytes duplicated? | No (references the library) | Yes (a vault copy, iCloud-synced) |
| Survives "delete from Photos"? | **No** — image gone | Yes — independent copy |
| `localIdentifier` stability | Not formally guaranteed across devices (empirically OK per Apple ID) | n/a |
| "Optimize iPad Storage" | May need a network fetch for full-res | n/a (file is synced) |
| Free→Paid migration | Must resolve ref → vault bytes at the boundary (Phase 18); fails if source deleted | Already vault-shaped — trivial |
| Setup cost | PhotoKit + permission handling | iCloud container entitlement + plugin |
| Two-cloud split-brain | Notes(OneDrive) + bytes(iCloud) | Notes(OneDrive) + bytes(iCloud) |

## Recommendation

**Leave parked for now; copy + downsize (shipped) is the default.** If a
real "iPhone + iPad, won't pay, wants photos on both" user appears,
prefer **B (iCloud Drive vault)** over **A (`phasset`)**:

- B keeps the robustness guarantee (an independent copy that survives
  Photos deletion) and stays a plain `vault` ref, so it has **no
  Free→Paid resolution debt** — it's just "where the vault lives on
  iOS," parallel to the desktop OneDrive-folder mechanism.
- A is more elegant (zero duplication) but carries the fragility that
  made us reject linking as the default, plus the Phase-18 migration
  debt. Keep `phasset` for a future power-user opt-in, warned and
  gated, never the default.

Either way it's **Apple-only** and never a substitute for `cloudApi`,
which remains the answer for cross-ecosystem (Windows/Android) mobile
photo sync.

## Status

Not implemented. `phasset` = Phase 16 (parked); iCloud Drive vault ≈
Phase 19 follow-up (parked). Today: copy + downsize-on-copy
(2048px / JPEG q85), bytes desktop-only on OneDrive, full mobile sync
via cloudApi.

## Decisions log

| Decision | Rationale | Date |
| --- | --- | --- |
| Apple-ecosystem byte sync stays parked; if built, prefer iCloud Drive vault (B) over `phasset` (A) | B keeps the copy-robustness guarantee + no Free→Paid debt; A is zero-dup but fragile (delete-from-Photos, unstable id) + carries Phase-18 debt. Both are Apple-only — never replace cloudApi for cross-ecosystem. | 2026-05-30 |
