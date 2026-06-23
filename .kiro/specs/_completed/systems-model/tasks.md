# Implementation Plan: Systems Model (BL-019)

## Overview

Implement star system coordinate data for the OE2 Empire Tracker, enabling distance calculations, system lookups, faction/infrastructure queries, and a system editor form. The implementation follows existing patterns: model in Common, services as singletons in the main project, Dictionary-based caches, ReadOnly wrappers, event-based notifications, and SafeFileWriter for persistence.

## Tasks

- [x] 1. Create StarSystem and ReadOnlyStarSystem models
  - [x] 1.1 Create StarSystem POCO in OE2EmpireTracker.Common/Models/StarSystem.cs
    - Define all 15 properties with JsonProperty attributes using compact names (id, n, x, y, q, s, r, l, st, fid, fn, fc, o, sp, sb)
    - Use decimal for X/Y coordinates, int for Id/Quadrant/Sector/Region/Locality/FactionId, string for Name/SpectralClass/FactionName/FactionColor, bool for HasOrbital/HasSpaceport/HasStarbase
    - Add DefaultValue attributes on mutable properties (FactionId=0, FactionName="", FactionColor="", HasOrbital=false, HasSpaceport=false, HasStarbase=false)
    - Satisfies: Req 1, Criteria 1-10 ("Star System data model with all properties and JSON serialization")

  - [x] 1.2 Create ReadOnlyStarSystem wrapper in OE2EmpireTracker.Common/Models/ReadOnlyStarSystem.cs
    - Wrap a mutable StarSystem instance, expose all 15 properties as read-only getters
    - Follow existing ReadOnlyBlueprint/ReadOnlyCommodity pattern
    - Satisfies: Req 12, Criterion 4 ("SHALL NOT allow modification of immutable properties through UI or service layer")

  - [x] 1.3 Write property test: Serialization round-trip (Property 1)
    - **Property 1: Serialization round-trip**
    - For any valid StarSystem, serialize to JSON with DefaultValueHandling.Ignore then deserialize back; all 15 properties must be equivalent
    - **Validates: Req 9, Criterion 1; Req 1, Criterion 10**

  - [x] 1.4 Write property test: Serialization omits default values (Property 2)
    - **Property 2: Serialization omits default values**
    - For any StarSystem with FactionId=0, empty FactionName/FactionColor, and all infrastructure flags false, serialized JSON must not contain keys "fid", "fn", "fc", "o", "sp", "sb"
    - **Validates: Req 9, Criterion 4**

  - [x] 1.5 Write unit tests for StarSystem serialization edge cases
    - Test deserialization with missing optional fields defaults to empty string/zero/false
    - Test compact JSON property names match source format
    - Test DefaultValue attributes produce smaller JSON for unclaimed systems
    - Satisfies: Req 9, Criteria 2-3 ("compact property names and missing field defaults")

