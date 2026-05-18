# Cross-Platform Cloud Sync — Architecture Research

Status: **Research / decision-support, pre-implementation**
Audience: anyone evaluating how to extend Hmm's note + attachment
sync beyond the current local + desktop-cloudStorage tiers.
Sibling docs:
- [`attachments-design.md`](./attachments-design.md) — the file-vault
  + tagged-union reference model that this research builds on.
- [`multi-device-cloud-sync.md`](./multi-device-cloud-sync.md) — the
  three-tier sync model (`local` / `cloudStorage` / `cloudApi`).

---

## 1. Context

After Phase 11.5 landed (2026-05-17), `cloudStorage` mode points the
vault root at a user-picked folder (typically inside their OneDrive
desktop folder) and lets the OS-level OneDrive client move bytes
across devices. This works on macOS / Windows / Linux desktops; it
**does not** give the same experience on mobile or across cloud
providers. Three architectural questions came up during testing
that warrant logging before they get re-litigated:

1. **Can `Hmm.ServiceAPI` sync local notes + attachments to the
   cloud today?** Where's the current implementation gap?
2. **Can iOS / Android devices sync directly to cloud storage
   providers (OneDrive, Drive, Dropbox, iCloud)?** Specifically:
   can Android sync to iCloud Drive?
3. **Is "API server proxies to cloud storage" (BYOS — Bring Your
   Own Storage)** a viable pattern that would give one API for all
   devices regardless of platform?

This document captures the findings so they don't need re-deriving.
**No decision is being committed here** — this is research material
to feed into the eventual cloudApi-tier work (Phases 4–8 + 15).

---

## 2. Q1 — Sync via `Hmm.ServiceAPI` today

**Short answer: no, not until the deferred phases land.** The API
has working CRUD for notes, automobiles, gas logs, and friends, but
the attachment + bulk-sync surface area required for full
device→server sync simply doesn't exist yet. The
`ApiSyncProvider` on the Flutter side is a stub — every method
throws `UnsupportedError('not yet implemented')`.

### 2.1 What works today

| Surface | State |
| --- | --- |
| `/v1/notes` CRUD (subject, content, catalog, …) | ✅ Working |
| `/v1/automobiles`, `/v1/gaslogs`, etc. | ✅ Working |
| JWT auth via `Hmm.Idp`, per-author data partition | ✅ Working |
| `Notes.attachments` JSON column on `Notes` (Flutter side) | ✅ v4 migration |
| Flutter `LocalVaultStore` + `AttachmentImage` widget | ✅ |

### 2.2 What's missing for cloudApi-tier sync

| Surface | Tracked as | Estimated effort |
| --- | --- | --- |
| `Hmm.Core.Vault` project (`IVaultBlobStore` + `FilesystemVaultBlobStore` + `VaultRef`) | Phase 4 | ~few days |
| `VaultController` — `POST/GET/HEAD/DELETE /v1/vault/{*relativePath}` + MIME/size guards + image downsize + subscription gating | Phase 5 | ~few days |
| `Notes.attachments` JSON column on `.NET Notes` table; AutoMapper; `ApiNote*` DTOs; result-filter pass-through | Phase 6 | ~couple days |
| Migration endpoints: `POST /v1/migration/upload`, `GET /v1/migration/export`, `POST /v1/migration/replace` | Phase 7 | ~couple days |
| `/var/lib/hmm-vault` Docker volume in `compose.api.yml`; `hmm-deploy.sh --backup` extended to tar the vault dir | Phase 8 | ~few hours |
| Flutter `ApiVaultStore` (Dio-backed `/v1/vault/{path}`) + real `ApiSyncProvider` + cloudApi tier of `vaultRootDirectoryProvider` | Phase 15 | ~few days |

Total: **2–3 weeks of focused work** to land the full cloudApi
tier end-to-end with the own-vault design.

### 2.3 Two sync patterns the design accommodates

Both work once the phases above ship:

1. **Live cloudApi sync** — every note save POSTs to `/v1/notes`;
   every photo pick POSTs to `/v1/vault/{path}`. Online-only for
   cloudApi-tier users; offline-first only in local / cloudStorage
   tiers.
