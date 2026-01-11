# Weight Class - Defects and Anti-Patterns Analysis

## Executive Summary
The `Weight` class has **16 critical defects and design anti-patterns** that violate:
- Immutability contract (struct mutability)
- Consistency with sibling classes (Volume, Dimension)
- Type safety and validation
- Operator completeness

## Critical Defects

### 1. 🔴 **CRITICAL: Mutable Struct Violation**
**Location:** `Weight.cs:76, 78-89`

```csharp
// CURRENT - INCORRECT
public WeightUnit Unit { get; set; }  // ❌ Has setter - violates immutability

public int Fractional
{
    get => _fractional;
    set { if (value > 0) { _fractional = value; } }  // ❌ Has setter
}

// SHOULD BE (like Volume/Dimension)
public WeightUnit Unit { get; }  // ✅ Read-only
public int Fractional => _fractional;  // ✅ Read-only
```

**Impact:** Breaks immutability contract declared by `[ImmutableObject(true)]`. Allows unexpected mutations.

**Fix:** Remove setters, make properties read-only.

---

### 2. 🔴 **CRITICAL: Missing `readonly` Modifier**
**Location:** `Weight.cs:30`

```csharp
// CURRENT - INCORRECT
private int _fractional;  // ❌ Not readonly

// SHOULD BE
private readonly int _fractional;  // ✅ Readonly
```

**Impact:** Allows field mutation, violating struct immutability best practices.

---

### 3. 🔴 **CRITICAL: Missing Input Validation in Constructor**
**Location:** `Weight.cs:36-41`

```csharp
// CURRENT - NO VALIDATION
private Weight(long valueInG, WeightUnit unit = WeightUnit.Kilogram, int fractional = 3)
{
    _valueInG = valueInG;
    Unit = unit;
    _fractional = fractional;
}

// SHOULD BE (like Volume/Dimension)
private Weight(long valueInG, WeightUnit unit = WeightUnit.Kilogram, int fractional = 3)
{
    if (fractional < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(fractional),
            "Fractional digits must be non-negative");
    }

    if (!Enum.IsDefined(typeof(WeightUnit), unit))
    {
        throw new ArgumentOutOfRangeException(nameof(unit),
            $"Invalid weight unit: {unit}");
    }

    _valueInG = valueInG;
    Unit = unit;
    _fractional = fractional;
}
```

**Impact:** Allows invalid data (negative fractional, invalid enum values).

---

### 4. 🔴 **CRITICAL: Missing Public Constructor**
**Location:** `Weight.cs:36`

The constructor is private, unlike Volume and Dimension which have public constructors.

```csharp
// CURRENT
private Weight(long valueInG, WeightUnit unit = WeightUnit.Kilogram, int fractional = 3)

// SHOULD ADD
public Weight(double value, WeightUnit unit = WeightUnit.Kilogram, int fractional = 3)
{
    if (fractional < 0)
        throw new ArgumentOutOfRangeException(nameof(fractional),
            "Fractional digits must be non-negative");

    if (!Enum.IsDefined(typeof(WeightUnit), unit))
        throw new ArgumentOutOfRangeException(nameof(unit),
            $"Invalid weight unit: {unit}");

    _valueInG = InternalValue(value, unit);
    Unit = unit;
    _fractional = fractional;
}
```

**Impact:** Users cannot create Weight objects with custom fractional digits directly.

---

### 5. 🟡 **BUG: Precision Loss in FromGrams**
**Location:** `Weight.cs:97`

```csharp
// CURRENT - INCORRECT
public static Weight FromGrams(double value)
{
    var g = (long)value;  // ❌ Truncates instead of rounding
    return new Weight(g, WeightUnit.Gram);
}

// SHOULD BE
public static Weight FromGrams(double value)
{
    var g = (long)Math.Round(value, 0);  // ✅ Rounds properly
    return new Weight(g, WeightUnit.Gram);
}
```

**Impact:** `FromGrams(5.9)` returns 5g instead of 6g.

---

### 6. 🟡 **BUG: ToString "all" Format Error**
**Location:** `Weight.cs:230`

```csharp
// CURRENT - INCORRECT
case "all":
    result = $"{TotalPounds} lbs / {TotalPounds} kg";  // ❌ TotalPounds twice!
    break;

// SHOULD BE
case "all":
    result = $"{TotalPounds} lbs / {TotalKilograms} kg";  // ✅ Correct
    break;
```