- [x] 2. Implement SystemRepository service
  - [x] 2.1 Create SystemRepository in OE2EmpireTracker/Services/SystemRepository.cs
    - Implement List<StarSystem> storage with Dictionary<int, StarSystem> _idIndex and Dictionary<string, StarSystem> _nameIndex (OrdinalIgnoreCase)
    - Implement Load(filePath): deserialize JSON array, call RebuildIndexes, log count
    - Implement Save(): serialize with DefaultValueHandling.Ignore and Formatting.None, write via SafeFileWriter.WriteAllText
    - Implement RebuildIndexes(): populate both dictionaries, log warnings for duplicate names (first-wins)
    - Expose Count property and Systems as IReadOnlyList<StarSystem>
    - Implement SystemDataChanged event (EventHandler)
    - Satisfies: Req 2, Criteria 1-3 ("separate SystemData.json, JSON array, load at startup"); Req 3, Criteria 1-3 ("O(1) lookups by Id and Name, expose count"); Req 3, Criterion 7 ("duplicate names: first-wins with warning")

  - [x] 2.2 Implement lookup methods on SystemRepository
    - FindById(int id): O(1) dictionary lookup, return null if not found
    - FindByName(string name): O(1) case-insensitive dictionary lookup, return null if not found
    - FindByGrid(int? quadrant, int? sector, int? region, int? locality): filter by any combination of grid values, return IEnumerable<StarSystem>
    - SearchByName(string partial): case-insensitive contains match, return IEnumerable<StarSystem>
    - Satisfies: Req 3, Criteria 1-2 ("O(1) lookup by Id and Name"); Req 3, Criteria 4-5 ("grid filter and partial name search"); Req 5, Criterion 1 ("resolve Colony.SystemName via FindByName")

  - [x] 2.3 Implement infrastructure and faction query methods on SystemRepository
    - FindWithSpaceport(): return systems where HasSpaceport == true as IEnumerable
    - FindWithStarbase(): return systems where HasStarbase == true as IEnumerable
    - FindWithInfrastructure(): return systems where HasOrbital || HasSpaceport || HasStarbase as IEnumerable
    - FindByFaction(int factionId): return systems where FactionId matches as IEnumerable
    - GetFactionSummary(): return distinct non-zero factions with count as IEnumerable<(int, string, string, int)>
    - Satisfies: Req 6, Criteria 1-4 ("infrastructure queries as enumerable"); Req 7, Criteria 1-3 ("faction queries and summary")

  - [x] 2.4 Implement mutation methods on SystemRepository
    - UpdateSystem(int id, Action<StarSystem> mutator): find by id, apply mutator, call Save(), fire SystemDataChanged; no-op with warning if id not found
    - ReplaceAll(List<StarSystem> systems): replace _systems list, call RebuildIndexes, call Save(), fire SystemDataChanged
    - Satisfies: Req 12, Criteria 1-3 ("update mutable properties, persist, fire event"); Req 12, Criterion 5 ("bulk re-import replaces all data and fires event")

  - [x] 2.5 Implement error handling in SystemRepository.Load
    - File missing: log warning, initialize with empty list (0 systems)
    - File empty: log warning, initialize with empty list
    - Malformed JSON: log error with exception details, initialize with empty list
    - No exceptions propagate — all caught and logged at service boundary
    - Satisfies: Req 2, Criteria 4-5 ("graceful degradation for missing/empty/malformed file")

  - [x] 2.6 Write property test: ID lookup correctness (Property 3)
    - **Property 3: ID lookup correctness**
    - For any set of StarSystems loaded, FindById(system.Id) returns that exact system
    - **Validates: Req 3, Criterion 1**

  - [x] 2.7 Write property test: Name lookup is case-insensitive (Property 4)
    - **Property 4: Name lookup is case-insensitive**
    - For any set of StarSystems with unique names, FindByName with any case variation returns the correct system
    - **Validates: Req 3, Criterion 2; Req 5, Criterion 1**

  - [x] 2.8 Write property test: Count equals loaded systems (Property 5)
    - **Property 5: Count equals loaded systems**
    - For any list of N StarSystems loaded, Count property equals N
    - **Validates: Req 3, Criterion 3**

  - [x] 2.9 Write property test: Grid filter returns only matching systems (Property 6)
    - **Property 6: Grid filter returns only matching systems**
    - For any grid filter parameters, all returned systems match the filter and no matching system is excluded
    - **Validates: Req 3, Criterion 4**

  - [x] 2.10 Write property test: Partial name search correctness (Property 7)
    - **Property 7: Partial name search returns only containing matches**
    - For any non-empty search string, all returned systems contain that string (case-insensitive) and no containing system is excluded
    - **Validates: Req 3, Criterion 5**

  - [x] 2.11 Write property test: Infrastructure filters (Property 10)
    - **Property 10: Infrastructure filters return exactly matching systems**
    - FindWithSpaceport returns exactly systems where HasSpaceport is true; FindWithStarbase returns exactly systems where HasStarbase is true; FindWithInfrastructure returns exactly systems where any infrastructure flag is true
    - **Validates: Req 6, Criteria 1-3**

  - [x] 2.12 Write property test: Faction query correctness (Property 11)
    - **Property 11: Faction query returns correct systems and summary**
    - FindByFaction returns exactly systems with matching FactionId; GetFactionSummary includes every distinct non-zero FactionId with correct count
    - **Validates: Req 7, Criteria 1-3**

  - [x] 2.13 Write property test: Mutation updates and fires event (Property 14)
    - **Property 14: Mutation updates values and fires event**
    - For any system and valid new mutable property values, UpdateSystem applies changes and fires SystemDataChanged exactly once
    - **Validates: Req 12, Criteria 1, 3**

  - [x] 2.14 Write unit tests for SystemRepository error handling
    - Test Load with missing file: 0 systems, warning logged
    - Test Load with empty file: 0 systems, warning logged
    - Test Load with malformed JSON: 0 systems, error logged
    - Test Load with duplicate names: first-wins, warning logged
    - Test UpdateSystem with unknown ID: no-op, no event fired
    - Test ReplaceAll replaces data and fires event
    - Satisfies: Req 2, Criteria 4-5; Req 3, Criterion 7; Req 12, Criterion 5

