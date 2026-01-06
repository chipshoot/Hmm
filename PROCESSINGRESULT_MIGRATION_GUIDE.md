# ProcessingResult Migration Guide

## Overview

The `ProcessingResult` class has been refactored to be **generic and immutable**, solving critical design issues including thread safety problems, return null ambiguity, and mutable shared state.

## Key Changes

| Aspect | Old (Mutable) | New (Immutable) |
|--------|---------------|-----------------|
| **Type** | Non-generic class | Generic `ProcessingResult<T>` |
| **Mutability** | Mutable (setters, Rest() method) | Immutable (readonly properties) |
| **Value Handling** | Separate return value | Value included in result |
| **Thread Safety** | ❌ Unsafe (shared state) | ✅ Safe (immutable) |
| **Creation** | Constructor + setters | Factory methods (Ok, Fail, etc.) |
| **Usage Pattern** | Property on manager | Return value per operation |

---

## Before (OLD Pattern - DO NOT USE)

```csharp
public class HmmNoteManager : IHmmNoteManager
{
    // ❌ Shared mutable state across all operations
    public ProcessingResult ProcessResult { get; } = new();

    public async Task<HmmNote> UpdateAsync(HmmNote note)
    {
        try
        {
            // ❌ Must manually reset state
            ProcessResult.Rest();

            if (!valid)
            {
                // ❌ Mutate shared state
                ProcessResult.AddErrorMessage("Validation failed");
                ProcessResult.Success = false;
                return null;  // ❌ Ambiguous - what does null mean?
            }

            var noteDao = await GetNoteByIdAsync(note.Id);
            if (noteDao == null)
            {
                ProcessResult.AddErrorMessage("Not found");
                return null;  // ❌ Same return, different meaning
            }

            return updatedNote;  // ✅ Success, but ProcessResult ignored
        }
        catch (Exception ex)
        {
            ProcessResult.WrapException(ex);
            return null;  // ❌ Same return, yet another meaning
        }
    }
}

// API Controller
public async Task<IActionResult> Update(HmmNote note)
{
    var result = await _manager.UpdateAsync(note);

    if (result == null)  // ❌ Is it 404? 400? 500? Who knows!
    {
        // Must check separate ProcessResult property
        return BadRequest(_manager.ProcessResult.GetWholeMessage());
    }

    return Ok(result);
}
```

---

## After (NEW Pattern - USE THIS)

```csharp
public class HmmNoteManager : IHmmNoteManager
{
    // ✅ No more ProcessResult property!

    public async Task<ProcessingResult<HmmNote>> UpdateAsync(HmmNote note)
    {
        try
        {
            // ✅ Explicit validation failure
            if (!valid)
            {
                return ProcessingResult<HmmNote>.ValidationError(
                    "Subject is required",
                    "Content cannot be empty"
                );
            }

            // ✅ Explicit not found
            var noteDao = await GetNoteByIdAsync(note.Id);
            if (noteDao == null)
            {
                return ProcessingResult<HmmNote>.NotFound(
                    $"Note {note.Id} not found"
                );
            }

            // ✅ Explicit mapping error
            var mapped = _mapper.Map<HmmNoteDao>(note);
            if (mapped == null)
            {
                return ProcessingResult<HmmNote>.MappingError(
                    "Failed to map note to DAO"
                );
            }

            var updated = await _repository.UpdateAsync(mapped);

            // ✅ Clear success with value
            return ProcessingResult<HmmNote>.Ok(updated);
        }
        catch (Exception ex)
        {
            // ✅ Explicit exception handling
            return ProcessingResult<HmmNote>.FromException(ex);
        }
    }
}

// API Controller
public async Task<IActionResult> Update(HmmNote note)
{
    var result = await _manager.UpdateAsync(note);

    // ✅ Explicit error handling with proper HTTP status codes
    if (!result.Success)
    {
        return result.ErrorCategory switch
        {
            ErrorCategory.NotFound => NotFound(new
            {
                error = result.GetWholeMessage()
            }),
            ErrorCategory.ValidationError => BadRequest(new
            {
                errors = result.GetErrorMessages()
            }),
            ErrorCategory.ConcurrencyError => Conflict(new
            {
                error = result.GetWholeMessage()
            }),
            _ => StatusCode(500, new
            {
                error = result.GetWholeMessage()
            })
        };
    }

    // ✅ Value is part of the result
    return Ok(result.Value);
}
```

---

## Migration Steps

### Step 1: Update Manager Interface

