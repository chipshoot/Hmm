# Refactoring Plan: Issue #14 - HmmNoteManager Single Responsibility Violation

**Issue Reference:** Design_analysis.txt #14
**Priority:** P1 - Critical (Fix Soon)
**Estimated Effort:** 40-60 hours
**Risk Level:** HIGH
**Created:** 2026-01-06
**Status:** PLANNED - NOT YET IMPLEMENTED

---

## Executive Summary

The `HmmNoteManager` class (313 lines) violates the Single Responsibility Principle (SRP) by handling multiple concerns: CRUD operations, tag association management, validation orchestration, and entity mapping. This refactoring plan outlines a structured approach to decompose this class into focused, maintainable services.

---

## 1. Current State Analysis

### 1.1 File Location
- **Path:** `src/Hmm.Core/DefaultManager/HmmNoteManager.cs`
- **Lines of Code:** 313
- **Interface:** `IHmmNoteManager`
- **Dependencies:** 6 (IVersionRepository, IMapper, ITagManager, IEntityLookup, IDateTimeProvider, IHmmValidator)

### 1.2 Current Responsibilities (SRP Violations)

| Responsibility | Methods | Lines | Percentage |
|----------------|---------|-------|------------|
| **CRUD Operations** | GetNotesAsync, GetNoteByIdAsync, CreateAsync, UpdateAsync, DeleteAsync | ~180 | 57% |
| **Tag Association** | ApplyTag (75 lines), RemoveTag (39 lines) | ~114 | 36% |
| **Validation Orchestration** | Used in CreateAsync, UpdateAsync | ~15 | 5% |
| **Expression Mapping** | Query expression translation | ~4 | 1% |

### 1.3 Complexity Metrics
- **Cyclomatic Complexity:** High (especially ApplyTag method)
- **Dependencies:** 6 injected dependencies
- **Public API Surface:** 7 methods
- **Test Complexity:** Requires mocking 6 dependencies per test

### 1.4 Specific Issues

#### Tag Association Methods (Lines 175-291)
**ApplyTag Method (75 lines):**
- Retrieves note by ID
- Looks up tag by ID or name
- Creates tag if doesn't exist
- Validates tag is activated
- Checks for duplicates
- Adds tag to note
- Updates note entity
- Has 4 different failure paths

**RemoveTag Method (39 lines):**
- Retrieves note by ID
- Finds tag in note's collection
- Removes tag
- Updates note entity

**Problems:**
1. Tag management is a separate concern from note management
2. N+1 query problem (GetNoteByIdAsync + GetTagByIdAsync + UpdateAsync = 3+ queries)
3. Difficult to test tag operations in isolation
4. Cannot reuse tag association logic for other entities
5. Mixes tag business logic with note business logic

---

## 2. Proposed Architecture

### 2.1 Target State - Three Focused Managers

```
┌─────────────────────────────────────────────────────┐
│                 HmmNoteManager                      │
│  (Focused on Note CRUD only)                        │
│                                                     │
│  - GetNotesAsync()                                  │
│  - GetNoteByIdAsync()                               │
│  - CreateAsync()                                    │
│  - UpdateAsync()                                    │
│  - DeleteAsync()                                    │
│                                                     │
│  Dependencies: Repository, Mapper, Validator,       │
│                DateProvider, EntityLookup           │
└─────────────────────────────────────────────────────┘
                         ↑
                         │ uses
                         │
┌─────────────────────────────────────────────────────┐
│            NoteTagAssociationManager                │
│  (Focused on Note-Tag relationships)                │
│                                                     │
│  - ApplyTagToNoteAsync()                            │
│  - RemoveTagFromNoteAsync()                         │
│  - ApplyMultipleTagsAsync()                         │
│  - GetNoteTagsAsync()                               │
│                                                     │
│  Dependencies: NoteManager, TagManager,             │
│                Repository (for optimized queries)   │
└─────────────────────────────────────────────────────┘
```

### 2.2 New Interface: INoteTagAssociationManager