**Impact:** Shows incorrect kg value (displays pounds instead).

---

### 7. 🔴 **CRITICAL: Operators Don't Preserve Unit and Fractional**
**Location:** `Weight.cs:136, 147, 155, 163, 171, 177`

```csharp
// CURRENT - INCORRECT
public static Weight operator +(Weight x, Weight y)
{
    var newValue = x._valueInG + y._valueInG;
    return new Weight(newValue);  // ❌ Lost Unit and Fractional from x
}

// SHOULD BE (like Volume)
public static Weight operator +(Weight x, Weight y)
{
    var newValue = x._valueInG + y._valueInG;
    return new Weight(newValue, x.Unit, x.Fractional);  // ✅ Preserves metadata
}
```

**Impact:** Arithmetic operations lose the original unit and fractional settings.

**Affects:** All arithmetic operators (+, -, *, /)

---

### 8. 🟡 **Missing Operators**
**Location:** Missing throughout

Volume and Dimension have these operators, but Weight doesn't:

```csharp
// MISSING
public static bool operator >=(Weight x, Weight y)
public static bool operator >=(Weight x, int y)
public static bool operator <=(Weight x, Weight y)
public static bool operator <=(Weight x, int y)
public static bool operator ==(Weight x, int y)
public static bool operator !=(Weight x, int y)
public static Weight operator %(Weight x, Weight y)  // Modulus
```

**Impact:** Inconsistent API, limited comparison capabilities.

---

### 9. 🟡 **Missing Static Utility Methods**
**Location:** Missing throughout

```csharp
// MISSING - Should add (like Volume/Dimension)
public static Weight Max(params Weight[] items)
{
    if (items.Length == 0)
        throw new ArgumentOutOfRangeException(nameof(items), "No weight object found");

    return items.Aggregate((i1, i2) => i1 > i2 ? i1 : i2);
}

public static Weight Min(params Weight[] items)
{
    if (items.Length == 0)
        throw new ArgumentOutOfRangeException(nameof(items), "No weight object found");

    return items.Aggregate((i1, i2) => i1 < i2 ? i1 : i2);
}

public static Weight Abs(Weight x)
{
    return new Weight(Math.Abs(x._valueInG), x.Unit, x.Fractional);
}
```

**Impact:** Missing convenient utility methods for min/max/abs operations.

---

### 10. 🟡 **Missing ToString Override**
**Location:** `Weight.cs:205`

```csharp
// CURRENT - INCORRECT
public string ToString(string format = null)  // ❌ Not overriding

// SHOULD BE
public override string ToString()
{
    return ToString(null);
}

public string ToString(string format)  // Overload, not override
{
    // ... existing implementation
}
```

**Impact:** Doesn't properly override `object.ToString()`, can cause unexpected behavior.

---

### 11. 🟡 **Incomplete Equals Implementation**
**Location:** `Weight.cs:256-259`

```csharp
// CURRENT - INCORRECT
public bool Equals(Weight other)
{
    return _valueInG == other._valueInG;  // ❌ Ignores Unit and Fractional
}

// SHOULD BE (like Volume)
public bool Equals(Weight other)
{
    return _valueInG == other._valueInG &&
           Unit == other.Unit &&
           Fractional == other.Fractional;
}
```

**Impact:** Two weights with same value but different units/fractional are considered equal.

---

### 12. 🟡 **Incomplete GetHashCode Implementation**
**Location:** `Weight.cs:270-273`

```csharp
// CURRENT - INCORRECT
public override int GetHashCode()
{
    return _valueInG.GetHashCode();  // ❌ Only hashes value
}

// SHOULD BE (like Volume)
public override int GetHashCode()
{
    return HashCode.Combine(_valueInG, Unit, Fractional);
}
```

**Impact:** Violates hash code contract (equal objects must have equal hash codes, but Equals now checks Unit and Fractional).

---

### 13. 🟡 **Inconsistent Factory Method Overloads**
**Location:** `Weight.cs:102-118`

```csharp
// CURRENT - INCONSISTENT
public static Weight FromGrams(double value)  // Only double
public static Weight FromKilograms(int value)    // ❌ int overload
public static Weight FromKilograms(long value)   // ❌ long overload
public static Weight FromKilograms(double value) // double overload
public static Weight FromPounds(double value)  // Only double

// SHOULD BE (like Volume) - All accept double only
public static Weight FromGrams(double value)
public static Weight FromKilograms(double value)
public static Weight FromPounds(double value)
```