```csharp
// Before
public interface IHmmNoteManager
{
    Task<HmmNote> GetNoteByIdAsync(int id);
    Task<HmmNote> CreateAsync(HmmNote note);
    Task<HmmNote> UpdateAsync(HmmNote note);
    Task<bool> DeleteAsync(int noteId);

    ProcessingResult ProcessResult { get; }  // ❌ Remove this
}

// After
public interface IHmmNoteManager
{
    Task<ProcessingResult<HmmNote>> GetNoteByIdAsync(int id);  // ✅ Generic result
    Task<ProcessingResult<HmmNote>> CreateAsync(HmmNote note);
    Task<ProcessingResult<HmmNote>> UpdateAsync(HmmNote note);
    Task<ProcessingResult> DeleteAsync(int noteId);  // ✅ Non-generic for void operations

    // ✅ ProcessResult property removed
}
```

### Step 2: Update Manager Implementation

Replace all instances of:
- `ProcessResult.Rest()` → Remove (no longer needed)
- `ProcessResult.AddErrorMessage(msg); return null;` → `return ProcessingResult<T>.Fail(msg)`
- `ProcessResult.AddWarningMessage(msg)` → `result.WithWarnings(msg)`
- `return entity;` → `return ProcessingResult<T>.Ok(entity)`

### Step 3: Update Repository Layer (if applicable)

```csharp
// If repositories also use ProcessingResult, update them:
public interface IVersionRepository<T>
{
    Task<ProcessingResult<T>> AddAsync(T entity);  // ✅ Generic result
    Task<ProcessingResult<T>> UpdateAsync(T entity);
    Task<ProcessingResult> DeleteAsync(T entity);
    // Remove: ProcessingResult ProcessMessage { get; }
}
```

### Step 4: Update API Controllers

Replace null checks with result checks:

```csharp
// Before
var note = await _manager.GetNoteByIdAsync(id);
if (note == null)
{
    return NotFound();
}
return Ok(note);

// After
var result = await _manager.GetNoteByIdAsync(id);
if (!result.Success)
{
    return result.ErrorCategory == ErrorCategory.NotFound
        ? NotFound(new { error = result.GetWholeMessage() })
        : StatusCode(500, new { error = result.GetWholeMessage() });
}
return Ok(result.Value);
```

---

## Common Patterns

### Pattern 1: Simple Success

```csharp
// Old
ProcessResult.Success = true;
return entity;

// New
return ProcessingResult<HmmNote>.Ok(entity);
```

### Pattern 2: Validation Failure

```csharp
// Old
ProcessResult.AddErrorMessage("Invalid input");
return null;

// New
return ProcessingResult<HmmNote>.ValidationError("Invalid input");

// Or multiple errors:
return ProcessingResult<HmmNote>.ValidationError(
    "Subject is required",
    "Content cannot be empty",
    "Author must be specified"
);
```

### Pattern 3: Not Found

```csharp
// Old
if (entity == null)
{
    ProcessResult.AddErrorMessage($"Entity {id} not found");
    return null;
}

// New
if (entity == null)
{
    return ProcessingResult<HmmNote>.NotFound($"Note {id} not found");
}
```

### Pattern 4: Propagating Errors

```csharp
// Old
var entity = await _repository.UpdateAsync(dao);
if (entity == null)
{
    ProcessResult.PropagandaResult(_repository.ProcessMessage);
    return null;
}

// New
var repoResult = await _repository.UpdateAsync(dao);
if (!repoResult.Success)
{
    return ProcessingResult<HmmNote>.Propagate(repoResult);
}
```

### Pattern 5: Adding Warnings to Success

```csharp
// Old
ProcessResult.AddWaningMessage("Deprecated field used");
return entity;

// New
return ProcessingResult<HmmNote>
    .Ok(entity)
    .WithWarnings("Deprecated field used");
```

### Pattern 6: Exception Handling

```csharp
// Old
catch (Exception ex)
{
    ProcessResult.WrapException(ex);
    return null;
}

// New
catch (Exception ex)
{
    _logger.LogError(ex, "Error updating note");
    return ProcessingResult<HmmNote>.FromException(ex);
}
```

### Pattern 7: Non-Generic (Delete/Void Operations)

```csharp
// For operations that don't return a value
public async Task<ProcessingResult> DeleteAsync(int id)
{
    var entity = await _repository.GetByIdAsync(id);
    if (entity == null)
    {
        return ProcessingResult.NotFound($"Entity {id} not found");
    }

    var deleted = await _repository.DeleteAsync(entity);
    if (!deleted)
    {
        return ProcessingResult.Fail("Failed to delete entity");
    }

    return ProcessingResult.Ok();  // No value needed
}
```