```csharp
namespace Hmm.Core
{
    public interface INoteTagAssociationManager
    {
        /// <summary>
        /// Applies a tag to a note. Creates tag if it doesn't exist.
        /// </summary>
        Task<ProcessingResult<List<Tag>>> ApplyTagToNoteAsync(int noteId, Tag tag);

        /// <summary>
        /// Removes a tag association from a note.
        /// </summary>
        Task<ProcessingResult<List<Tag>>> RemoveTagFromNoteAsync(int noteId, int tagId);

        /// <summary>
        /// Applies multiple tags to a note in a single operation (optimized).
        /// </summary>
        Task<ProcessingResult<List<Tag>>> ApplyMultipleTagsAsync(int noteId, IEnumerable<Tag> tags);

        /// <summary>
        /// Gets all tags associated with a note.
        /// </summary>
        Task<ProcessingResult<List<Tag>>> GetNoteTagsAsync(int noteId);
    }
}
```

### 2.3 Updated IHmmNoteManager Interface

```csharp
namespace Hmm.Core
{
    public interface IHmmNoteManager
    {
        Task<ProcessingResult<HmmNote>> GetNoteByIdAsync(int id, bool includeDelete = false);

        Task<ProcessingResult<PageList<HmmNote>>> GetNotesAsync(
            Expression<Func<HmmNote, bool>> query = null,
            bool includeDeleted = false,
            ResourceCollectionParameters resourceCollectionParameters = null);

        Task<ProcessingResult<HmmNote>> CreateAsync(HmmNote note);

        Task<ProcessingResult<HmmNote>> UpdateAsync(HmmNote note);

        Task<ProcessingResult<Unit>> DeleteAsync(int noteId);

        // TAG METHODS REMOVED - Now in INoteTagAssociationManager
        // - ApplyTag()
        // - RemoveTag()
    }
}
```

---

## 3. Implementation Plan

### Phase 1: Create New Manager (No Breaking Changes)

**Estimated Time:** 8-12 hours

#### Step 1.1: Create INoteTagAssociationManager Interface
- [ ] Create `src/Hmm.Core/INoteTagAssociationManager.cs`
- [ ] Define methods: ApplyTagToNoteAsync, RemoveTagFromNoteAsync, ApplyMultipleTagsAsync, GetNoteTagsAsync
- [ ] Add XML documentation

#### Step 1.2: Implement NoteTagAssociationManager
- [ ] Create `src/Hmm.Core/DefaultManager/NoteTagAssociationManager.cs`
- [ ] Inject dependencies: IHmmNoteManager, ITagManager
- [ ] Copy and refactor ApplyTag logic from HmmNoteManager
- [ ] Copy and refactor RemoveTag logic from HmmNoteManager
- [ ] Implement ApplyMultipleTagsAsync (new, optimized)
- [ ] Implement GetNoteTagsAsync (new)
- [ ] Fix N+1 query issue by batching operations

#### Step 1.3: Register in DI Container
- [ ] Add to `Startup.cs`: `.AddScoped<INoteTagAssociationManager, NoteTagAssociationManager>()`

#### Step 1.4: Create Unit Tests
- [ ] Create `src/Hmm.Core.Tests/NoteTagAssociationManagerTests.cs`
- [ ] Test ApplyTagToNoteAsync (new tag, existing tag, deactivated tag, duplicate)
- [ ] Test RemoveTagFromNoteAsync (existing tag, non-existing tag)
- [ ] Test ApplyMultipleTagsAsync (batch operations)
- [ ] Test error scenarios

**Deliverable:** New service exists alongside old implementation (no breaking changes yet)

---

### Phase 2: Update HmmNoteController to Use New Manager

**Estimated Time:** 4-6 hours

#### Step 2.1: Update HmmNoteController Constructor
- [ ] Inject `INoteTagAssociationManager _tagAssociationManager`
- [ ] Keep existing `IHmmNoteManager _noteManager` for now

#### Step 2.2: Update ApplyTag Endpoint
- [ ] Change from: `_noteManager.ApplyTag(note, tag)`
- [ ] Change to: `_tagAssociationManager.ApplyTagToNoteAsync(id, tag)`
- [ ] Update error handling
- [ ] Update response mapping

#### Step 2.3: Create RemoveTag Endpoint (if needed)
- [ ] Check if RemoveTag endpoint exists
- [ ] Update to use `_tagAssociationManager.RemoveTagFromNoteAsync(noteId, tagId)`

