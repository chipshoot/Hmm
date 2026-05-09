# Cloud (API) Tier — Sync, Migration & Subscription Lifecycle

Owner: Flutter `hmm_console` + .NET `Hmm.ServiceApi`
Status: **Draft / pre-implementation**
Audience: anyone touching data-mode switching, billing, or the API's
per-user state.

## Why

The Flutter app exposes three data modes (see `core/data/data_mode.dart`):
`local`, `cloudStorage` (local Drift + OneDrive sync), `cloudApi`. With
insurance / service / scheduled records now landing locally too, every
mode switch risks silently losing data — there's no story for "I've
been editing on Local, now I want it in the Cloud" or "two paid
devices both edited the same record".

This document defines:
- The **tier model** (free vs paid).
- The **live sync rule** (one-way: local/cloud → API).
- How users move **into** the paid tier (one-shot upload migration).
- How users move **out** of the paid tier — voluntarily and when
  billing lapses — without losing data.

## Model

### Tier rules

| Tier         | Storage modes available                   |
| ------------ | ----------------------------------------- |
| Free         | `local`, `cloudStorage` (OneDrive)        |
| Paid (Pro)   | All three, including `cloudApi`           |

`cloudApi` is **paid-only** and is **canonical** for that user. While
on `cloudApi`, the .NET `Hmm.ServiceApi` is the single source of
truth; all paid devices read and write the API directly. The local
Drift store may be used as a read cache but is never authoritative.

### Live sync direction is one-way

```
   local / cloudStorage  ────one-shot copy───►  cloudApi
                                                  │
                                                  │ (live read/write,
                                                  │  multi-device)
                                                  ▼
                                          all paid devices
```

There is no live API → local sync. Users can:

- Upload (free → paid): one-shot bulk migration with confirmation.
- Export (paid → local snapshot): one-shot snapshot pull, no live
  sync afterwards. This is the dignified exit path; see "Migration
  scenarios" below.

### Multi-device under the paid tier

Because the API is canonical, multi-device is trivially correct:
every paid device just hits the API directly. The existing
`VersionedEntity` optimistic-concurrency pattern (the `Version`
byte[]) handles concurrent edits — last writer wins per record, with
HTTP 409 surfaced to the loser so the client can refetch and retry.
**No master/follower election needed.**

We still keep a lightweight `Device` registry (see Schema) so users
can see "this account is active on iPhone, iPad, Mac" in Settings and
remotely sign out a lost device. It carries no sync responsibility.

## Subscription states

Authoritative status lives on the .NET side, cached from the billing
provider's webhooks (Stripe / Apple / Google).

| State        | Meaning                                                       | API access                                                                |
| ------------ | ------------------------------------------------------------- | ------------------------------------------------------------------------- |
| `Active`     | Billing current.                                              | Full read + write.                                                        |
| `Grace`      | Renewal failed; inside grace window (default 30 days).        | Read + write **for renewal-related ops + export only**; mutating record endpoints return `402 Payment Required`. |
| `Lapsed`     | Grace expired; data still on server.                          | Read-only for **export only**, scheduled delete in N days (default 30).    |
| `Cancelled`  | User actively cancelled.                                      | Same as `Lapsed`.                                                         |
| `Deleted`    | Grace + cancellation window passed; data removed.             | No data, no API access.                                                   |

Total time from a missed payment to data deletion = grace window +
post-lapse export window (default 30 + 30 = 60 days). Configurable
server-side per environment.

State transitions:
```
        ┌──────────┐  webhook: payment ok      ┌──────────┐
        │  Active  │ ◄───────────────────────► │  Grace   │
        └────┬─────┘                            └────┬─────┘
             │ user cancels                          │ grace expires
             ▼                                       ▼
        ┌──────────┐  user re-subscribes      ┌──────────┐
        │Cancelled │ ◄─────────────────────── │  Lapsed  │
        └────┬─────┘                            └────┬─────┘
             │ retention window expires             │
             └──────────────────┬───────────────────┘
                                ▼
                          ┌──────────┐
                          │ Deleted  │
                          └──────────┘
```

A re-subscribe from `Grace`, `Lapsed`, or `Cancelled` (before
`Deleted`) restores `Active` with the data intact.

## Migration scenarios

There are three distinct flows. Each one is consent-gated; nothing
copies or deletes silently.

### 1. Upgrade — Free → Paid (`local|cloudStorage` → `cloudApi`)

