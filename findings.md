# Findings

## Research Notes

### GasStation Entity (already has geo fields)
- **Domain**: `src/Hmm.Automobile/DomainEntity/GasStation.cs`
- Has `double? Latitude` and `double? Longitude` (nullable)
- Validation: latitude [-90, 90], longitude [-180, 180] in `GasStationValidator`
- Stored as JSON in note content via `GasStationJsonNoteSerialize`

### Existing Patterns to Follow
- **Manager pattern**: All business logic in `EntityManagerBase<T>` subclasses, return `ProcessingResult<T>`
- **Result filters**: `[TypeFilter(typeof(XxxResultFilter))]` for automatic domain→DTO mapping
- **DI registration**: All in `AutomobileInfoServiceStartup.cs`
- **AutoMapper**: Profiles in `AutomobileMappingProfile.cs`
- **Controller routing**: `[Route("/api/v{version:apiVersion}/automobiles/...")]`
- **Authorization**: `[Authorize]` on all controllers

### Nominatim Reverse Geocoding API
- Endpoint: `https://nominatim.openstreetmap.org/reverse?lat={lat}&lon={lon}&format=json`
- Rate limit: 1 request/second (acceptable for single-user app)
- Returns: `display_name`, `address.road`, `address.city`, `address.state`, `address.country`, `address.postcode`
- Requires `User-Agent` header per usage policy

## Key Files

| Component | Path |
|-----------|------|
| GasStation domain entity | `src/Hmm.Automobile/DomainEntity/GasStation.cs` |
| GasStation manager | `src/Hmm.Automobile/GasStationManager.cs` |
| GasStation validator | `src/Hmm.Automobile/Validator/GasStationValidator.cs` |
| GasStation controller | `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Controllers/GasStationController.cs` |
| AutoMapper profile | `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Infrastructure/AutomobileMappingProfile.cs` |
| DI setup | `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Infrastructure/AutomobileInfoServiceStartup.cs` |
| GasStation DTOs | `src/Hmm.ServiceApi.DtoEntity/GasLogNotes/ApiGasStation*.cs` |
| Result filters | `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Filters/GasStation*ResultFilter*.cs` |
| Existing plan file | `plans/reverse-geocoding-plan.md` |

## Open Questions
- Should the geocoding endpoint require authentication (`[Authorize]`)? → Likely yes, for consistency
- Should we cache results in-memory or skip caching for v1? → Skip for v1, add later
- Preferred geocoding provider if Nominatim is too slow? → Defer decision until needed