- [x] 3. Implement DistanceCalculator utility
  - [x] 3.1 Create DistanceCalculator in OE2EmpireTracker/Services/DistanceCalculator.cs
    - Static class with no state
    - Calculate(StarSystem a, StarSystem b): Euclidean distance sqrt((x2-x1)² + (y2-y1)²) using (decimal)Math.Sqrt((double)(...)), return decimal
    - Calculate(int idA, int idB, SystemRepository repo): resolve IDs via repo, return -1 if either unresolvable
    - CalculateRoute(IList<int> systemIds, SystemRepository repo): sum consecutive leg distances, skip unresolvable legs with warning, return 0 for empty/single-system routes
    - Return -1 for null system objects
    - Satisfies: Req 4, Criteria 1-7 ("Euclidean distance, static utility, ID resolution, route calculation, error handling")

  - [x] 3.2 Write property test: Distance symmetry, non-negativity, identity (Property 8)
    - **Property 8: Distance is symmetric, non-negative, and zero for identical points**
    - Calculate(A,B) == Calculate(B,A); Calculate(A,B) >= 0; Calculate(A,A) == 0
    - **Validates: Req 4, Criterion 1**

  - [x] 3.3 Write property test: Route distance equals sum of legs (Property 9)
    - **Property 9: Route distance equals sum of consecutive leg distances**
    - For any ordered list of 2+ resolvable system IDs, CalculateRoute equals sum of Calculate for consecutive pairs
    - **Validates: Req 4, Criterion 6**

  - [x] 3.4 Write unit tests for DistanceCalculator error cases
    - Test unresolvable system ID returns -1
    - Test null system object returns -1
    - Test empty route list returns 0
    - Test single-system route returns 0
    - Test route with unresolvable leg skips that leg and logs warning
    - Satisfies: Req 4, Criteria 4, 7 ("return -1 for invalid, skip unresolvable legs")

- [x] 4. Implement SystemImporter service
  - [x] 4.1 Create SystemImporter in OE2EmpireTracker/Services/SystemImporter.cs
    - Static class with Import(string sourcePath, string outputPath) method returning int (count imported)
    - Read source JSON array, map fields: id→Id, n→Name, x→X, y→Y, q→Quadrant, s→Sector, r→Region, l→Locality, st→SpectralClass, fid→FactionId, fn→FactionName, fc→FactionColor
    - Convert integer flags o/sp/sb (0/1) to boolean HasOrbital/HasSpaceport/HasStarbase
    - Preserve decimal precision on X/Y (at least 6 decimal places)
    - Serialize output with DefaultValueHandling.Ignore and Formatting.None
    - Write via SafeFileWriter.WriteAllText for atomic persistence
    - Handle missing source file: log warning, return 0
    - Handle malformed source: log error with exception, return 0
    - Satisfies: Req 8, Criteria 1-6 ("import utility, field mapping, int-to-bool conversion, precision, missing file handling, idempotent")

  - [x] 4.2 Write property test: Import field mapping preserves source data (Property 12)
    - **Property 12: Import field mapping preserves all source data**
    - For any valid source record, the mapped StarSystem has equivalent values for all fields, with int flags correctly converted to booleans
    - **Validates: Req 8, Criteria 2-3**

  - [x] 4.3 Write property test: Import is idempotent (Property 13)
    - **Property 13: Import is idempotent**
    - Running import twice on the same source produces byte-identical SystemData.json output
    - **Validates: Req 8, Criterion 6**

  - [x] 4.4 Write unit tests for SystemImporter error cases
    - Test missing source file: returns 0, no crash, warning logged
    - Test malformed source file: returns 0, error logged
    - Satisfies: Req 8, Criterion 5 ("missing source file logs warning and skips")

- [x] 5. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Integrate SystemRepository into EmpireContext
  - [x] 6.1 Add SystemRepository to EmpireContext
    - Add private SystemRepository _systemRepository field
    - Add public SystemRepository property
    - In constructor (after existing Init* calls): instantiate SystemRepository and call Load(SystemRepository.FilePath)
    - Do NOT include in WriteContext() — systems have their own persistence
    - Handle missing SystemData.json gracefully (0 systems, no crash)
    - Satisfies: Req 2, Criterion 3 ("load SystemData.json at startup as part of EmpireContext initialization"); Req 3, Criterion 6 ("accessible via EmpireContext as read-only property"); Req 10, Criterion 4 ("initialized once at startup")

  - [x] 6.2 Write unit tests for EmpireContext SystemRepository integration
    - Test SystemRepository is accessible after EmpireContext initialization
    - Test missing SystemData.json does not crash EmpireContext startup
    - Satisfies: Req 2, Criteria 3-4; Req 3, Criterion 6