2. **Free → Paid migration (bulk upload)** — user has been on
   local mode and wants to switch: `POST /v1/migration/upload`
   accepts the full note JSON dump + a streamed tar of vault files
   in one shot. Free→Paid resolution (Phases 17/18 client-side)
   rewrites any non-vault refs (PhAsset, CloudFile) to vault refs
   before upload, so the server only sees `vault` kinds.

3. **Lapsed export** — reverse of (2). User downloads everything
   as a tar. Works even when the subscription is in `Lapsed`
   state.

---

## 3. Q2 — Direct device-to-cloud feasibility per platform

**Short answer: only desktops can do "vault folder lives inside
OneDrive."** Mobile platforms don't expose mountable cloud-provider
folders; they require SDK / API integration in our process.

### 3.1 The fundamental gap

The Phase 11.5 design assumes a cloud provider's **OS-level sync
client mounts a real folder** that our app can `writeAsBytes` into.
That assumption holds on desktops; mobile platforms are sandboxed
and don't surface that folder to other apps.

| Platform | Mountable cloud folder writable from our app? |
| --- | --- |
| macOS desktop | ✅ OneDrive / Dropbox / Google Drive mount real folders. iCloud Drive too. |
| Windows | ✅ Same. |
| Linux | ✅ OneDrive (via `insync` / `onedriver`), Dropbox native, Google Drive via `rclone` or Insync — all mount folders. |
| **iOS** | ❌ Sandboxed. OneDrive / Google Drive / Dropbox iOS apps don't expose folders other apps can write to. Only iCloud Drive's *own-app ubiquity container* and one-shot Files-app document pickers are accessible. |
| **Android** | ❌ Same story. OneDrive / Drive / Dropbox Android apps store data in their own sandboxed areas. Apps reach them via SDK calls or the Storage Access Framework (user picks a tree URI per session). |

### 3.2 Provider × platform compatibility matrix

What's actually achievable for byte transfer, assuming we're
willing to write per-platform code:

| Provider | macOS | Win | Linux | iOS | Android |
| --- | --- | --- | --- | --- | --- |
| **OneDrive** | ✅ folder mount | ✅ folder mount | ✅ folder mount (3rd-party) | ⚠️ Microsoft Graph in-app | ⚠️ Microsoft Graph in-app |
| **Google Drive** | ✅ folder mount | ✅ folder mount | ⚠️ 3rd-party | ⚠️ Drive REST API in-app | ⚠️ Drive REST API in-app |
| **Dropbox** | ✅ folder mount | ✅ folder mount | ✅ folder mount | ⚠️ Dropbox SDK | ⚠️ Dropbox SDK |
| **iCloud Drive** | ✅ folder mount | ✅ iCloud for Windows | ❌ no client | ✅ own-app ubiquity container only | ❌ **not supported by Apple** |

"⚠️ via API" = we have to ship a provider against that cloud's API,
including its OAuth flow + per-platform handling. We already do
this for OneDrive note JSON (Microsoft Graph) — same pattern would
extend to bytes.

### 3.3 Can Android sync to iCloud Drive?

**No.** Apple does not publish an iCloud SDK for Android. There's
no public API for an Android process to read or write iCloud Drive
— by design. iCloud on Windows exists; on Android there's only
the web app at iCloud.com (manual, no programmatic access from
another Android app).

### 3.4 Cross-provider sync (OneDrive on Mac ↔ iCloud on iPhone)

**Not possible through cloud storage alone.** OneDrive and iCloud
don't talk to each other. Two ways to bridge them:

1. **Standardise on one provider** the user runs on every device
   they care about. E.g. OneDrive everywhere (iOS via Graph,
   macOS via folder mount).
2. **Use Hmm's own server as the hub** (the `cloudApi` tier from
   Phases 4–8 + 15). The only design-clean way to support iOS +
   Android together with multi-provider freedom.

### 3.5 Implication for cloudStorage's positioning

`cloudStorage` as designed is a **desktop-class feature**, not a
universal mobile one. Truthful product framing:

