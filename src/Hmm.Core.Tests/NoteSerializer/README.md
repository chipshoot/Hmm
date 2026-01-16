# DefaultJsonNoteSerializer Tests

## Overview
`DefaultJsonNoteSerializerTests.cs` provides comprehensive test coverage for the `DefaultJsonNoteSerializer<T>` base class.

## Test Coverage

### 1. GetEntity Tests (6 tests)
- ✅ Null note handling
- ✅ HmmNote type deserialization
- ✅ Non-HmmNote type handling
- ✅ ProcessingResult pattern validation

### 2. GetNote Tests (4 tests)
- ✅ Null entity handling
- ✅ Valid HmmNote serialization
- ✅ JSON content preservation
- ✅ Output structure validation

### 3. GetNoteSerializationText Tests (2 tests)
- ✅ Null entity handling
- ✅ Valid JSON string generation

### 4. GetNoteContent Tests (7 tests)
- ✅ Empty string handling
- ✅ Plain text wrapping
- ✅ Valid note JSON handling
- ✅ Invalid JSON recovery
- ✅ Missing structure wrapping
- ✅ JsonElement conversion
- ✅ Structure validation

### 5. IsValidNoteStructure Tests (4 tests)
- ✅ Valid structure detection
- ✅ Missing 'note' property detection
- ✅ Missing 'content' property detection
- ✅ Null document handling

### 6. Helper Method Tests (14 tests)
- ✅ GetStringProperty (valid & missing)
- ✅ GetIntProperty (valid & missing)
- ✅ GetDateTimeProperty (valid & missing)
- ✅ GetBoolProperty (valid & missing)
- ✅ GetDoubleProperty (valid & missing)

### 7. CreateEmptyNoteDocument Tests (1 test)
- ✅ Empty note structure creation

### 8. CreateNoteJsonDocument Tests (2 tests)
- ✅ Content wrapping
- ✅ Null content handling

### 9. GetCatalog Tests (1 test)
- ✅ Default catalog generation

### 10. Edge Cases and Error Handling (6 tests)
- ✅ Empty content handling
- ✅ Special characters (< > & " ')
- ✅ Unicode characters (中文, emoji)
- ✅ ProcessingResult success properties
- ✅ ProcessingResult failure properties

## Total Test Count: **47 comprehensive tests**

## Running the Tests

```bash
# Run all tests in this file
dotnet test --filter "FullyQualifiedName~DefaultJsonNoteSerializerTests"

# Run a specific test
dotnet test --filter "FullyQualifiedName~DefaultJsonNoteSerializerTests.GetEntity_WithNullNote_ReturnsFailureResult"

# Run with verbose output
dotnet test --filter "FullyQualifiedName~DefaultJsonNoteSerializerTests" --logger "console;verbosity=detailed"
```

## Test Structure

The test class includes a nested `TestJsonNoteSerializer` helper class that:
- Extends `DefaultJsonNoteSerializer<HmmNote>` for concrete testing
- Exposes protected methods as public for unit testing
- Allows testing of internal functionality without reflection

## Key Testing Patterns

### 1. ProcessingResult Pattern Testing
```csharp
// Success case
var result = _serializer.GetEntity(validNote);
Assert.True(result.Success);
Assert.NotNull(result.Value);
Assert.False(result.HasError);

// Failure case
var result = _serializer.GetEntity(null);
Assert.False(result.Success);
Assert.True(result.HasError);
Assert.Contains("expected message", result.ErrorMessage);
```

### 2. JSON Structure Validation
```csharp
var result = _serializer.GetNote(entity);
using var jsonDoc = JsonDocument.Parse(result.Value.Content);
Assert.True(jsonDoc.RootElement.TryGetProperty("note", out var noteElement));
Assert.True(noteElement.TryGetProperty("content", out var contentElement));
```

### 3. Helper Method Testing
```csharp
var json = "{\"property\":\"value\"}";
using var doc = JsonDocument.Parse(json);
var element = doc.RootElement;
var result = _serializer.TestGetStringProperty(element, "property");
Assert.Equal("value", result);
```

## Expected JSON Structure

All tests validate against this standard structure:

```json
{
  "note": {
    "content": "..." or { ... }
  }
}
```

## Dependencies

- **xUnit**: Test framework
- **Microsoft.Extensions.Logging**: For logger injection
- **System.Text.Json**: For JSON operations
- **Hmm.Core.Map.DomainEntity**: For domain entities

## Notes

1. The tests do NOT depend on `CoreTestFixtureBase` - they are standalone
2. All tests properly dispose JsonDocument instances to prevent memory leaks
3. Tests cover both success and failure paths extensively
4. Edge cases include special characters, Unicode, and malformed JSON
5. Tests verify the immutable ProcessingResult pattern works correctly

## Future Enhancements

Potential areas for additional testing:
- [ ] Schema validation with actual JSON schemas
- [ ] Performance testing with large JSON documents
- [ ] Concurrent access testing (thread safety)
- [ ] Integration tests with actual persistence layer
- [ ] Derived class behavior (GasLogJsonNoteSerialize, etc.)
