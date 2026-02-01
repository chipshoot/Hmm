# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Building the Solution
```bash
# Build entire solution
dotnet build Hmm.sln

# Build specific project
dotnet build src/Hmm.ServiceApi/Hmm.ServiceApi.csproj

# Build in Release mode
dotnet build Hmm.sln -c Release
```

### Running Tests
```bash
# Run all tests in solution
dotnet test Hmm.sln

# Run tests for specific project
dotnet test src/Hmm.Core.Tests/Hmm.Core.Tests.csproj

# Run tests with coverage
dotnet test Hmm.sln --collect:"XPlat Code Coverage"

# Run single test (use test method name)
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

### Running the Applications
```bash
# Run the main API service
dotnet run --project src/Hmm.ServiceApi/Hmm.ServiceApi.csproj

# Run the Identity Provider
dotnet run --project src/Hmm.Idp/Hmm.Idp.csproj

# Using Docker Compose (starts API + PostgreSQL)
docker-compose up
```

### Database Operations
```bash
# Add new migration (from Hmm.Core.Dal.EF project)
cd src/Hmm.Core.Dal.EF
dotnet ef migrations add MigrationName

# Update database to latest migration
dotnet ef database update

# Generate SQL script for migration
dotnet ef migrations script
```

## Architecture Overview

This is a multi-layered .NET 8.0 application following Domain-Driven Design (DDD) principles with clear separation of concerns.

### Layer Structure

```
API Layer (Hmm.ServiceApi)
    ↓
DTO Layer (Hmm.ServiceApi.DtoEntity)
    ↓
Business Logic (Hmm.Core + Domain Modules)
    ↓
Data Access (Hmm.Core.Dal.EF + Hmm.Core.Map)
    ↓