- [x] 7. Add FsCheck NuGet dependency to test project
  - [x] 7.1 Add FsCheck and FsCheck.NUnit packages to OE2EmpireTracker.Tests
    - Add FsCheck 2.16+ and FsCheck.NUnit to OE2EmpireTracker.Tests/packages.config
    - Run nuget restore to download packages
    - Verify test project builds with FsCheck references
    - Satisfies: Design testing strategy ("FsCheck 2.16+ with FsCheck.NUnit integration")

- [x] 8. Create SystemViewModel
  - [x] 8.1 Create SystemViewModel in OE2EmpireTracker/ViewModels/SystemViewModel.cs
    - Wrap a ReadOnlyStarSystem for display binding
    - Expose all properties from ReadOnlyStarSystem
    - Add computed GridLocation string property (e.g. "Q1-S2-R3-L4")
    - Add formatted display properties as needed for DataGridView binding
    - Satisfies: Req 11, Criteria 2-3 ("searchable list display and detail panel")

- [x] 9. Create FormSystem UI
  - [x] 9.1 Create FormSystem MDI child in OE2EmpireTracker/Forms/System/FormSystem.cs
    - MDI child window accessible from Manage menu
    - Left panel: searchable DataGridView with system list
    - Partial name filter TextBox and grid location filter (Quadrant/Sector/Region/Locality dropdowns)
    - Right panel: detail view showing all system properties
    - Read-only fields: Id, Name, X, Y, Quadrant, Sector, Region, Locality, SpectralClass
    - Editable fields: FactionId (combo), FactionName, FactionColor, HasOrbital (checkbox), HasSpaceport (checkbox), HasStarbase (checkbox)
    - Faction dropdown populated from distinct factions in dataset plus manual entry option
    - Save button: calls SystemRepository.UpdateSystem with mutator for mutable fields
    - Re-import button: confirmation dialog warning about overwriting manual edits, then calls SystemImporter.Import followed by SystemRepository.ReplaceAll
    - Subscribe to SystemRepository.SystemDataChanged for refresh
    - Satisfies: Req 11, Criteria 1-10 ("FormSystem MDI child with search, detail, edit, save, re-import")

  - [x] 9.2 Wire FormSystem into MainWindow Manage menu
    - Add "Systems" menu item to Manage menu in MainWindow
    - Open FormSystem as MDI child when clicked
    - Satisfies: Req 11, Criterion 1 ("MDI child window accessible from Manage menu")

- [x] 10. Colony-to-System resolution integration
  - [x] 10.1 Verify Colony.SystemName resolves via SystemRepository.FindByName
    - Confirm Colony model already has SystemName property (do NOT modify Colony model)
    - Document that colony location is resolved at runtime via SystemRepository.FindByName(colony.SystemName)
    - Distance between colonies: resolve both systems via FindByName, then use DistanceCalculator; return -1 if either is unresolvable
    - Satisfies: Req 5, Criteria 1-4 ("colony-to-system association via name lookup, no Colony model modification")

- [x] 11. Performance validation
  - [x] 11.1 Verify performance characteristics
    - Confirm Dictionary-based indexes provide O(1) lookups (by design)
    - Confirm DistanceCalculator is constant-time (no allocations beyond return value)
    - Confirm SystemRepository is initialized once at startup with mutable properties updatable without full reload
    - Satisfies: Req 10, Criteria 1-4 ("fast load, O(1) lookups, constant-time distance, single initialization")

- [x] 12. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.


## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements and criteria for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties (14 total from design)
- Unit tests validate specific examples and edge cases
- FsCheck must be added as a NuGet dependency before property tests can run (Task 7)
- The Colony model must NOT be modified — system resolution is via runtime name lookup
- StarSystem model goes in OE2EmpireTracker.Common/Models/ (shared project)
- SystemRepository, DistanceCalculator, SystemImporter go in OE2EmpireTracker/Services/
- FormSystem goes in OE2EmpireTracker/Forms/System/
- SystemData.json is separate from BaselineData.json to avoid bloating the shared data file