#### Step 2.4: Update Controller Tests
- [ ] Update `NoteControllerTests.cs` to mock `INoteTagAssociationManager`
- [ ] Update test scenarios
- [ ] Verify all tests pass

**Deliverable:** Controller uses new Manager, old methods still exist in HmmNoteManager

---

### Phase 3: Deprecate Old Methods (Transition Period)

**Estimated Time:** 2-4 hours

#### Step 3.1: Mark Old Methods as Obsolete
- [ ] Add `[Obsolete("Use INoteTagAssociationManager.ApplyTagToNoteAsync instead", false)]` to `HmmNoteManager.ApplyTag`
- [ ] Add `[Obsolete("Use INoteTagAssociationManager.RemoveTagFromNoteAsync instead", false)]` to `HmmNoteManager.RemoveTag`

#### Step 3.2: Update Documentation
- [ ] Add migration guide in XML comments
- [ ] Update CLAUDE.md with new manager usage

#### Step 3.3: Search for Other Usages
- [ ] Search codebase for `ApplyTag(` calls
- [ ] Search codebase for `RemoveTag(` calls
- [ ] Update any other consumers

**Deliverable:** Deprecated methods with migration path, warnings in build

---

### Phase 4: Remove Old Methods (Breaking Change)

**Estimated Time:** 4-6 hours

#### Step 4.1: Remove from Interface
- [ ] Remove `ApplyTag` from `IHmmNoteManager`
- [ ] Remove `RemoveTag` from `IHmmNoteManager`
- [ ] Update interface documentation

#### Step 4.2: Remove from Implementation
- [ ] Delete `ApplyTag` method from `HmmNoteManager` (lines 175-249)
- [ ] Delete `RemoveTag` method from `HmmNoteManager` (lines 252-291)
- [ ] Remove `ITagManager` dependency from `HmmNoteManager` (no longer needed)

#### Step 4.3: Update Tests
- [ ] Update `HmmNoteManagerTests.cs` to remove tag-related tests
- [ ] Move tag association tests to `NoteTagAssociationManagerTests.cs`
- [ ] Verify all tests pass

#### Step 4.4: Build and Verify
- [ ] Run full solution build
- [ ] Run all unit tests
- [ ] Run integration tests (if available)

**Deliverable:** Clean separation of concerns, HmmNoteManager reduced to ~200 lines

---

### Phase 5: Performance Optimization

**Estimated Time:** 8-12 hours

#### Step 5.1: Fix N+1 Query Problem
- [ ] Implement batch tag lookup in `ApplyMultipleTagsAsync`
- [ ] Use single update operation for multiple tags
- [ ] Add integration test to verify query count

#### Step 5.2: Add Caching (Optional)
- [ ] Consider caching frequently accessed tags
- [ ] Implement cache invalidation strategy

#### Step 5.3: Performance Testing
- [ ] Benchmark ApplyTagToNoteAsync with 1, 10, 100 tags
- [ ] Compare with old implementation
- [ ] Document performance improvements

**Deliverable:** Optimized tag operations, N+1 query eliminated

---

## 4. Risk Assessment

### 4.1 High Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Breaking API contract** | HIGH | MEDIUM | Use phased approach with deprecation |
| **Data inconsistency during migration** | HIGH | LOW | No data migration needed, only code |
| **Performance regression** | MEDIUM | LOW | Benchmark before/after, optimize in Phase 5 |
| **Test coverage gaps** | MEDIUM | MEDIUM | Write comprehensive tests in Phase 1 |
| **Unforeseen dependencies** | MEDIUM | MEDIUM | Search codebase thoroughly in Phase 3 |

### 4.2 Medium Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Controller tests break** | MEDIUM | HIGH | Update in Phase 2 |
| **Documentation outdated** | LOW | HIGH | Update CLAUDE.md in Phase 3 |
| **DI registration issues** | MEDIUM | LOW | Test DI container in integration tests |

---

## 5. Testing Strategy

### 5.1 Unit Tests (Required)