Trigger: user toggles mode in Settings.

Flow:
1. **Subscription check.** If not `Active`, show paywall, return.
2. **Conflict check.** Hit `GET /v1/subscription/cloud-state`. Three
   outcomes:
   - **Cloud is empty** → straight to upload.
   - **Cloud has data from a previous session on this account** → show
     "Cloud already has X automobiles, Y gas logs from before. Upload
     new local data and **replace** cloud, or **discard** local and
     pull cloud down?" Replace = full delete on server then upload;
     discard = clear local, switch to API.
   - **Cloud has data from another device that's currently in API
     mode** → "Your account is already active on iPhone. Switch this
     device to follow it?" (No data uploaded; this device just joins
     the live API.)
3. **Upload with progress.** Block UI, show count + cancel.
4. On success: switch to `cloudApi`, write a `MigrationLog` entry
   server-side (when, from-mode, record counts).
5. Local Drift is **not** wiped — kept as a fallback cache. The
   client will preferentially read from API but tolerate API outage by
   reading the last known cache.

### 2. Voluntary downgrade — Paid → Local while subscribed

Trigger: user switches mode `cloudApi` → `local` (or `cloudStorage`)
while subscription is `Active`.

Flow:
1. **Confirm dialog.** "Switching to local mode will copy your cloud
   data here as a snapshot. Your cloud data stays on our server while
   your subscription is active — switch back any time." Buttons:
   `Cancel` / `Copy and switch`.
