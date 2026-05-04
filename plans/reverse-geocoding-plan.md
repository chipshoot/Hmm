# Plan: Reverse Geocoding - Get Address from Geo Location

## Overview
Add a new API endpoint that returns address information (street, city, state, country, zip) based on latitude/longitude coordinates. This leverages the existing `GasStation` entity which already has `Latitude`/`Longitude` fields.

## Architecture Decision
- Use an external reverse geocoding provider (e.g., Nominatim/OpenStreetMap, Google Maps, Azure Maps)
- Introduce a new `IGeocodingService` abstraction in the core layer
- Expose via a new API endpoint under the automobile area

---

## Implementation Steps

### Phase 1: Domain Model

**1.1 Create Address Result Model**
- File: `src/Hmm.Automobile/DomainEntity/GeoAddress.cs`
- Properties: `Street`, `City`, `State`, `Country`, `ZipCode`, `FormattedAddress`, `Latitude`, `Longitude`
- This is a read-only result model (no persistence needed)

### Phase 2: Service Abstraction

**2.1 Define Geocoding Service Interface**
- File: `src/Hmm.Automobile/IGeocodingService.cs`
- Method: `Task<ProcessingResult<GeoAddress>> ReverseGeocodeAsync(double latitude, double longitude)`

**2.2 Implement Geocoding Service**
- File: `src/Hmm.Automobile/GeocodingService.cs` (or `src/Hmm.Infrastructure/`)
- Use `HttpClient` to call external geocoding API (e.g., Nominatim for free tier)
- Parse response into `GeoAddress` model
- Handle errors (network failures, rate limits, invalid coordinates)

**2.3 Configuration**
- Add geocoding provider settings to `appsettings.json`:
  ```json
  "GeocodingSettings": {
    "Provider": "Nominatim",
    "BaseUrl": "https://nominatim.openstreetmap.org",
    "ApiKey": ""
  }
  ```

### Phase 3: API Layer

**3.1 Create API DTO**
- File: `src/Hmm.ServiceApi.DtoEntity/GasLogNotes/ApiGeoAddress.cs`
- Properties matching `GeoAddress` domain model

**3.2 Add AutoMapper Mapping**
- File: `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Infrastructure/AutomobileMappingProfile.cs`
- Add `CreateMap<GeoAddress, ApiGeoAddress>()`

**3.3 Create Controller Endpoint**
- File: `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Controllers/GeocodingController.cs`
- Or add to existing `GasStationController.cs` as a new action
- Endpoint: `GET /v1/automobiles/geocoding/reverse?latitude={lat}&longitude={lng}`
- Validate coordinates: latitude [-90, 90], longitude [-180, 180]
- Return `ApiGeoAddress`

**3.4 Create Result Filter** (if using separate controller)
- File: `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Filters/GeoAddressResultFilterAttribute.cs`

### Phase 4: Dependency Injection

**4.1 Register Services**
- File: `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Infrastructure/AutomobileInfoServiceStartup.cs`
- Register `IGeocodingService` as scoped/singleton
- Register `HttpClient` with named client for geocoding provider

### Phase 5: Optional Enhancements

**5.1 Auto-fill Address on GasStation Create/Update**
- When a `GasStation` is created/updated with lat/lng but no address, auto-populate address fields
- Modify `GasStationManager.CreateAsync()` / `UpdateAsync()` to call `IGeocodingService`

**5.2 Caching**
- Add in-memory or distributed cache for geocoding results
- Avoid redundant API calls for same coordinates

**5.3 Batch Geocoding**
- Endpoint to resolve addresses for multiple coordinate pairs

### Phase 6: Testing

**6.1 Unit Tests**
- `src/Hmm.Automobile.Tests/GeocodingServiceTests.cs` - Test parsing, error handling
- Mock `HttpClient` responses for deterministic testing

**6.2 Controller Tests**
- `src/Hmm.ServiceApi.Core.Tests/GeocodingControllerTests.cs`
- Test validation (invalid coordinates), success response, service error handling

---

## Key Files to Create/Modify

| Action | File |
|--------|------|
| Create | `src/Hmm.Automobile/DomainEntity/GeoAddress.cs` |
| Create | `src/Hmm.Automobile/IGeocodingService.cs` |
| Create | `src/Hmm.Automobile/GeocodingService.cs` |
| Create | `src/Hmm.ServiceApi.DtoEntity/GasLogNotes/ApiGeoAddress.cs` |
| Create | `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Controllers/GeocodingController.cs` |
| Modify | `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Infrastructure/AutomobileMappingProfile.cs` |
| Modify | `src/Hmm.ServiceApi/Areas/AutomobileInfoService/Infrastructure/AutomobileInfoServiceStartup.cs` |
| Modify | `src/Hmm.ServiceApi/appsettings.json` (add geocoding config) |
| Create | `src/Hmm.Automobile.Tests/GeocodingServiceTests.cs` |
| Create | `src/Hmm.ServiceApi.Core.Tests/GeocodingControllerTests.cs` |

## Dependencies
- `System.Net.Http` (already available)
- `System.Text.Json` (already available)
- No new NuGet packages required if using Nominatim (free, no API key needed)

## Notes
- Nominatim has a rate limit of 1 request/second - consider caching or queuing
- For production, consider a paid provider (Google Maps, Azure Maps) for higher rate limits
- Coordinate validation already exists in `GasStationValidator` - reuse the [-90,90] / [-180,180] range
