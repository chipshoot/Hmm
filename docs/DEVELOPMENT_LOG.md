# Development Log

## System Architecture Overview

The Hmm system uses a two-service architecture:

- **IDP (Identity Provider):** Manages authentication, user accounts (`ApplicationUser`), and identity claims (name, email, picture). Issues JWTs consumed by the Service API.
- **Service API:** Manages application data — notes, authors, catalogs, tags, gas logs. The `Author` entity represents a user within the application domain.

```
┌─────────────┐         JWT          ┌──────────────────┐
│     IDP     │ ──────────────────── │   Service API    │
│             │                      │                  │
│ Application │   Claims:           │ Author           │
│ User        │   - sub             │  - AccountName   │
│  - Email    │   - name            │  - ContactInfo   │
│  - Name     │   - email           │  - Role          │
│  - Picture  │   - picture         │  - Bio           │
│             │                      │  - AvatarUrl     │
│             │                      │  - TimeZone      │
└─────────────┘                      └──────────────────┘
```

### IDP-to-Author Data Flow

1. User authenticates via IDP, receives JWT with claims (`sub`, `name`, `email`, `picture`)
2. Client sends JWT as Bearer token to Service API
3. `CurrentUserAuthorProvider` extracts `sub` claim to look up or create `Author`
4. On first login (author creation), `picture` claim is synced to `Author.AvatarUrl`
5. Subsequent profile updates (bio, timezone) are managed directly on `Author` via API

---

## Design Decisions

### Profile-on-Author vs Profile-as-Note

**Decision:** Store profile fields directly on `Author` entity.

**Alternatives considered:**

1. **Profile as a Note** — Store bio/avatar/timezone as a special note with a dedicated catalog.
   - Rejected: Creates semantic mismatch (a profile is not a note), complicates queries (must join through notes to get basic profile data), and introduces dual source-of-truth problems.

2. **Separate Profile entity** — Create a new `UserProfile` entity linked to `Author`.
   - Rejected: Over-engineering for 3 fields. Adds unnecessary join complexity. Can be refactored later if profile grows significantly.

3. **Extend Author directly** — Add `Bio`, `AvatarUrl`, `TimeZone` columns to `Author`.
   - Chosen: Simple, follows existing patterns, no new entities or relationships needed. AutoMapper handles the new fields automatically via convention-based mapping.

**Rationale:** The Author entity already serves as the application-level user representation. Profile fields are intrinsic to the author concept. Adding them directly avoids unnecessary abstraction layers while keeping the domain model clean.

### IDP vs Author Field Ownership

| Field | Owner | Rationale |
|-------|-------|-----------|
| Name, Email | IDP | Identity fields managed by authentication provider |
| Picture | IDP (synced to Author) | One-way sync on author creation; Author.AvatarUrl can diverge |
| Bio | Author | App-specific, no IDP equivalent |
| TimeZone | Author | User preference for app behavior |

---

## Schema Changelog

### 2026-02-12: Add Profile Fields to Author

**Migration:** Add `bio`, `avatarurl`, `timezone` columns to Author table.

| Column | Type | Max Length | Nullable | Default |
|--------|------|-----------|----------|---------|
| `bio` | string | 2000 | Yes | NULL |
| `avatarurl` | string | 500 | Yes | NULL |
| `timezone` | string | 100 | Yes | NULL |

**Files modified:**
- `Author.cs` — Domain entity: added Bio, AvatarUrl, TimeZone properties
- `AuthorDao.cs` — DAO: added bio, avatarurl, timezone columns
- `ApiAuthor.cs` — Response DTO: added fields
- `ApiAuthorForCreate.cs` — Create DTO: added fields
- `ApiAuthorForUpdate.cs` — Update DTO: added fields with StringLength validation
- `CurrentUserAuthorProvider.cs` — Populates AvatarUrl from JWT `picture` claim on author creation
- `SampleDataGenerator.cs` — Test data updated with sample profile values
- `AuthorMappingTests.cs` — Mapping assertions for new fields

---

## Future Considerations

- **Avatar upload:** Currently AvatarUrl points to external URLs (e.g., IDP picture claim). A future iteration could add file upload with storage (Azure Blob / S3) and generate the URL server-side.
- **Timezone usage:** Once stored, timezone can drive server-side date formatting, notification scheduling, and activity summaries.
- **Profile sync endpoint:** A dedicated endpoint to re-sync IDP claims (name, picture) to Author fields on demand, rather than only on first login.
- **Profile completeness:** Consider adding fields like `DisplayName`, `Locale`, or `PreferredTheme` as user-facing features grow.
