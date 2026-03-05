# Task Plan

## Objective
Add a reverse geocoding feature — a new API endpoint that returns address information (street, city, state, country, zip) based on latitude/longitude coordinates.

## Phases

### Phase 1: Research & Discovery
- [x] Explore `Hmm.ServiceApi` and `Hmm.Automobile` structure
- [x] Understand GasStation entity (already has Latitude/Longitude)
- [x] Identify existing patterns: Manager, NoteSerializer, Validator, ResultFilter
- [x] Review DI registration in `AutomobileInfoServiceStartup`

### Phase 2: Domain & Service Layer
- [x] Create `GeoAddress` result model in `Hmm.Automobile/DomainEntity/`
- [x] Define `IGeocodingService` interface in `Hmm.Automobile/`
- [x] Implement `NominatimGeocodingService` (Nominatim/OpenStreetMap provider)
- [x] Add `GeocodingSettings` configuration class
- [x] Add config section to `appsettings.json`

### Phase 3: API Layer
- [x] Create `ApiGeoAddress` DTO in `Hmm.ServiceApi.DtoEntity/GasLogNotes/`
- [x] Add AutoMapper mapping in `AutomobileMappingProfile`
- [x] Create `GeocodingController` with reverse geocode endpoint
- [x] Result filter not needed (simple single-object response, mapped in controller)
- [x] Register services in `AutomobileInfoServiceStartup`

### Phase 4: Testing & Verification
- [x] Unit tests for `NominatimGeocodingService` (13 tests — validation, success, error handling)
- [x] Controller tests for `GeocodingController` (5 tests — success, not found, bad request, validation, field mapping)
- [x] Build solution (`dotnet build Hmm.sln`) — 0 errors
- [x] Run all tests (`dotnet test Hmm.sln`) — 1,611 passed (22 new)

### Phase 5: Optional Enhancements
- [ ] Auto-fill address on GasStation create/update when lat/lng provided
- [ ] Add caching for geocoding results
- [ ] Batch geocoding endpoint

## Decisions Log
| Decision | Rationale | Date |
|----------|-----------|------|
| Use Nominatim (OpenStreetMap) | Free, no API key required, sufficient for single-user app | 2026-03-04 |
| Separate `GeocodingController` | Keeps geocoding concern separate from GasStation CRUD | 2026-03-04 |
| `GeoAddress` as read-only model | No persistence needed — it's a transient lookup result | 2026-03-04 |
| No new NuGet packages | `HttpClient` + `System.Text.Json` already available | 2026-03-04 |

## Status: PHASES 1-4 COMPLETE — PHASE 5 (OPTIONAL ENHANCEMENTS) DEFERRED
