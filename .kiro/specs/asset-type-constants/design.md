# Design Document: Asset Type Constants

## Overview

This feature centralises all known game API `typeC` string codes into a single `AssetTypeCodes` static constants class in `OE2EmpireTracker.Common/Constants/`. The class follows the existing pattern established by `SlotTypes`, `BlueprintTypes`, and `GameConstants` — a static class with `public const string` fields in the `OE2EmpireTracker.Constants` namespace.

The refactoring is purely mechanical: raw string literals are replaced with constant references of identical value. No runtime behaviour changes. The magic-strings audit tool is extended to scan the Common project so future regressions are caught automatically.

### Design Goals

1. Single source of truth for all typeC values
2. Compile-time safety — typos become compile errors
3. Self-documenting code via XML comments on each constant
4. Trailing-space normalisation at comparison sites (constants store trimmed canonical form)
5. Extended audit coverage to prevent future raw literal drift

## Architecture

```
┌─────────────────────────────────────────────────────┐
│ OE2EmpireTracker.Common                             │
│                                                     │
│  Constants/                                         │
│    AssetTypeCodes.cs  ← NEW (public const string)   │
│    BlueprintTypes.cs  (existing pattern)            │
│    SlotTypes.cs       (existing pattern)            │
│    GameConstants.cs   (existing pattern)            │
│                                                     │
│  Services/                                          │
│    AssetMergeService.cs   ← uses AssetTypeCodes.*   │
│    QueueSyncService.cs    ← uses AssetTypeCodes.*   │
│    GameApiSyncScheduler.cs← uses AssetTypeCodes.*   │
│    ColonyMergeService.cs  ← uses AssetTypeCodes.*   │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ OE2EmpireTracker.Tests                              │
│                                                     │
│  Services/                                          │
│    QueueSyncServicePropertyTests.cs ← updated gens  │
│    AssetMergeServiceTests.cs        ← updated refs  │
│  Constants/                                         │
│    AssetTypeCodesTests.cs           ← NEW           │
└─────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────┐
│ .kiro/tools/                                        │
│    magic-strings.js  ← extended to scan Common      │
└─────────────────────────────────────────────────────┘
```

## Components and Interfaces

### AssetTypeCodes Class

```csharp
namespace OE2EmpireTracker.Constants
{
    /// <summary>
    /// Defines string constants for game API typeC codes used to classify
    /// cargo items and asset locations. API codes are case-sensitive, but
    /// comparison code uses OrdinalIgnoreCase for resilience against API
    /// inconsistencies.
    /// 
    /// NOTE: The "SH" vs "Sh" pair is ambiguous — "SH" (both uppercase)
    /// means ShipHull, while "Sh" (capital S, lowercase h) means Share.
    /// This pair requires case-sensitive discrimination in MapAssetTypeC.
    /// </summary>
    public static class AssetTypeCodes
    {
        // --- Cargo Item TypeC Codes (swagger-confirmed) ---
        
        /// <summary>Blueprint ("Bp") — swagger-confirmed.</summary>
        public const string Blueprint = "Bp";
        
        /// <summary>Crate ("Cr") — swagger-confirmed.</summary>
        public const string Crate = "Cr";
        
        /// <summary>Survey ("Sc") — swagger-confirmed.</summary>
        public const string Survey = "Sc";
        
        /// <summary>Ship Part ("S") — swagger-confirmed.</summary>
        public const string ShipPart = "S";
        
        /// <summary>Flatpack ("F") — swagger-confirmed.</summary>
        public const string Flatpack = "F";
        
        /// <summary>Workforce ("W") — swagger-confirmed.</summary>
        public const string Workforce = "W";
        
        /// <summary>Resource ("R") — swagger-confirmed.</summary>
        public const string Resource = "R";
        
        /// <summary>Ammunition ("A") — swagger-confirmed.</summary>
        public const string Ammunition = "A";
        
        /// <summary>Deployable ("D") — swagger-confirmed.</summary>
        public const string Deployable = "D";
        
        /// <summary>Share ("Sh") — swagger-confirmed. Case-sensitive: lowercase h.</summary>
        public const string Share = "Sh";
        
        /// <summary>Commodity ("L") — swagger-confirmed.</summary>
        public const string CommodityL = "L";
        
        // --- Cargo Item TypeC Codes (discovered in real API data) ---
        
        /// <summary>Commodity ("C") — observed in real API data, not in swagger type filter.</summary>
        public const string Commodity = "C";
        
        /// <summary>Ship Hull ("SH") — observed in real API data. Case-sensitive: both uppercase.</summary>
        public const string ShipHull = "SH";
        
        // --- Location TypeC Codes ---
        
        /// <summary>Colony location ("Co") — location type from asset API.</summary>
        public const string Colony = "Co";
        
        /// <summary>Station location ("St") — location type from asset API.</summary>
        public const string Station = "St";
        
        /// <summary>Ship location ("Sh") — location type from asset API.</summary>
        public const string Ship = "Sh";
    }
}
```

### Replacement Strategy