---

## Factory Methods Reference

### Success Methods

| Method | Use Case | Example |
|--------|----------|---------|
| `Ok(value)` | Simple success | `ProcessingResult<HmmNote>.Ok(note)` |
| `Ok(value, "info1", "info2")` | Success with info | `ProcessingResult<HmmNote>.Ok(note, "Created successfully")` |

### Failure Methods

| Method | Error Category | HTTP Status | Use Case |
|--------|---------------|-------------|----------|
| `NotFound(msg)` | NotFound | 404 | Entity doesn't exist |
| `ValidationError("err1", "err2")` | ValidationError | 400 | Invalid input |
| `ConcurrencyError(msg)` | ConcurrencyError | 409 | Optimistic locking failure |
| `MappingError(msg)` | MappingError | 500 | AutoMapper failed |
| `BusinessRuleViolation(msg)` | BusinessRuleViolation | 422 | Business rule violated |
| `Fail(msg)` | ServerError | 500 | General error |
| `FromException(ex)` | ServerError | 500 | Exception occurred |

---

## Breaking Changes

⚠️ **This is a breaking change** for:
1. All manager interfaces
2. All manager implementations
3. All API controllers
4. Any code that references `ProcessResult` property

✅ **Not breaking** for:
1. Domain entities
2. DTOs
3. Database layer
4. External APIs

---

## Benefits of New Pattern

1. **Thread-Safe**: Each operation gets its own immutable result
2. **No Null Ambiguity**: Explicit error types replace null returns
3. **Type-Safe**: Value included in result, no separate null check needed
4. **Explicit Errors**: ErrorCategory maps directly to HTTP status codes
5. **Testable**: Easy to test without mocking ProcessResult property
6. **Composable**: Results can be transformed with Map(), WithWarnings(), etc.
7. **Self-Documenting**: Code clearly shows what can go wrong

---

## Gradual Migration Strategy

You can migrate gradually using this approach:

```csharp
// Phase 1: Add new methods alongside old ones
public interface IHmmNoteManager
{
    // Old (mark as obsolete)
    [Obsolete("Use UpdateAsyncV2 instead")]
    Task<HmmNote> UpdateAsync(HmmNote note);

    // New
    Task<ProcessingResult<HmmNote>> UpdateAsyncV2(HmmNote note);
}

// Phase 2: Update controllers to use V2 methods

// Phase 3: Remove old methods and rename V2 → original names
```

---

## Testing Examples

### Old (Hard to Test)

```csharp
[Fact]
public async Task UpdateAsync_WhenNotFound_ReturnsNull()
{
    var result = await _manager.UpdateAsync(note);

    Assert.Null(result);  // ❌ Ambiguous - could be many reasons
    Assert.False(_manager.ProcessResult.Success);  // ❌ Must check separate property
    Assert.Contains("not found", _manager.ProcessResult.GetWholeMessage());  // ❌ String matching
}
```

### New (Easy to Test)

```csharp
[Fact]
public async Task UpdateAsync_WhenNotFound_ReturnsNotFoundResult()
{
    var result = await _manager.UpdateAsync(note);

    Assert.False(result.Success);  // ✅ Clear
    Assert.Equal(ErrorCategory.NotFound, result.ErrorCategory);  // ✅ Explicit
    Assert.Contains("not found", result.GetWholeMessage());  // ✅ Same property
}
```

---

## Questions?

- **Q: Do I need to change my domain entities?**
  A: No, domain entities remain unchanged.

- **Q: What about existing database code?**
  A: Repositories should also return `ProcessingResult<T>` for consistency.

- **Q: Can I still use ProcessingResult without a generic type?**
  A: Yes! Use non-generic `ProcessingResult` for void operations (e.g., Delete).

- **Q: Is the old ProcessingResult.cs file still available?**
  A: Yes, backed up as `ProcessingResult.OLD.cs` for reference.

- **Q: How do I handle multiple errors?**
  A: Use `ValidationError()` with multiple strings or `Fail()` with a list.

---

## Additional Resources

- See `ErrorCategory.cs` for all error types
- See `ProcessingResult.cs` for full API
- See `DESIGN_ANALYSIS.txt` for issues this fixes
- See sample managers in `src/Hmm.Core/DefaultManager/` for examples after migration