**Impact:** API confusion, unnecessary overload complexity.

---

### 14. 🟡 **Flawed Fractional Setter Validation**
**Location:** `Weight.cs:78-89`

```csharp
// CURRENT - INCORRECT
public int Fractional
{
    get => _fractional;
    set
    {
        if (value > 0)  // ❌ Rejects 0, silently ignores invalid values
        {
            _fractional = value;
        }
    }
}

// SHOULD BE - Remove setter entirely (make struct immutable)
public int Fractional => _fractional;
```

**Impact:**
- Silently ignores invalid input instead of throwing exception
- Rejects valid value of 0
- Violates immutability

---

### 15. 🟡 **Poor Internal Precision**
**Location:** `Weight.cs:28`

```csharp
// CURRENT
private readonly long _valueInG;  // Stores whole grams

// BETTER (like Volume uses microliters)
private readonly long _valueInMilligrams;  // Or micrograms for better precision
```

**Impact:** Cannot accurately represent sub-gram weights (e.g., 0.5 grams).

---

### 16. 🟡 **Struct Not Marked as `readonly`**
**Location:** `Weight.cs:22`

```csharp
// CURRENT
public struct Weight : IEquatable<Weight>, IComparable<Weight>

// SHOULD BE (like Volume/Dimension)
public readonly struct Weight : IEquatable<Weight>, IComparable<Weight>
```

**Impact:** Compiler cannot enforce immutability guarantees, potential performance issues.

---

## Comparison Table

| Feature | Volume | Dimension | Weight | Status |
|---------|--------|-----------|--------|--------|
| `readonly struct` | ✅ | ✅ | ❌ | Missing |
| Public constructor | ✅ | ✅ | ❌ | Missing |
| Constructor validation | ✅ | ✅ | ❌ | Missing |
| Readonly fields | ✅ | ✅ | ❌ | `_fractional` not readonly |
| Readonly properties | ✅ | ✅ | ❌ | Unit and Fractional have setters |
| Operators preserve Unit/Fractional | ✅ | ✅ | ❌ | Lost in operators |
| Complete operators (>=, <=, %, ==int) | ✅ | ✅ | ❌ | Missing many |
| Max/Min/Abs static methods | ✅ | ✅ | ❌ | Missing |
| ToString override | ✅ | ✅ | ❌ | Not overridden |
| Equals checks all fields | ✅ | ✅ | ❌ | Only checks value |
| HashCode includes all fields | ✅ | ✅ | ❌ | Only hashes value |
| Consistent factory methods | ✅ | ✅ | ❌ | Inconsistent overloads |
| Sub-unit precision | ✅ (microliters) | ✅ (microns) | ❌ | Only whole grams |

---

## Recommended Actions

### Priority 1 - Critical (Breaking Changes)
1. Make struct `readonly`
2. Remove setters from `Unit` and `Fractional`
3. Make `_fractional` field `readonly`
4. Add public constructor with validation
5. Add validation to private constructor
6. Fix operators to preserve Unit and Fractional

### Priority 2 - Important (API Completeness)
7. Add missing operators (>=, <=, %, ==int, !=int)
8. Add Max/Min/Abs static methods
9. Fix Equals to check Unit and Fractional
10. Fix GetHashCode to include all fields
11. Add ToString override

### Priority 3 - Quality Improvements
12. Fix FromGrams precision loss
13. Fix ToString "all" format bug
14. Remove unnecessary FromKilograms overloads
15. Consider using milligrams/micrograms internally for precision
16. Update XML documentation

---

## Suggested Refactored Weight Class

A refactored Weight class should follow the same pattern as Volume:
- `readonly struct` modifier
- Public and private constructors with validation
- `readonly` fields throughout
- Read-only properties
- Operators that preserve Unit and Fractional
- Complete operator set
- Static utility methods (Max, Min, Abs)
- Proper Equals/GetHashCode implementation
- Internal representation in sub-units (milligrams or micrograms)

---

## Code Smell Summary
- **Mutability in Immutable Type**: 🔴 Critical
- **Missing Validation**: 🔴 Critical
- **Inconsistent API**: 🟡 Important
- **Incomplete Implementation**: 🟡 Important
- **Precision Issues**: 🟡 Important
- **Documentation Bugs**: 🟡 Minor