> "Multi-device sync on macOS / Windows / Linux via OneDrive
> folder. Mobile devices in cloudStorage mode store attachments
> locally (note JSON still syncs via the API path); for full
> cross-platform mobile sync, switch to cloudApi."

The Phase 11.5 work is correct for the desktop use case; it's not
broken for mobile, it just doesn't move bytes between devices
there.

---

## 4. Q3 — Server-proxied cloud storage (BYOS)

**Pattern:** Hmm's API server acts as a proxy between clients and
the user's chosen cloud storage provider. Clients only ever talk
to Hmm's REST API; the server holds the user's OAuth refresh token
and translates `/v1/vault/{path}` calls into Microsoft Graph /
Google Drive / Dropbox API calls under the hood.

```
┌──────────────┐                                  ┌─────────────────┐
│ iOS / Android│                                  │ User's OneDrive │
│ macOS / Win  │     One API surface              │ Google Drive    │
│ Linux        │ ──────────────────►              │ Dropbox         │
└──────┬───────┘                                  └────────▲────────┘
       │                                                   │
       │ /v1/notes, /v1/vault/{path}                       │
       ▼                                                   │
┌──────────────────────────────────────────────────────────┴────────┐
│                      Hmm.ServiceAPI                                │
│   ┌──────────────┐  ┌──────────────────────────────────────────┐  │
│   │  Notes DB    │  │ ICloudStorageProvider                    │  │
│   │  (Postgres)  │  │  ├─ OneDriveStorageProvider (Graph SDK) │  │
│   └──────────────┘  │  ├─ GoogleDriveStorageProvider (API)    │  │
│                     │  └─ DropboxStorageProvider (SDK)        │  │
│                     └──────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────┘
```

Used in production by: Standard Notes (their server proxies to S3 /
extended-storage backends), Joplin (server-mediated WebDAV /
Dropbox / OneDrive), various enterprise note tools.

### 4.1 Three variants

The "what does Hmm's server actually hold?" knob has three
positions:

| Variant | Server stores | Client reads from | Bandwidth cost on Hmm |
| --- | --- | --- | --- |
| **(a) Pure pass-through** | Just metadata (note rows + `cloudPath` per attachment). No bytes. | Server pipe-streams from cloud → client | High (every byte transits the server, both ways) |
| **(b) Hybrid (recommended)** | Notes + attachments < ~1 MB live in the server vault; large attachments push through to cloud | Mixed: small files served directly, large from cloud | Medium (only large files round-trip) |
| **(c) Server-cache** | Notes + cloud-cached attachments (LRU on disk; cloud is source of truth) | Cache hit served immediately; miss fetches and caches | Medium-low (warm cache pays once) |

(a) is the simplest but most bandwidth-hungry. (b) is the
production-grade middle ground. (c) is what content-delivery
products end up converging to.

### 4.2 Pros — the case for BYOS

1. **One API surface, every platform.** iOS, Android, web,
   desktop — all just call `/v1/notes` and `/v1/vault/{path}`. No
   per-platform SDK integration for each cloud provider.
2. **OAuth complexity stays server-side.** Only the server needs
   Microsoft App Registration / Google Cloud project / Dropbox App
   keys + the redirect/callback flow. Clients don't deal with
   platform-specific OAuth at all.
3. **User pays for storage; Hmm pays for compute.** User uses
   their already-free OneDrive / Drive / Dropbox quota (5–15 GB
   free typically). Storage cost on the API server stays small
   (metadata DB + small attachments + caches).
4. **Cross-provider freedom and migration.** User can swap
   OneDrive → Google Drive without changing client code or losing
   data — server can migrate by reading from old, writing to new.
5. **Encryption opportunity.** Encrypt server-side before pushing
   to cloud (per-user key in server DB) and the cloud provider
   literally cannot read user data. Big privacy win.
6. **Server-side aggregation / search.** Variants (b) and (c)
   keep metadata + caches on the server, so server-side queries
   ("photos with EXIF date in 2024") work without round-tripping
   every cloud read.
7. **Android-to-iCloud limitation disappears.** Server talks to
   whichever provider; client doesn't care. iCloud's
   Android-hostility doesn't matter because we don't have to
   support iCloud as a backing provider at all.

