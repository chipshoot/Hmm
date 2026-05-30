# User Profile & Settings Sync — cloudApi tier (addendum)

> **Addendum to** [`multi-device-cloud-sync.md`](multi-device-cloud-sync.md).
> Closes the one settings gap in the paid tier so a second device that
> signs in for the first time pulls the user's preferences down with
> no manual setup. Reuses the existing `SyncableSettings` LWW bundle
> and the orchestrator's settings step — only the **cloudApi transport**
> (one server entity + one endpoint + two Dart methods) is new.

## Why

The paid `cloudApi` tier is **canonical** — every paid device reads
and writes `Hmm.ServiceApi` directly, so multi-device for *notes* is
already trivially correct. But **settings** never got a server-side
home:

- `hmm_console` already has the full settings-sync machinery:
  `SyncableSettings` (whole-bundle last-writer-wins, stamped with
  `lastModified`), `CloudSyncProvider.pullSettings` / `pushSettings`,
  and the orchestrator's "step 0b" that runs the LWW compare every
  sync.
- The **OneDrive (`cloudStorage`) tier** implements it via a
  `settings.json` file blob (`OneDriveGraphClient.getSettings` /
  `putSettings`).
- The **cloudApi tier does not**: `ApiSyncProvider.pullSettings`
  returns `null` and `pushSettings` is a documented no-op, because
  "the API has no `/v1/settings` endpoint." So a paid user who flips
  their default fuel unit on the phone never sees it on the laptop —
  even though their *notes* sync perfectly.

This is the concrete gap behind the **D.2.5 cross-device unit-flip**
smoke gate, which cannot pass on the cloudApi tier until this lands.

## Scope decision (respects the tier philosophy)

The parent doc's promise stands:

| Tier | Settings home | Changed here? |
| --- | --- | --- |
| `local` | device only (SharedPreferences) | no |
| `cloudStorage` | the user's own OneDrive `settings.json` | no |
| `cloudApi` | **the API (this addendum)** | **yes — new** |

We are **not** centralising free-tier config on the backend. Free
users still own their bytes; only the paid tier — where the API is
already the source of truth — gains a server-side settings store.

**The IdP is untouched.** App preferences (default units, network
policy, locale) are app domain, not identity. They go in
`Hmm.ServiceApi`, never in `Hmm.Idp`. (Identity profile — display
name, email — stays in the IdP as today.)

## What syncs

Exactly the existing `SyncableSettings` bundle — no new fields:

```jsonc
{
  "gasLog":   { /* units, currency, showRegistration toggle */ },
  "syncSettings": { "networkPolicy": "wifiOnly" },
  "localeCode": "en",            // optional; null = follow system
  "lastModified": "2026-05-29T18:04:11.000Z",  // UTC, the LWW stamp
  "_v": 1                        // bundle schema version (client-owned)
}
```

Deliberately **out** (per `syncable_settings.dart`): `DataMode`,
`CloudProvider`, vault path, db path, OneDrive tokens, sync cursor /
device id — all device-local or operational state.

## Server is an opaque LWW store

The server **does not parse the preferences**. It stores the bundle
JSON verbatim and hands it back on read. The *only* field it reads is
the top-level envelope `lastModified`, used for the monotonicity guard
below. This keeps the contract stable: the client can add a new
preference (bumping `_v`) with **zero server migrations**.

## Schema (.NET)

New `AuthorSettings` — one row per `Author`, mirroring how
`Subscription` is modelled (separate table, not columns bolted onto
the hot `Author` row that every note op reads). Lives in
`Hmm.Core.Map` alongside `Author`.

| Column | Type | Notes |
| --- | --- | --- |
| `AuthorId` | int | **PK, FK → `Author.Id`** (one-to-one) |
| `SettingsJson` | text | the full bundle, opaque to the server (`NVARCHAR(MAX)` / `text` / `TEXT`) |
| `LastModified` | UTC | parsed from the bundle envelope on write; the LWW stamp + monotonicity guard |
| `UpdatedAt` | UTC | server clock, last successful write (audit) |
| `Version` | byte[] | optimistic concurrency (existing `VersionedEntity` pattern) |

Three-provider EF migration (SQL Server / PostgreSQL / SQLite), same
as every other table.

## API surface

Under `/v1`, bearer JWT, scoped to the authenticated author. Two
verbs:

| Verb | Path | Purpose |
| --- | --- | --- |
| `GET` | `/v1/profile/settings` | Return the caller's stored bundle, or `204 No Content` if none yet |
| `PUT` | `/v1/profile/settings` | Upsert the caller's bundle |

Notes:

