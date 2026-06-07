# Requirements Document

## Introduction

The game API returns `typeC` codes as short string values (e.g. "Bp", "Cr", "Sc", "S") across multiple endpoints (asset locations, cargo items, market listings). These codes are currently scattered as raw string literals across 50+ locations in production and test code. This has already caused a real bug where "Crate" was used instead of the correct API value "Cr", preventing crate processing entirely.

This feature centralises all known typeC values into a single constants class (`AssetTypeCodes`) in `OE2EmpireTracker.Common/Constants/`, replaces all raw literals with constant references, handles the trailing-space issue from the API, and extends the magic-strings audit tool to cover the new constants.

## Glossary

- **AssetTypeCodes**: Static class in `OE2EmpireTracker.Common/Constants/` defining `public const string` fields for all known game API typeC values
- **TypeC**: A short string code returned by the game API to classify cargo items (e.g. "Bp" = Blueprint, "Cr" = Crate, "R" = Resource)
- **LocationTypeC**: A short string code returned by the game API to classify asset location types (e.g. "Co" = Colony, "St" = Station, "Sh" = Ship)
- **Magic_Strings_Audit**: The automated audit tool (`.kiro/tools/magic-strings.js`) that cross-references constant definitions against raw literal usage in code
- **Trailing_Space**: A known API quirk where single-character typeC codes are sometimes padded with a trailing space (e.g. "S " instead of "S")

## Requirements

### Requirement 1: Define Asset Type Code Constants

**User Story:** As a developer, I want all known typeC values defined as named constants in a single location, so that I can reference them by name instead of using error-prone string literals.

#### Acceptance Criteria

1. THE AssetTypeCodes class SHALL define `public const string` fields for each known cargo item typeC value confirmed by the API swagger documentation: Blueprint ("Bp"), Crate ("Cr"), Survey ("Sc"), ShipPart ("S"), Flatpack ("F"), Workforce ("W"), Resource ("R"), Ammunition ("A"), Deployable ("D"), Share ("Sh"), and Commodity ("L")
2. THE AssetTypeCodes class SHALL define `public const string` fields for typeC values observed in real API data but not listed in the swagger type filter: Commodity ("C") and ShipHull ("SH")
3. THE AssetTypeCodes class SHALL define `public const string` fields for each known location typeC value: Colony ("Co"), Station ("St"), and Ship ("Sh")
4. THE AssetTypeCodes class SHALL store the trimmed (no trailing space) canonical form of each code as the constant value
5. THE AssetTypeCodes class SHALL reside in namespace `OE2EmpireTracker.Constants` within the `OE2EmpireTracker.Common` project at path `OE2EmpireTracker.Common/Constants/AssetTypeCodes.cs`
6. THE AssetTypeCodes class SHALL include XML documentation comments on each field identifying the human-readable meaning of the code and its source (swagger, discovery data, or code observation)

### Requirement 2: Trailing-Space Normalisation

**User Story:** As a developer, I want the codebase to trim typeC values before comparing, so that trailing-space variants from the API (e.g. "S ") match the constants correctly.

#### Acceptance Criteria

1. WHEN production code compares an API-supplied typeC value against an AssetTypeCodes constant, THE comparison site SHALL trim the API value before comparing (using `.Trim()` or `switch` on a pre-trimmed variable)
2. WHEN a `switch` statement is used for typeC dispatch, THE switch expression SHALL operate on the trimmed value rather than the raw API field
3. THE AssetTypeCodes constants SHALL NOT contain trailing spaces — the constants store the canonical trimmed form only

### Requirement 3: Replace Raw Literals in Production Code

**User Story:** As a developer, I want all raw typeC string literals in production code replaced with AssetTypeCodes constant references, so that typos and inconsistencies are eliminated.

#### Acceptance Criteria