### 4.3 Cons — the case against

1. **Server bandwidth and runtime cost.** Variant (a) is roughly
   2× the bytes through Hmm's pipe (in from client, out to cloud,
   in from cloud on read, out to client). For a vehicle-log app
   this is small (KB per note, ~MB per photo, low frequency); for
   anything heavier it gets expensive fast.
2. **Latency vs. direct cloud-client sync.** Native OneDrive
   client does background incremental sync; server-proxy makes
   every fetch a synchronous round-trip. Cached variants (b)/(c)
   help.
3. **Server holds refresh tokens.** Security-sensitive: encrypt
   at rest with a per-user key, rotate, handle revocation, log
   access. Token theft → cloud-account compromise.
4. **Cloud provider rate limits.** Every user's traffic goes
   through Hmm's API quota with that cloud. Microsoft Graph
   throttles aggressively. Mitigation: per-tenant client IDs so
   each user's quota counts separately, plus retry/backoff.
5. **Streaming, not buffering.** Need to stream multi-MB files
   without buffering to memory (ASP.NET Core supports it; needs
   discipline).
6. **Operational complexity multiplies per provider.** Adding
   Google Drive ≠ "copy-paste the OneDrive code." Each provider
   has its own quirks: Google Drive uses opaque file IDs (no path
   concept); OneDrive supports path-by-id; Dropbox has its own
   delta-token semantics. The `ICloudStorageProvider` abstraction
   ends up lowest-common-denominator.
7. **Atomicity across two backends.** If the server's note row
   is upserted but the cloud-side blob upload fails, you have a
   partial state. Eventual consistency + reconciliation jobs is
   the realistic answer.
8. **Single point of failure.** Cloud provider down only blocks
   reads for users on that provider; Hmm server down blocks
   **everything**. Direct device→cloud sync at least survives Hmm
   server outages.

### 4.4 Estimated effort

| Increment | Effort |
| --- | --- |
| First provider (OneDrive, single tenant) | ~2–3 months |
| Each additional provider (Drive, Dropbox) | ~1 month each |
| Per-user provider linking + token storage + refresh job | ~couple weeks (counted in the first-provider estimate) |
| Encryption-at-rest before cloud push | ~weeks |
| Streaming, rate-limit + retry, caching layer | ~weeks |

Significantly larger than own-vault (Phases 4–8: ~2–3 weeks).

---

## 5. Comparison — own-vault vs. BYOS

Both deliver "one API for every device" (the cross-platform
requirement). Differences are in cost structure and engineering
investment.

| | Own-vault (Phases 4–8) | BYOS (server proxy) |
| --- | --- | --- |
| Cross-platform mobile sync | ✅ Solves it | ✅ Also solves it |
| Per-user storage cost | Hmm pays | User pays (often free quota) |
| Hmm bandwidth cost | 1× in + 1× out per byte | ~2× (proxy adds a hop) |
| Implementation effort | ~2–3 weeks for full set | ~2–3 months for first provider; +1 month per additional |
| Server-side complexity | Filesystem + REST | OAuth flows × N providers, token refresh, rate-limit handling, streaming, per-provider quirks |
| User signup friction | Hmm account only | Hmm account + OAuth-link cloud account |
| Privacy story | "We hold your data" | "We see it in flight; you control where it lives" |
| Failure modes | Hmm server down → no sync | Hmm server down → no sync; cloud down → no sync (worse) |
| Business model | Subscription funds VPS storage | Lighter VPS; harder to monetise per-user, but lower per-user cost |
| Search / aggregation server-side | Yes (data is on server) | Yes only with variants (b)/(c) and the cached portion |
| Encryption-at-rest in cloud | Built in (own disk) | Possible with per-user key in server (extra work) |

### 5.1 Cost reality check for the vehicle-log use case

A typical Hmm user is expected to have: 1–3 cars, maybe 5–20
photos per car over years, plus service-record / insurance / etc.
attachments. **Per-user footprint: ~50–500 MB.** At commodity VPS
prices that's pennies per user per year. The "user pays for their
own cloud" advantage of BYOS is real but small in absolute terms
at the current target scale.