- **`GET` → `204`** when the row is absent maps cleanly to the Dart
  contract: `pullSettings()` returns `null` ("cloud has nothing yet,
  seed from local"). A `200` returns the bundle as-is.
- **`PUT` monotonicity guard**: the server reads the incoming
  `lastModified`. If it is **<=** the stored `LastModified`, the write
  is a no-op returning `200` with the *stored* (newer/equal) bundle —
  so a racing stale device can't clobber a fresher one. A strictly
  newer stamp overwrites. (The client orchestrator already only pushes
  when local is newer, so this is belt-and-suspenders for the
  two-devices-race case.)
- **Subscription gate**: `PUT` carries
  `RequireActiveSubscriptionAttribute` like every other
  record-mutating endpoint (settings are paid-tier data). `GET` stays
  readable in `Grace` / `Lapsed` so an exporting user can still read
  their preferences. (Until the subscription gate from the parent doc
  ships, this is a no-op — everyone is `Active`.)
- Self-scoped only — no `{authorId}` in the path; the author comes
  from the token. No cross-user reads.

### DTO

`ApiAuthorSettings { string SettingsJson; DateTimeOffset LastModified; }`
on the wire is the minimal shape, but since the client already speaks
the bundle JSON, the endpoint simply accepts/returns the **raw bundle
object** as the body (the server stores `SettingsJson` = the serialized
body, and lifts `lastModified` out of it). No second DTO schema to
keep in lockstep with the Dart `SyncableSettings.toJson`.

## Flutter wiring (replaces two stubs)

`ApiSyncProvider` (`lib/core/data/sync/api_sync_provider.dart`) —
swap the no-ops for real calls; **nothing else changes**, the
orchestrator's step 0b already drives them:

```dart
@override
Future<Map<String, dynamic>?> pullSettings() async {
  final res = await _dio.get('/v1/profile/settings');
  if (res.statusCode == 204) return null;        // cloud empty → seed
  return res.data as Map<String, dynamic>;
}

@override
Future<void> pushSettings(Map<String, dynamic> body) async {
  await _dio.put('/v1/profile/settings', data: body);
}
```

The orchestrator's existing LWW logic (`_syncSettings` /
`_pushSettings`) needs **no change** — it already treats a `null`
pull as "seed the cloud," applies a strictly-newer remote bundle via
`_settingsRepo.apply` + the `SettingsBus` tick, and pushes when local
is newer.

## Concurrency & edge cases

- **First sign-in on device 2**: `GET` → `204` → orchestrator seeds
  from local if local has been touched, otherwise leaves both at
  epoch zero (no all-defaults blob uploaded). Once device 1 has ever
  pushed, device 2's first `GET` returns the bundle and applies it.
- **Two devices edit then sync**: both `PUT`; the monotonicity guard
  + whole-bundle LWW means the later `lastModified` wins all fields
  (the documented, accepted v1 behaviour — settings change rarely).
- **Schema bump (`_v` 1 → 2)**: server is opaque, stores whatever it
  gets; old clients reading a v2 bundle fall back per-field exactly
  as `SyncableSettings.fromJson` already does for unknown values.
- **Downgrade paid → local/export**: `GET` still works in
  `Grace`/`Lapsed`, so the exiting user's preferences come along in
  the export snapshot.

## Implementation order

Each step ships independently; mirrors the parent doc's style.

1. **.NET — `AuthorSettings` entity + DAO + EF migration** (3
   providers) + AutoMapper + a thin repository (or fold into
   `IAuthorManager`). Tests: round-trip, absent-row read.
2. **.NET — `ProfileSettingsController`** with `GET` (`200`/`204`)
   and `PUT` (upsert + monotonicity guard) + the subscription gate on
   `PUT`. Tests: seed, overwrite-newer, reject-stale, auth scoping,
   `204`-when-absent.
3. **Flutter — wire `ApiSyncProvider.pullSettings` / `pushSettings`**
   to the endpoint. Tests: `204`→null, `200`→map, push PUTs the
   body (http_mock_adapter, same pattern as the rest of
   `api_sync_provider_test.dart`).
4. **Verification** — satisfies the **D.2.5** smoke gate: flip default
   unit on sim A (cloudApi) → sync → second cloudApi client pulls the
   flip. `dotnet test` + `flutter test` green.

## Out of scope (deliberately)

- Per-field settings merge — whole-bundle LWW is the accepted v1
  behaviour (see `syncable_settings.dart`).
- Centralising free-tier (`local` / `cloudStorage`) settings on the
  backend — explicitly **not** done; preserves the data-ownership
  promise.
- Moving identity profile (name/email/avatar) off the IdP — unrelated;
  stays where it is.
- Server-side validation of preference *values* — the server is an
  opaque store; the client owns the schema.

## Decisions log

| Decision | Rationale | Date |
| --- | --- | --- |
| Settings server-side **only** in the cloudApi tier | The API is already canonical for paid users; free tiers keep their data-ownership promise (OneDrive file / device-local) | 2026-05-29 |
| App preferences go in `Hmm.ServiceApi`, **never** `Hmm.Idp` | IdP is identity/tokens; coupling app config to it means an auth-server migration per setting + stale-claim reads | 2026-05-29 |
| Dedicated `AuthorSettings` table, not columns on `Author` | Mirrors the `Subscription` one-row-per-author pattern; keeps the big opaque blob off the hot `Author` row read on every note op | 2026-05-29 |
| Server stores the bundle **opaque**, reads only envelope `lastModified` | Client can add preferences (`_v` bump) with zero server migrations; server stays a dumb LWW store | 2026-05-29 |
| Endpoint accepts/returns the **raw bundle**, no parallel DTO schema | The Dart `SyncableSettings.toJson` is the single source of the shape; avoids two schemas drifting | 2026-05-29 |
| `GET` → `204` when absent | Maps directly to the existing Dart `pullSettings() == null` "seed from local" contract; no client logic change | 2026-05-29 |
| Reuse the existing `SyncableSettings` LWW bundle + orchestrator step 0b unchanged | The whole client pipeline already exists and is tested; only the transport is missing | 2026-05-29 |