**NoteTagAssociationManagerTests.cs:**
- ✓ ApplyTagToNoteAsync_WithNewTag_CreatesAndAppliesTag
- ✓ ApplyTagToNoteAsync_WithExistingTag_AppliesTag
- ✓ ApplyTagToNoteAsync_WithDeactivatedTag_ReturnsValidationError
- ✓ ApplyTagToNoteAsync_WithDuplicateTag_ReturnsSuccessWithMessage
- ✓ ApplyTagToNoteAsync_WithInvalidNoteId_ReturnsNotFound
- ✓ RemoveTagFromNoteAsync_WithExistingTag_RemovesTag
- ✓ RemoveTagFromNoteAsync_WithNonExistingTag_ReturnsSuccessWithMessage
- ✓ ApplyMultipleTagsAsync_WithBatch_AppliesAllTags
- ✓ GetNoteTagsAsync_ReturnsAllTagsForNote

**HmmNoteManagerTests.cs (Updates):**
- Remove tag-related tests after Phase 4
- Verify CRUD operations still work

**NoteControllerTests.cs (Updates):**
- Update to mock INoteTagAssociationManager
- Update tag endpoint tests

### 5.2 Integration Tests (Recommended)

- Test full flow: Create note → Apply tag → Retrieve note with tags → Remove tag
- Test query count for N+1 prevention
- Test transaction rollback scenarios

### 5.3 Manual Testing Checklist

- [ ] Create note via API
- [ ] Apply tag to note via API (existing tag)
- [ ] Apply tag to note via API (new tag)
- [ ] Apply same tag twice (should handle gracefully)
- [ ] Apply deactivated tag (should fail validation)
- [ ] Remove tag from note
- [ ] Verify note.Tags property is updated correctly
- [ ] Verify Swagger documentation is updated

---

## 6. Rollback Plan

### 6.1 Before Phase 4 (Low Risk)
**Rollback:** Simply don't proceed to Phase 4. Old methods still exist.
- Remove `[Obsolete]` attributes
- Keep using old methods
- Delete new manager if needed

### 6.2 After Phase 4 (Medium Risk)
**Rollback:** Revert git commits from Phases 1-4
- Restore old IHmmNoteManager interface
- Restore ApplyTag/RemoveTag methods in HmmNoteManager
- Restore old controller implementation
- Restore old tests
- Remove NoteTagAssociationManager registration from DI

### 6.3 Emergency Rollback (Production)
- Deploy previous known-good version
- Investigate issue in development environment
- Fix and redeploy

---

## 7. Estimated Effort

### 7.1 Time Breakdown

| Phase | Task | Hours |
|-------|------|-------|
| **Phase 1** | Create new manager | 8-12 |
| **Phase 2** | Update controller | 4-6 |
| **Phase 3** | Deprecation | 2-4 |
| **Phase 4** | Removal | 4-6 |
| **Phase 5** | Optimization | 8-12 |
| **Testing** | Comprehensive testing | 8-12 |
| **Documentation** | Update docs | 4-6 |
| **Code Review** | Review & revisions | 4-6 |
| **Buffer** | Unforeseen issues | 8-10 |
| **TOTAL** | | **50-74 hours** |

### 7.2 Resource Requirements

- **Developer:** 1 senior developer with architecture experience
- **Reviewer:** 1 senior developer or architect
- **Tester:** QA engineer for integration testing
- **Timeline:** 2-3 weeks (assuming 50% allocation)

---

## 8. Success Criteria

### 8.1 Functional Requirements
- ✓ All existing tag operations continue to work
- ✓ No regression in functionality
- ✓ API contract preserved until Phase 4
- ✓ All tests pass

### 8.2 Non-Functional Requirements
- ✓ HmmNoteManager reduced to ~200 lines (35% reduction)
- ✓ N+1 query eliminated in tag operations
- ✓ Test coverage maintained or improved (>80%)
- ✓ Clear separation of concerns
- ✓ Improved testability (fewer mocked dependencies)

### 8.3 Code Quality Metrics
- ✓ Cyclomatic complexity < 10 per method
- ✓ Single Responsibility Principle satisfied
- ✓ Dependency count reduced (HmmNoteManager: 6→5 dependencies)
- ✓ Code duplication eliminated

---

## 9. Post-Implementation

### 9.1 Documentation Updates
- [ ] Update CLAUDE.md with new manager usage examples
- [ ] Update API documentation (Swagger)
- [ ] Create migration guide for other developers
- [ ] Update architecture diagrams

### 9.2 Knowledge Transfer
- [ ] Code walkthrough session with team
- [ ] Update onboarding documentation
- [ ] Share lessons learned