BYOS becomes economically interesting if the feature set grows to
include: full-resolution videos, scanned service manuals, dashcam
clips, receipt OCRs at original resolution, etc. — anything that
pushes per-user footprint into the multi-GB range.

---

## 6. Recommendation

**Build Phase 4–8 (own-vault) first; layer BYOS on top later as a
plug-in to the `IVaultBlobStore` abstraction.**

Reasons:

1. **Phase 4–8 already solves the cross-platform mobile question.**
   One API endpoint set, all devices, no per-platform SDK work.
2. **Cost asymmetry** — 2–3 weeks vs. 2–3 months for the same
   user-visible outcome ("one API for all devices").
3. **Foundation, not lock-in.** The `IVaultBlobStore` interface
   landed in Phase 4 *is* the BYOS extension point. Swapping
   `FilesystemVaultBlobStore` for `OneDriveCloudStorageProvider`
   per-user is a Phase ~20 follow-on, not a rewrite.
4. **Storage cost at this scale is small.** The pinching case for
   BYOS (multi-GB per user) isn't here yet for vehicle-log
   workloads.
5. **BYOS shines later** as a privacy / cost-shifting feature, not
   as a multi-platform-sync enabler (Phase 4–8 already covers
   that).

### 6.1 Suggested order of operations

1. Finish local + cloudStorage testing (current active scope).
2. Phase 4–8 + 15 — own-vault cloudApi tier. Validates the
   "one API" architecture, ships cross-platform mobile sync.
3. (Optional, later) **Phase ~20 — BYOS as `IVaultBlobStore`
   implementation.** Start with OneDrive (lowest marginal cost
   since Microsoft Graph wiring already exists Flutter-side).
   Behind a per-user opt-in toggle. Migration utility to move
   existing users' bytes from own-vault to their linked cloud.

---

## 7. Open questions (for when BYOS work begins)

1. **Token storage encryption.** Per-user KEK derived from
   passphrase, or app-wide KEK in env? Trade-off: server-only key
   means Hmm staff could theoretically decrypt; user-derived
   means cloud relink on password reset.
2. **Multi-provider per user.** Single linked provider, or allow
   one user to have OneDrive + Dropbox simultaneously (different
   note types in different places)? v1 simplest = single provider
   per user.
3. **Mid-upload server crash.** Saga / outbox / reconciliation
   strategy for partial uploads (server says "ok" before cloud
   confirms; cloud confirms after server crash; etc.).
4. **OAuth scope per provider.** Minimal scopes only (no contacts,
   no calendar). Document explicitly per provider.
5. **Quota awareness.** Server detects "user's OneDrive is full"
   from API responses and surfaces actionable error to client.
6. **Encryption-at-rest opt-in vs. mandatory.** If opt-in, search
   degrades for encrypted accounts. Mandatory simplifies the
   model but loses search.
7. **Web client story.** A future Hmm web app (if any) on the
   own-vault model "just works" via existing API; on BYOS, it
   inherits whichever provider the user linked — same code.
8. **Backup story for BYOS users.** Their data lives in their
   cloud — Hmm doesn't back it up. Document clearly: "your cloud
   provider is your backup."

---

## 8. Decision log

| Decision | Status | Date |
| --- | --- | --- |
| `cloudStorage` is a desktop-class feature; mobile in cloudStorage mode does not get cross-device byte sync | Documented in this research; not a new product decision — surfaces a known limit | 2026-05-17 |
| `cloudApi` (own-vault) is the only design-clean answer for cross-platform mobile multi-device sync | Confirmed | 2026-05-17 |
| Build own-vault (Phases 4–8 + 15) before BYOS | Recommended, not yet committed | 2026-05-17 |
| BYOS is a Phase ~20 follow-on, not a Phase 4–8 alternative | Recommended | 2026-05-17 |
| Android → iCloud Drive is impossible (Apple-side, not implementation) | Confirmed | 2026-05-17 |
| If BYOS lands, OneDrive is the first provider (lowest marginal cost — Microsoft Graph wiring already exists) | Recommended | 2026-05-17 |