Infrastructure (Hmm.Utility)
```

### Key Projects and Responsibilities

**Hmm.ServiceApi** - ASP.NET Core 8.0 REST API
- API routes use `/api/v{version}/` prefix to separate from web application routes
- JWT Bearer authentication validated against Hmm.Idp
- Swagger/OpenAPI documentation at `/swagger`
- Controllers organized by domain areas (HmmNoteService, AutomobileInfoService)
- Uses result filters for consistent DTO projection

**Hmm.Core** - Business logic layer with Manager pattern
- `IHmmNoteManager` / `HmmNoteManager` - Core note operations
- `IAuthorManager` / `AuthorManager` - Author management
- `ITagManager` / `TagManager` - Tag operations
- All operations return `ProcessingResult` for error handling
- Expression-based querying with domain/DAO model mapping

**Hmm.Core.Dal.EF** - Entity Framework Core 8.0 data access
- PostgreSQL primary database (uses Npgsql.EntityFrameworkCore.PostgreSQL)
- SQL Server fallback support available
- Repository pattern with `IRepository<T>` and `IVersionRepository<T>`
- Optimistic concurrency using Version/Timestamp fields
- Async-first approach for all database operations

**Hmm.Core.Map** - Entity mapping layer
- Domain entities in `DomainEntity/` folder (business logic models)
- DAO entities in `DbEntity/` folder (database-mapped models)
- AutoMapper profiles for bidirectional conversions
- `ExpressionMapper` for LINQ query translation between domain/DAO models

**Hmm.ServiceApi.DtoEntity** - API contracts
- Separate DTOs for Create/Update/Read operations
- `ApiMappingProfile` maps between domain entities and DTOs
- Property mapping services for dynamic sorting/filtering via query strings

**Hmm.Automobile** - Domain module for vehicle expense tracking
- `AutomobileInfo`, `GasLog`, `GasDiscount` entities
- Stores complex objects as serialized note content using JSON
- Subject pattern: `GasLog,AutomobileId:{id}` for searchability

**Hmm.BigCalendar** - Domain module for calendar/appointment management
- `Appointment` entity with Guid-based primary keys
- `IAppointmentManager` for CRUD and date range queries

**Hmm.Idp** - Duende IdentityServer 7 identity provider (.NET 9.0)
- ASP.NET Identity for user management
- OAuth 2.0 / OpenID Connect authentication
- JWT token generation
- Runs on separate port (default: https://localhost:5001)
- Strict password policy (12+ chars, complexity requirements)
- Account lockout after 5 failed attempts

**Hmm.Utility** - Cross-cutting infrastructure
- Base entity classes: `Entity`, `VersionedEntity`, `GuidEntity`
- Repository interfaces: `IRepository<T>`, `IVersionRepository<T>`
- `PageList<T>` for pagination support
- `ProcessingResult` for operation feedback
- Value types: `Money`, `Dimension`, `Volume`
- `Guard` clauses for validation

**Hmm.Infrastructure** - IDP integration
- `IdpUserProfileProvider` fetches user profile from IDP using access tokens

## Important Architectural Patterns

### Manager Pattern
Business logic is encapsulated in manager classes (e.g., `HmmNoteManager`, `AuthorManager`). All managers:
- Have interface contracts in `Hmm.Core`
- Have implementations in `Hmm.Core/DefaultManager`
- Return `ProcessingResult` for operation outcomes
- Work with domain entities (not DAOs directly)

### Expression Mapping Pattern
The codebase separates domain entities from database entities:
- Service layer works with domain models (e.g., `HmmNote`)
- Repository layer works with DAO models (e.g., `HmmNoteDao`)
- `ExpressionMapper<TSource, TDest>.MapExpression()` translates LINQ queries between layers
- This allows clean domain model isolation from EF annotations

### Soft Deletion Pattern
Notes use `IsDeleted` flag instead of actual deletion:
- Preserves referential integrity and audit trails
- Queries filter deleted records by default
- Authors and Tags use `IsActivated` flag for similar deactivation pattern

### Repository Pattern with Lookup Service
Generic entity retrieval through:
- `IEntityLookup` for type-safe entity lookups
- `IRepository<T>` for standard CRUD operations
- `IVersionRepository<T>` for entities with optimistic concurrency

### Note-Based Storage Pattern
Complex domain objects can be stored as note content:
- Used by Automobile module (GasLog stored as JSON in note)
- Subject field used for searchability and categorization
- Leverages existing HmmNote infrastructure for any domain

## Configuration

### appsettings.json Structure
- `AppSettings.MaxPageSize` - Maximum page size for paginated queries (default: 100)
- `idpBaseUrl` - Identity Provider URL (default: https://localhost:5001)
- `Serilog` - Logging configuration with Seq sink (http://localhost:5341)
- Connection strings for PostgreSQL and SQL Server

### Database Configuration
Primary database is PostgreSQL. Connection string should be configured in appsettings or user secrets:
```json
{
  "ConnectionStrings": {
    "HmmNoteConnection": "Host=localhost;Database=hmm;Username=user;Password=pass"
  }
}
```

## Testing Framework

All test projects use:
- **xUnit** as the test framework
- **Coverlet** for code coverage
- **Hmm.Utility.TestHelp** for shared test utilities
- Tests follow naming convention: `ProjectName.Tests`

## Authentication Flow

1. Client authenticates with **Hmm.Idp** (IdentityServer) to get JWT access token
2. Client includes token in API requests to **Hmm.ServiceApi** as Bearer token
3. ServiceApi validates token against Idp authority
4. ServiceApi can use `IdpUserProfileProvider` to fetch user claims from Idp

## Key Domain Entities

**HmmNote** - Core note entity with:
- Subject, Content, CreateDate, LastModifiedDate
- `Author` (required) - Who created/owns the note
- `NoteCatalog` (optional) - Template/schema for note structure
- `Tags` (many-to-many) - Categorization tags
- `IsDeleted` flag for soft deletion
- `Version` byte[] for optimistic concurrency

**Author** - Note author with:
- AccountName, Role (AuthorRoleType enum)
- `Contact` information (optional)
- `IsActivated` flag

**Tag** - Categorization with:
- Name, `IsActivated` flag
- Many-to-many relationship with Notes

**NoteCatalog** - Note templates with:
- Name, Schema (JSON), Render (HTML)
- `NoteContentFormatType` (Plain, Html, Markdown, etc.)

## Development Notes

### Adding New Domain Entities
1. Create domain entity in appropriate module (or Hmm.Core.Map/DomainEntity)
2. Create corresponding DAO entity in Hmm.Core.Map/DbEntity
3. Add AutoMapper configuration in mapping profile
4. Create repository interface and implementation in Hmm.Core.Dal.EF
5. Create manager interface in Hmm.Core and implementation in Hmm.Core/DefaultManager
6. Create API DTOs in Hmm.ServiceApi.DtoEntity
7. Create controller in appropriate ServiceApi area

### Working with Migrations
Migrations are managed in `Hmm.Core.Dal.EF` project. When adding new entities:
1. Update DbContext to include new DbSet
2. Run `dotnet ef migrations add MigrationName` from Hmm.Core.Dal.EF directory
3. Review generated migration for correctness
4. Apply with `dotnet ef database update`

### API Versioning
Current API version is v1.0. All API routes use `/api/v{version}/` prefix to allow the same domain (homemademessage.com) to serve both API and web application:

**HmmNoteService endpoints:**
- `/api/v1/notes` - Note CRUD operations
- `/api/v1/authors` - Author management
- `/api/v1/contacts` - Contact information
- `/api/v1/tags` - Tag operations (GET by name: `/api/v1/tags/by-name/{name}`)
- `/api/v1/notecatalogs` - Note catalog/template management

**AutomobileInfoService endpoints:**
- `/api/v1/automobiles` - Automobile management
- `/api/v1/automobiles/{autoId}/gaslogs` - Gas log entries for specific automobile
- `/api/v1/automobiles/gaslogs/discounts` - Discount program management
- `/api/v1/automobiles/gasstations` - Gas station management

### Result Filters
Controllers use result filters to automatically map entities to DTOs:
- `[NoteResultFilter]` for single note responses
- `[NotesResultFilter]` for collections
- Similar filters exist for Author, Tag, etc.
- This keeps controller actions focused on business logic

### Pagination
All collection endpoints support pagination via `ResourceCollectionParameters`:
- `PageNumber` - Page number (1-based)
- `PageSize` - Items per page (max: AppSettings.MaxPageSize)
- `OrderBy` - Comma-separated property names for sorting
- Response includes X-Pagination header with metadata