### 9.3 Monitoring
- [ ] Monitor application logs for errors
- [ ] Track API response times (tag operations)
- [ ] Monitor database query counts
- [ ] Collect user feedback

---

## 10. Future Enhancements (Post-Refactoring)

### 10.1 Additional Improvements
- **Bulk Tag Operations:** Add endpoint to apply tags to multiple notes
- **Tag Suggestions:** Auto-suggest tags based on note content
- **Tag Hierarchies:** Support parent-child tag relationships
- **Tag Analytics:** Track most-used tags, tag trends
- **Tag Validation Rules:** Custom validation per tag type

### 10.2 Other Managers to Refactor
This pattern can be applied to other managers with multiple responsibilities:
- **AuthorManager:** Extract contact management
- **NoteCatalogManager:** Extract schema validation
- **TagManager:** May be fine as-is (single responsibility)

---

## 11. References

- **Design Analysis:** `Design_analysis.txt` Issue #14
- **Current Implementation:** `src/Hmm.Core/DefaultManager/HmmNoteManager.cs`
- **Interface:** `src/Hmm.Core/IHmmNoteManager.cs`
- **Controller:** `src/Hmm.ServiceApi/Areas/HmmNoteService/Controllers/HmmNoteController.cs`
- **Tests:** `src/Hmm.Core.Tests/HmmNoteManagerTests.cs`

---

## 12. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-01-06 | Create detailed plan instead of immediate implementation | High risk, requires careful planning and testing |
| TBD | Approval to proceed | Pending stakeholder review |

---

## Appendix A: Code Samples

### Before (Current State)
```csharp
// HmmNoteManager handling both note CRUD and tag association
public class HmmNoteManager : IHmmNoteManager
{
    private readonly ITagManager _tagManager; // Tag dependency in Note manager

    public async Task<ProcessingResult<List<Tag>>> ApplyTag(HmmNote note, Tag tag)
    {
        // 75 lines of tag management logic mixed with note management
        var hmmNoteResult = await GetNoteByIdAsync(note.Id);
        var tagResult = await _tagManager.GetTagByIdAsync(tag.Id);
        // ... complex logic
        return await UpdateAsync(hmmNote); // N+1 query
    }
}
```

### After (Target State)
```csharp
// HmmNoteManager focused only on note CRUD
public class HmmNoteManager : IHmmNoteManager
{
    // No ITagManager dependency

    public async Task<ProcessingResult<HmmNote>> CreateAsync(HmmNote note) { }
    public async Task<ProcessingResult<HmmNote>> UpdateAsync(HmmNote note) { }
    // ... only note operations
}

// New service focused on tag associations
public class NoteTagAssociationManager : INoteTagAssociationManager
{
    private readonly IHmmNoteManager _noteManager;
    private readonly ITagManager _tagManager;

    public async Task<ProcessingResult<List<Tag>>> ApplyTagToNoteAsync(int noteId, Tag tag)
    {
        // Clean, focused tag association logic
        // Optimized queries (no N+1)
    }
}
```

---

## Appendix B: Migration Example for Consumers

### Old Code (Before Refactoring)
```csharp
// In HmmNoteController
public class HmmNoteController : Controller
{
    private readonly IHmmNoteManager _noteManager;

    [HttpPut("{id:int}/applyTag")]
    public async Task<IActionResult> ApplyTag(int id, [FromBody] ApiTagForApply tag)
    {
        var note = await _noteManager.GetNoteByIdAsync(id);
        var result = await _noteManager.ApplyTag(note.Value, mappedTag);
        return Ok(result);
    }
}
```

### New Code (After Refactoring)
```csharp
// In HmmNoteController
public class HmmNoteController : Controller
{
    private readonly IHmmNoteManager _noteManager;
    private readonly INoteTagAssociationManager _tagAssociationManager; // NEW

    [HttpPut("{id:int}/applyTag")]
    public async Task<IActionResult> ApplyTag(int id, [FromBody] ApiTagForApply tag)
    {
        // No need to fetch note first - manager handles it
        var result = await _tagAssociationManager.ApplyTagToNoteAsync(id, mappedTag);
        return Ok(result);
    }
}
```

---

**END OF REFACTORING PLAN**