Each production code site is updated mechanically:

| File | Current Pattern | Replacement |
|------|----------------|-------------|
| `AssetMergeService.MapAssetTypeC` | `string.Equals(trimmed, "Bp", ...)` | `string.Equals(trimmed, AssetTypeCodes.Blueprint, ...)` |
| `QueueSyncService.CascadeCargoDetailItems` | `case "Cr":` / `case "Crate":` | `case AssetTypeCodes.Crate:` (remove "Crate" case) |
| `GameApiSyncScheduler` | `string.Equals(location.LocationType, "Co", ...)` | `string.Equals(location.LocationType, AssetTypeCodes.Colony, ...)` |
| `ColonyMergeService.HasBlueprintProperties` | `string.Equals(typeC, "Bp", ...)` | `string.Equals(typeC, AssetTypeCodes.Blueprint, ...)` |
| `ColonyMergeService.IsSurveyItem` | `string.Equals(..., "Sc", ...)` | `string.Equals(..., AssetTypeCodes.Survey, ...)` |

### Trailing-Space Normalisation

The API sometimes pads single-character codes with a trailing space (e.g. `"S "` instead of `"S"`). The normalisation strategy:

1. **Constants store trimmed values** — no trailing spaces in constant definitions
2. **Comparison sites trim before comparing** — `entry.TypeC?.Trim()` in switch expressions, or the existing `typeC?.Trim()` variable pattern
3. **No changes needed for `string.Equals` with OrdinalIgnoreCase** — these already operate on a pre-trimmed variable (`var trimmed = typeC?.Trim()`)

### Magic-Strings Audit Extension

The `magic-strings.js` tool needs two changes:

1. **Scan constants from both directories** — add `OE2EmpireTracker.Common/Constants` as a second constants source
2. **Scan source files in both projects** — add `OE2EmpireTracker.Common` to the source file scan (excluding its own Constants directory)
3. **Single-character false-positive exclusion** — for constants with a value of 1 character (e.g. `"R"`, `"S"`, `"F"`), only flag matches where the literal appears as a standalone quoted string `"X"`, not as a substring of a longer literal like `"Resource"`

The exclusion logic: when the constant value is a single character, the regex match must verify the literal is exactly `"X"` (the constant value surrounded by quotes with nothing else between them).

## Data Models

No new data models are introduced. The `AssetTypeCodes` class contains only `public const string` fields — no instances, no state, no methods.

The existing `GameApiAssetCargoItem.TypeC` and `GameApiAssetLocation.LocationType` string properties remain unchanged. They continue to hold raw API values; comparison sites trim before comparing against constants.

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: All constants are trimmed

*For any* public const string field in the AssetTypeCodes class, the field value SHALL equal its trimmed form (i.e., `value == value.Trim()`).

**Validates: Requirements 1.4, 2.3**

### Property 2: Trailing-space resilience in MapAssetTypeC

*For any* known AssetTypeCodes cargo constant value, calling `MapAssetTypeC` with that value padded with arbitrary trailing whitespace (1-3 spaces) SHALL return the same `ItemTypeEnum` result as calling it with the trimmed value.

**Validates: Requirements 2.1, 2.2**

## Error Handling

This feature introduces no new error paths. The refactoring is mechanical (constant substitution), so all existing error handling remains unchanged:

- `MapAssetTypeC` already logs a warning and returns `None` for unknown typeC values
- `CascadeCargoDetailItems` already falls through the switch default for unrecognised codes
- `GameApiSyncScheduler` already logs and skips unrecognised location types

The removal of the `"Crate"` case label does not introduce a new error — the API has never sent this value (it was a bug in the original code). The `"Cr"` case (now `AssetTypeCodes.Crate`) handles all crate items.

## Testing Strategy

### Property-Based Tests (FsCheck, minimum 100 iterations)

| Property | Test Description | Library |
|----------|-----------------|---------|
| Property 1 | Reflect over AssetTypeCodes fields, verify all are trimmed | FsCheck/NUnit |
| Property 2 | Generate whitespace suffixes, verify MapAssetTypeC handles them | FsCheck/NUnit |

**Configuration:**
- FsCheck 2.16.6 (installed version)
- `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute
- Tag format: `Feature: asset-type-constants, Property {N}: {text}`

### Unit Tests (NUnit, example-based)

| Test | Purpose |
|------|---------|
| Each constant has expected value | Smoke test: Blueprint == "Bp", Crate == "Cr", etc. |
| MapAssetTypeC returns correct enum for each constant | Regression for the type mapping |
| "Crate" no longer appears as a switch case label | Verify dead code removal |
| Audit tool reports zero findings after replacement | Integration: `magic-strings.js` exit code 0 |

### Verification via Existing Tests

All existing tests in the three test suites (WinForms NUnit, Server, Web) must pass without modification to assertions. This confirms the refactoring is behaviour-preserving.

### Audit Tool Verification

After all replacements, `node .kiro/tools/magic-strings.js` must exit with code 0 (zero findings). This confirms no raw typeC literals remain where constants should be used.