1. WHEN production code in `OE2EmpireTracker.Common` references a typeC value, THE code SHALL use the corresponding AssetTypeCodes constant instead of a raw string literal
2. WHEN `AssetMergeService.MapAssetTypeC` compares against typeC values, THE comparisons SHALL use AssetTypeCodes constants
3. WHEN `QueueSyncService.CascadeCargoDetailItems` dispatches on typeC values, THE switch cases SHALL use AssetTypeCodes constants
4. WHEN `GameApiSyncScheduler` compares location typeC values, THE comparisons SHALL use AssetTypeCodes constants (Colony, Station, Ship)
5. WHEN `ColonyMergeService` checks typeC for blueprint or survey processing, THE comparisons SHALL use AssetTypeCodes constants
6. IF the legacy value "Crate" exists as a case label alongside "Cr", THEN THE code SHALL remove the "Crate" case since the API never sends this value

### Requirement 4: Replace Raw Literals in Test Code

**User Story:** As a developer, I want all raw typeC string literals in test code replaced with AssetTypeCodes constant references, so that tests stay in sync with production code and document expected API values.

#### Acceptance Criteria

1. WHEN test code assigns a `TypeC` property on a test fixture object, THE assignment SHALL use the corresponding AssetTypeCodes constant instead of a raw string literal
2. WHEN test code generates typeC values in property-based test generators (e.g. `Gen.Elements(...)`), THE generator SHALL use AssetTypeCodes constants
3. WHEN test code uses typeC values in switch statements or comparisons, THE code SHALL use AssetTypeCodes constants
4. IF test code contains the legacy value "Crate" for typeC, THEN THE test SHALL be updated to use `AssetTypeCodes.Crate` (value "Cr")

### Requirement 5: Extend Magic-Strings Audit Tool

**User Story:** As a developer, I want the magic-strings audit tool to scan constants from `OE2EmpireTracker.Common/Constants/` in addition to `OE2EmpireTracker/Constants/`, so that future raw typeC literal usage is caught automatically.

#### Acceptance Criteria

1. THE Magic_Strings_Audit tool SHALL scan constant definitions from both `OE2EmpireTracker/Constants/*.cs` and `OE2EmpireTracker.Common/Constants/*.cs`
2. THE Magic_Strings_Audit tool SHALL scan source files in both `OE2EmpireTracker/` and `OE2EmpireTracker.Common/` for raw literal usage (excluding each project's own Constants directory)
3. WHEN the audit runs after all replacements are complete, THE audit SHALL report zero findings for typeC-related string literals
4. THE Magic_Strings_Audit tool SHALL exclude single-character constants from matching when the character appears inside longer string literals (to avoid false positives where "R" matches inside "Resource")

### Requirement 6: Backward Compatibility

**User Story:** As a developer, I want the refactoring to be purely mechanical with no behaviour changes, so that existing tests continue to pass and runtime behaviour is identical.

#### Acceptance Criteria

1. THE refactoring SHALL NOT change any runtime behaviour — only raw string literals are replaced with named constant references of identical value
2. WHEN the "Crate" case label is removed from `QueueSyncService`, THE remaining `AssetTypeCodes.Crate` case SHALL continue to handle crate items correctly (the API has never sent "Crate")
3. THE solution SHALL compile with zero errors and zero warnings after all replacements
4. ALL existing tests SHALL pass after all replacements without modification to test assertions

### Requirement 7: Consistent Case Handling Strategy

**User Story:** As a developer, I want a clear, documented strategy for case-sensitive vs case-insensitive typeC comparisons, so that the approach is uniform across the codebase.

#### Acceptance Criteria

1. WHEN code uses `string.Equals` for typeC comparison, THE comparison SHALL use `StringComparison.OrdinalIgnoreCase` to match existing production behaviour
2. WHEN code uses `switch` statements for typeC dispatch, THE switch expression SHALL be on a trimmed value and case labels SHALL use the exact constant values (which preserves existing case-sensitive switch behaviour)
3. THE AssetTypeCodes class SHALL include an XML documentation comment on the class stating that API codes are case-sensitive but comparison code uses OrdinalIgnoreCase for resilience
4. THE AssetTypeCodes class SHALL document the ambiguous "SH" vs "Sh" pair, noting that "SH" (both uppercase) means ShipHull while "Sh" (capital S, lowercase h) means Share, and that this pair requires case-sensitive discrimination