2. **One-shot snapshot pull.** Replace local Drift with current API
   contents inside a transaction. (Same machinery as #3 below.)
3. Switch mode to `local`. Cloud data is **untouched**.
4. **On later re-entry to `cloudApi`** (back to scenario #1), the
   "Cloud already has data" branch fires and the user picks: push
   local edits up (replacing cloud) or discard local + pull cloud
   back down.

### 3. Involuntary fallback — subscription lapsed

Trigger: billing provider webhook moves status to `Grace` or `Lapsed`.

Server behaviour:
- `Grace`: mutating record endpoints (POST/PUT/PATCH/DELETE on notes,
  policies, etc.) return `402 Payment Required` with a body
  describing the grace deadline. Read endpoints + export endpoint +
  subscription endpoints still work.
- `Lapsed`: same, but reads also restricted to the export endpoint
  only. A scheduled job deletes data after the post-lapse retention
  window.

Client behaviour:
- A persistent banner at the top of every screen: "Subscription
  lapsed — Renew or export your data before {date}." Tappable into
  the renewal / export sheet.
- The records screens show a softer read-only state with the same
  banner and a prominent "Export to local & switch off cloud" button.
- Pressing **Export**: pulls full snapshot to Drift (one-shot,
  identical machinery to #2), switches mode to `local`. Subscription
  status stays `Lapsed` — user has dignified offboarding without
  having to confirm cancellation in two places.
- Pressing **Renew**: opens billing flow; on success, banner
  disappears.

## Edit policy per (mode × subscription)

| Mode + Sub                           | Edit allowed?                              |
| ------------------------------------ | ------------------------------------------ |
| `local` (any sub)                    | Yes — local Drift, no sync                 |
| `cloudStorage` (any sub)             | Yes — local Drift + OneDrive               |
| `cloudApi` + `Active`                | Yes — writes to API, multi-device live     |
| `cloudApi` + `Grace`                 | **Read-only** + banner; export allowed     |
| `cloudApi` + `Lapsed` / `Cancelled`  | **Read-only** + banner; export only        |

The records screens already have a clean "read-only" code path from
the earlier discussion (followers in the master/follower model — now
repurposed for the lapsed-subscription state). Pencils + FABs hidden;
banner explains why and where to act.

## Schema (cloud / .NET API)

Lives alongside `Author` in `Hmm.Core.Map`.

### `Subscription`

One row per `Author`. Cached from billing-provider webhooks; the
provider remains authoritative for billing-time questions but the API
treats this row as the truth for access decisions.

| Column                | Type           | Notes                                                                  |
| --------------------- | -------------- | ---------------------------------------------------------------------- |
| `AuthorId`            | int            | PK, FK → `Author.Id`                                                   |
| `Status`              | enum           | `None` (never subscribed), `Active`, `Grace`, `Lapsed`, `Cancelled`, `Deleted` |
| `Plan`                | string(40)     | e.g. `"hmm-pro-monthly"`                                               |
| `Provider`            | enum           | `Stripe`, `AppleAppStore`, `GooglePlay`, `Manual`                      |
| `ExternalId`          | string(80)     | provider's subscription id                                             |
| `CurrentPeriodEnd`    | UTC?           | next renewal attempt                                                   |
| `GraceUntil`          | UTC?           | when `Grace` flips to `Lapsed`                                         |
| `RetentionUntil`      | UTC?           | when `Lapsed` data is deleted                                          |
| `UpdatedAt`           | UTC            | last webhook applied                                                   |
| `Version`             | byte[]         | optimistic concurrency                                                 |

### `Device`

Lightweight registry — no master responsibilities, just visibility.

| Column          | Type       | Notes                                                        |
| --------------- | ---------- | ------------------------------------------------------------ |
| `Id`            | int        | PK                                                           |
| `DeviceUuid`    | string(36) | client-generated UUID, persisted on the device, unique idx   |
| `AuthorId`      | int        | FK → `Author.Id`                                             |
| `Name`          | string(80) | user-editable; defaults to `"{Platform} {Model}"`            |
| `Platform`      | enum       | iOS / Android / Web / Desktop                                |
| `AppVersion`    | string(40) | semver                                                       |
| `FirstSeenAt`   | UTC        |                                                              |
| `LastSeenAt`    | UTC        | touched on every API round-trip                              |
| `State`         | enum       | `Active` \| `Removed` (soft-delete)                          |
| `Version`       | byte[]     |                                                              |

Composite uniqueness on `(AuthorId, DeviceUuid)`.

### `MigrationLog`

Append-only audit. Tells support what happened when "I lost my data!"
tickets arrive.

| Column          | Type     | Notes                                                                    |
| --------------- | -------- | ------------------------------------------------------------------------ |
| `Id`            | int      | PK                                                                       |
| `AuthorId`      | int      | FK                                                                       |
| `DeviceId`      | int      | FK — which device initiated                                              |
| `Kind`          | enum     | `UploadFromLocal`, `ExportToLocal`, `CloudReplaced`, `LapsedDelete`      |
| `RecordCounts`  | string   | JSON: `{"automobiles":3,"gasLogs":42,"insurancePolicies":1,...}`          |
| `At`            | UTC      |                                                                          |

## API surface (`Hmm.ServiceApi`)

All under `/v1`. Auth: bearer JWT. Subscription state is enforced by
a new `RequireActiveSubscriptionAttribute` on every record-mutating
controller — returns `402 Payment Required` with the `Subscription`
status in the body.

### Subscription
| Verb     | Path                              | Purpose                                                   |
| -------- | --------------------------------- | --------------------------------------------------------- |
| `GET`    | `/v1/subscription`                | Current status, deadlines, plan                            |
| `POST`   | `/v1/subscription/checkout`       | Returns provider checkout URL / payment sheet token       |
| `POST`   | `/v1/subscription/cancel`         | User-initiated cancel (immediate or end-of-period)         |
| `POST`   | `/v1/billing/webhook/{provider}`  | Provider webhook receiver (no JWT, signature-verified)    |

### Cloud state — used during Upgrade flow
| Verb  | Path                              | Purpose                                                    |
| ----- | --------------------------------- | ---------------------------------------------------------- |
| `GET` | `/v1/subscription/cloud-state`    | Summary: is cloud empty, when was last write, by which device — drives the upgrade conflict prompt |

### Migration
| Verb     | Path                              | Purpose                                                    |
| -------- | --------------------------------- | ---------------------------------------------------------- |
| `POST`   | `/v1/migration/upload`            | Bulk upload — accepts a single envelope of records, returns counts and any per-record errors |
| `GET`    | `/v1/migration/export`            | Bulk export — returns the user's full record set as a single envelope (works in `Active`, `Grace`, `Lapsed`, `Cancelled`) |
| `POST`   | `/v1/migration/replace`           | Server-side wipe-then-upload, used by the "Replace cloud" branch of the upgrade flow |
| `GET`    | `/v1/migration/log`               | Last N entries from `MigrationLog`                         |

### Devices
| Verb     | Path                              | Purpose                                                    |
| -------- | --------------------------------- | ---------------------------------------------------------- |
| `GET`    | `/v1/devices`                     | List active devices                                        |
| `POST`   | `/v1/devices/register`            | Register / refresh (idempotent on `DeviceUuid`)            |
| `PUT`    | `/v1/devices/{id}`                | Rename                                                     |
| `DELETE` | `/v1/devices/{id}`                | Soft-delete — that device is signed out next round-trip    |

## State machine (per device)

Combines data mode with subscription state. Five effective states:

```
                       free user                 subscribed user
       ┌─────────┐                          ┌─────────────┐
       │  Local  │ ◄────── export ──────────│ApiReadOnly  │
       └────┬────┘                          │ (Grace /    │
            │                               │  Lapsed)    │
            │  user toggles mode → cloudApi │             │
            │  + has Active subscription    └──────┬──────┘
            │                                      │
            │  ┌───────────────────────────────────┘
            │  │   webhook: subscription
            ▼  ▼   transitions Active → Grace
       ┌──────────────┐
       │ ApiReadWrite │
       │   (Active)   │
       └──────┬───────┘
              │  user toggles mode → local
              │  (snapshot pull on the way out)
              ▼
        back to Local
```

`cloudStorage` slots in next to `Local` — same edit policy, just
adds OneDrive sync. The `cloudStorage` ↔ `Local` toggle has no API
involvement; it's a free-tier setting.

Edges that need explicit consent dialogs:
- Free → ApiReadWrite when cloud has prior data.
- ApiReadWrite → Local while subscribed.
- ApiReadOnly → Local (export-and-switch button).
- Subscription cancel from inside the app.

## Sync flow (paid tier)

The paid tier is **not** a sync engine. There is no diff/merge layer,
no master pointer, no offline queue. Reads and writes go directly to
the API; the existing `VersionedEntity` concurrency control handles
race conditions per record.

Concretely, `cloudApi` mode in the Flutter client:
- Hits `/v1/automobiles/...` etc. directly via Dio (already true today).
- Reads optionally cached in Drift for offline display only — writes
  in the offline state are blocked and surface a "you're offline,
  changes can't be saved" snackbar.
- On `409` from the API: refetch the row, show "this record was
  changed elsewhere — overwrite or keep theirs?" dialog.

## Settings UX (Flutter)

Two new sections under Settings:

```
┌─ Subscription ────────────────────────────┐
│  Hmm Pro — Active                         │
│  Renews Mar 15, 2027                      │
│  [Manage] [Cancel]                        │
└───────────────────────────────────────────┘

┌─ Cloud devices ───────────────────────────┐
│  iPhone 16 Pro             this device    │
│  Last seen: 5 min ago                     │
│  [Rename]                                 │
│                                           │
│  iPad Pro                                 │
│  Last seen: 2 hours ago                   │
│  [Rename] [Remove]                        │
│                                           │
│  Mac mini                                 │
│  Last seen: 4 days ago    (inactive)      │
│  [Rename] [Remove]                        │
└───────────────────────────────────────────┘
```

The existing `Storage Mode` setting gains a paywall + consent dialog
when the user picks `Cloud (API)`.

## Implementation order

Each step is small enough to ship independently.

1. **API: `Subscription` table + `RequireActiveSubscriptionAttribute`.**
   Stub the billing provider — manual `POST /v1/subscription` for
   testing. Default everyone to `Active` until the gate ships in #2.
2. **API: gate the existing record-mutation endpoints behind the
   attribute.** Returns `402` for non-`Active` users.
3. **API: `Device` table + register/list endpoints.** No sync
   responsibilities yet, just visibility.
4. **API: `/v1/migration/{upload,export,replace}` + `MigrationLog`.**
5. **Client: paywall + Subscription Settings section.**
6. **Client: upgrade flow (Free → Paid migration).** Conflict
   detection, progress UI, consent dialogs.
7. **Client: voluntary downgrade flow.** Snapshot pull on switch.
8. **Client: read-only banner + export-and-switch button for
   `Grace` / `Lapsed`.**
9. **Billing provider integration.** Stripe webhook handler, real
   subscription lifecycle.
10. **Multi-device "Cloud devices" Settings section.**
11. **Server-side retention job: delete `Lapsed` users past
    `RetentionUntil`.**

## Out of scope (deliberately)

- Real-time push between paid devices (no WebSocket / SSE — pull on
  open is enough for personal data).
- Selective sync (e.g. sync only some automobiles).
- Cross-account sharing / family plans.
- Encryption at rest beyond what the device OS / cloud provider
  already provides.
- Rich offline editing in `cloudApi` mode (writes blocked when
  offline; users wanting offline editing use `local` or
  `cloudStorage`).

These can be added later without breaking the tier model.
